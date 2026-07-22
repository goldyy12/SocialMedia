import { useState, useEffect } from "react";
import api from "../Api";
import type { Post, Comment } from "../types/Home";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../context/useAuth";
import type { SearchUser, Friend } from "../types/UserProfile";

async function fetchPosts(): Promise<Post[]> {
  const response = await api.get("/post");
  return response.data;
}
async function searchUsers(query: string): Promise<SearchUser[]> {
  const response = await api.get(`/user/search?query=${query}`);
  return response.data;
}

export default function Home() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [content, setContent] = useState("");
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [expandedPosts, setExpandedPosts] = useState<Set<number>>(new Set());
  const [editingPostId, setEditingPostId] = useState<number | null>(null);
  const [editContent, setEditContent] = useState("");
  const [openMenuPostId, setOpenMenuPostId] = useState<number | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [commentText, setCommentText] = useState<{ [key: number]: string }>({});
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState(searchQuery);

  const toggleComments = (postId: number) => {
    setExpandedPosts((prev) => {
      const next = new Set(prev);
      if (next.has(postId)) {
        next.delete(postId);
      } else {
        next.add(postId);
      }
      return next;
    });
  };
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearchQuery(searchQuery), 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const {
    data: posts = [],
    isLoading,
    isError,
  } = useQuery<Post[]>({
    queryKey: ["posts"],
    queryFn: fetchPosts,
  });

  const addPostMutation = useMutation({
    mutationFn: async () => {
      let imageUrl = null;
      if (imageFile) {
        imageUrl = await uploadImage(imageFile);
      }
      return api.post("/post", { content, imageUrl });
    },
    onSuccess: (response) => {
      const newPost = { ...response.data, comments: [] };
      queryClient.setQueryData<Post[]>(["posts"], (prev = []) => [
        newPost,
        ...prev,
      ]);
      setContent("");
      setImageFile(null);
      setImagePreview(null);
      setShowForm(false);
    },
  });
  const { data: searchedUsers = [] } = useQuery<SearchUser[]>({
    queryKey: ["searchedUsers", debouncedSearchQuery],
    queryFn: () => searchUsers(debouncedSearchQuery),
    enabled: debouncedSearchQuery.length > 0,
  });
  const toggleLikeMutation = useMutation({
    mutationFn: (post: Post) =>
      post.isLikedByCurrentUser
        ? api.delete(`/like/${post.id}`)
        : api.post(`/like/${post.id}`),
    onMutate: (post) => {
      const previousPosts = queryClient.getQueryData<Post[]>(["posts"]);

      queryClient.setQueryData<Post[]>(["posts"], (prev = []) =>
        prev.map((p) =>
          p.id === post.id
            ? {
                ...p,
                likesCount: p.isLikedByCurrentUser
                  ? p.likesCount - 1
                  : p.likesCount + 1,
                isLikedByCurrentUser: !p.isLikedByCurrentUser,
              }
            : p,
        ),
      );

      return { previousPosts };
    },
    onError: (err, post, context) => {
      queryClient.setQueryData(["posts"], context?.previousPosts);
    },
  });

  const deletePostMutation = useMutation({
    mutationFn: (postId: number) => api.delete(`/post/${postId}`),
    onSuccess: (_, postId) => {
      queryClient.setQueryData<Post[]>(["posts"], (prev = []) =>
        prev.filter((post) => post.id !== postId),
      );
    },
  });

  const editPostMutation = useMutation({
    mutationFn: ({ postId, content }: { postId: number; content: string }) =>
      api.put(`/post/${postId}`, { content }),
    onSuccess: (response, { postId }) => {
      queryClient.setQueryData<Post[]>(["posts"], (prev = []) =>
        prev.map((post) =>
          post.id === postId
            ? { ...post, content: response.data.content }
            : post,
        ),
      );
      setEditingPostId(null);
      setEditContent("");
    },
  });
  const { data: availableFriends = [] } = useQuery({
    queryKey: ["availableFriends"],
    queryFn: () => api.get("/follow/available-friends").then((r) => r.data),
  });
  const followMutation = useMutation({
    mutationFn: (friendId: number) => api.post(`/follow/${friendId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["availableFriends"] });
    },
  });

  const addCommentMutation = useMutation({
    mutationFn: ({
      postId,
      commentText,
    }: {
      postId: number;
      commentText: string;
    }) => api.post(`/comment/${postId}`, { content: commentText }),
    onSuccess: (response, { postId }) => {
      const newComment: Comment = response.data;
      queryClient.setQueryData<Post[]>(["posts"], (prev = []) =>
        prev.map((post) =>
          post.id === postId
            ? { ...post, comments: [...(post.comments || []), newComment] }
            : post,
        ),
      );
    },
  });
  async function uploadImage(file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post("/post/upload", formData);
    return response.data.url;
  }

  const handleAddComment = (postId: number, commentText: string) => {
    if (!commentText.trim()) return;
    if (commentText.length > 200) {
      alert("Comment must be less than 200 characters.");
      return;
    }
    addCommentMutation.mutate({ postId, commentText });
  };
  function optimizeCloudinaryUrl(url: string, width = 700): string {
    if (!url?.includes("res.cloudinary.com")) return url;
    return url.replace("/upload/", `/upload/w_${width},f_auto,q_auto/`);
  }

  if (isLoading)
    return <p className="text-center mt-8 text-gray-500">Loading...</p>;
  if (isError)
    return (
      <p className="text-center mt-8 text-red-500">Failed to load posts.</p>
    );

  return (
    <div className="max-w-5xl mx-auto p-4 flex gap-6">
      <div className="flex-1 min-w-0">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold">Feed</h1>
          <button
            onClick={() => setShowForm(!showForm)}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg"
          >
            {showForm ? "Cancel" : "New Post"}
          </button>
        </div>

        {showForm && (
          <div className="bg-white p-4 rounded-lg shadow mb-6">
            <textarea
              className="w-full border rounded p-2 mb-2"
              rows={3}
              placeholder="What's on your mind?"
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
            <input
              type="file"
              accept="image/*"
              className="text-sm text-gray-500 mb-2"
              onChange={(e) => {
                const file = e.target.files?.[0] || null;
                setImageFile(file);
                setImagePreview(file ? URL.createObjectURL(file) : null);
              }}
            />
            {imagePreview && (
              <div className="relative mb-2">
                <img
                  src={imagePreview}
                  className="rounded-lg w-full object-cover max-h-64"
                />
                <button
                  onClick={() => {
                    setImageFile(null);
                    setImagePreview(null);
                  }}
                  className="absolute top-1 right-1 bg-black/50 text-white text-xs px-2 py-0.5 rounded-full"
                >
                  Remove
                </button>
              </div>
            )}
            <button
              onClick={() => addPostMutation.mutate()}
              disabled={addPostMutation.isPending}
              className="bg-blue-500 text-white px-4 py-2 rounded-lg disabled:opacity-50"
            >
              {addPostMutation.isPending ? "Posting..." : "Post"}
            </button>
          </div>
        )}

        {/* posts */}
        {posts.length === 0 ? (
          <p className="text-gray-500">No posts yet.</p>
        ) : (
          posts.map((post) => {
            const isExpanded = expandedPosts.has(post.id);
            const comments = post.comments || [];
            const visibleComments = isExpanded
              ? comments
              : comments.slice(0, 2);
            const isOwner = user && post.userId === Number(user.userId);
            const isEditing = editingPostId === post.id;

            return (
              <div
                key={post.id}
                className="bg-white p-4 rounded-lg shadow mb-4"
              >
                <div className="flex items-center gap-2 mb-2">
                  <img
                    src={
                      post.profilePic
                        ? optimizeCloudinaryUrl(post.profilePic, 80)
                        : "/default-avatar.png"
                    }
                    width={32}
                    height={32}
                    alt={`${post.username}'s avatar`}
                    className="w-8 h-8 rounded-full object-cover"
                  />
                  <span className="font-medium">
                    <a href={`/user/${post.userId}`}>{post.username}</a>
                  </span>
                  <span className="text-gray-400 text-sm ml-auto">
                    {new Date(post.createdAt).toLocaleDateString()}
                  </span>
                  {isOwner && (
                    <div className="relative">
                      <button
                        onClick={() =>
                          setOpenMenuPostId(
                            openMenuPostId === post.id ? null : post.id,
                          )
                        }
                        className="text-gray-400 hover:text-gray-600 px-2"
                      >
                        •••
                      </button>
                      {openMenuPostId === post.id && (
                        <div className="absolute right-0 mt-1 w-32 bg-white border border-gray-100 rounded-lg shadow-lg z-10">
                          <button
                            onClick={() => {
                              setEditingPostId(post.id);
                              setEditContent(post.content);
                              setOpenMenuPostId(null);
                            }}
                            className="w-full text-left px-4 py-2 text-sm hover:bg-gray-50"
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => {
                              deletePostMutation.mutate(post.id);
                              setOpenMenuPostId(null);
                            }}
                            className="w-full text-left px-4 py-2 text-sm text-red-500 hover:bg-gray-50"
                          >
                            Delete
                          </button>
                        </div>
                      )}
                    </div>
                  )}
                </div>

                {isEditing ? (
                  <div className="mb-2">
                    <textarea
                      className="w-full border rounded p-2 mb-2 text-sm"
                      rows={3}
                      value={editContent}
                      onChange={(e) => setEditContent(e.target.value)}
                    />
                    <div className="flex gap-2">
                      <button
                        onClick={() =>
                          editPostMutation.mutate({
                            postId: post.id,
                            content: editContent,
                          })
                        }
                        disabled={editPostMutation.isPending}
                        className="px-3 py-1 text-sm bg-blue-500 text-white rounded-lg disabled:opacity-50"
                      >
                        {editPostMutation.isPending ? "Saving..." : "Save"}
                      </button>
                      <button
                        onClick={() => setEditingPostId(null)}
                        className="px-3 py-1 text-sm bg-gray-200 rounded-lg"
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <p className="mb-2">{post.content}</p>
                    {post.imageUrl && (
                      <div
                        className="relative w-full mb-2"
                        style={{ aspectRatio: "16/9" }}
                      >
                        <img
                          src={optimizeCloudinaryUrl(post.imageUrl)}
                          fetchPriority="high"
                          alt={`Post by ${post.username}`}
                          className="rounded-lg w-full h-full object-cover"
                        />
                      </div>
                    )}
                  </>
                )}

                <button
                  onClick={() => toggleLikeMutation.mutate(post)}
                  className={`text-sm px-3 py-1 rounded-full border ${post.isLikedByCurrentUser ? "bg-blue-500 text-white border-blue-500" : "text-gray-500 border-gray-300"}`}
                >
                  ♥ {post.likesCount}
                </button>

                <div className="mt-4">
                  <input
                    value={commentText[post.id] || ""}
                    placeholder="Add a comment..."
                    className="w-full border rounded p-2 text-sm"
                    onChange={(e) =>
                      setCommentText((prev) => ({
                        ...prev,
                        [post.id]: e.target.value,
                      }))
                    }
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        handleAddComment(post.id, commentText[post.id] || "");
                        setCommentText((prev) => ({ ...prev, [post.id]: "" }));
                      }
                    }}
                  />
                </div>

                <div className="mt-4 border-t pt-2 space-y-2">
                  {visibleComments.map((comment) => (
                    <div
                      key={comment.id}
                      className="flex items-start gap-2 bg-gray-50 p-2 rounded"
                    >
                      <img
                        src={comment.profilePic || "/default-avatar.png"}
                        className="w-6 h-6 rounded-full mt-0.5"
                      />
                      <div className="flex flex-col">
                        <span className="text-xs font-semibold text-gray-800">
                          {comment.username}
                        </span>
                        <p className="text-sm text-gray-700">
                          {comment.content}
                        </p>
                      </div>
                      <span className="text-gray-400 text-xs ml-auto">
                        {new Date(comment.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  ))}
                  {comments.length > 2 && (
                    <button
                      onClick={() => toggleComments(post.id)}
                      className="text-sm text-blue-500 hover:underline mt-1"
                    >
                      {isExpanded
                        ? "Show less"
                        : `See ${comments.length - 2} more comments`}
                    </button>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>

      <div className="w-72 shrink-0 hidden lg:block">
        <div className="relative mb-6">
          <input
            type="text"
            placeholder="Search users..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full bg-gray-100 border border-gray-200 rounded-lg py-2 px-4 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {searchedUsers.length > 0 && searchQuery.length > 0 && (
            <div className="absolute top-full mt-1 left-0 w-full bg-white border border-gray-100 rounded-xl shadow-lg z-20">
              {searchedUsers.map((u) => (
                <a
                  key={u.id}
                  href={`/user/${u.id}`}
                  className="flex items-center gap-3 px-4 py-2 hover:bg-gray-50"
                >
                  <img
                    src={u.profilePic || "/default-avatar.png"}
                    className="w-8 h-8 rounded-full object-cover"
                  />
                  <p className="text-sm font-medium text-gray-900">
                    {u.username}
                  </p>
                </a>
              ))}
            </div>
          )}
        </div>

        <div className="bg-white rounded-xl border border-gray-100 p-4">
          <p className="text-sm font-medium text-gray-900 mb-3">
            Who to follow
          </p>
          <div className="flex flex-col gap-3">
            {availableFriends.slice(0, 5).map((friend: Friend) => (
              <div key={friend.id} className="flex items-center gap-3">
                <img
                  src={friend.profilePic || "/default-avatar.png"}
                  className="w-9 h-9 rounded-full object-cover shrink-0"
                />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {friend.username}
                  </p>
                  <p className="text-xs text-gray-500 truncate">
                    {friend.bio || "No bio"}
                  </p>
                </div>
                <button
                  onClick={() => followMutation.mutate(friend.id)}
                  className="text-xs bg-blue-500 text-white px-3 py-1 rounded-full hover:bg-blue-600 shrink-0"
                >
                  Follow
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
