import { useState } from "react";
import api from "../Api";
import type { Post, Comment } from "../types/Home";
import {
  useMutation,
  useQueryClient,
  type QueryKey,
} from "@tanstack/react-query";

function optimizeCloudinaryUrl(url: string, width = 700, height = 394): string {
  if (!url?.includes("res.cloudinary.com")) return url;
  return url.replace(
    "/upload/",
    `/upload/w_${width},h_${height},c_fill,g_auto,f_auto,q_auto/`,
  );
}

interface PostCardProps {
  post: Post;
  queryKey: QueryKey;
  currentUserId?: string | number;
}

export default function PostCard({
  post,
  queryKey,
  currentUserId,
}: PostCardProps) {
  const queryClient = useQueryClient();
  const [isExpanded, setIsExpanded] = useState(false);
  const [editingPostId, setEditingPostId] = useState<number | null>(null);
  const [editContent, setEditContent] = useState("");
  const [openMenu, setOpenMenu] = useState(false);
  const [commentText, setCommentText] = useState("");

  const isOwner = currentUserId && post.userId === Number(currentUserId);
  const isEditing = editingPostId === post.id;
  const comments = post.comments || [];
  const visibleComments = isExpanded ? comments : comments.slice(0, 2);

  const toggleLikeMutation = useMutation({
    mutationFn: () =>
      post.isLikedByCurrentUser
        ? api.delete(`/like/${post.id}`)
        : api.post(`/like/${post.id}`),
    onMutate: () => {
      const previous = queryClient.getQueryData<Post[]>(queryKey);
      queryClient.setQueryData<Post[]>(queryKey, (prev = []) =>
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
      return { previous };
    },
    onError: (_err, _vars, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
  });

  const deletePostMutation = useMutation({
    mutationFn: () => api.delete(`/post/${post.id}`),
    onSuccess: () => {
      queryClient.setQueryData<Post[]>(queryKey, (prev = []) =>
        prev.filter((p) => p.id !== post.id),
      );
    },
  });

  const editPostMutation = useMutation({
    mutationFn: (content: string) => api.put(`/post/${post.id}`, { content }),
    onSuccess: (response) => {
      queryClient.setQueryData<Post[]>(queryKey, (prev = []) =>
        prev.map((p) =>
          p.id === post.id ? { ...p, content: response.data.content } : p,
        ),
      );
      setEditingPostId(null);
      setEditContent("");
    },
  });

  const addCommentMutation = useMutation({
    mutationFn: (text: string) =>
      api.post(`/comment/${post.id}`, { content: text }),
    onSuccess: (response) => {
      const newComment: Comment = response.data;
      queryClient.setQueryData<Post[]>(queryKey, (prev = []) =>
        prev.map((p) =>
          p.id === post.id
            ? { ...p, comments: [...(p.comments || []), newComment] }
            : p,
        ),
      );
    },
  });

  const handleAddComment = () => {
    const trimmed = commentText.trim();
    if (!trimmed) return;
    if (trimmed.length > 200) {
      alert("Comment must be less than 200 characters.");
      return;
    }
    addCommentMutation.mutate(trimmed);
    setCommentText("");
  };

  return (
    <div className="bg-white p-4 rounded-lg shadow mb-4">
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
              onClick={() => setOpenMenu(!openMenu)}
              className="text-gray-400 hover:text-gray-600 px-2"
            >
              •••
            </button>
            {openMenu && (
              <div className="absolute right-0 mt-1 w-32 bg-white border border-gray-100 rounded-lg shadow-lg z-10">
                <button
                  onClick={() => {
                    setEditingPostId(post.id);
                    setEditContent(post.content);
                    setOpenMenu(false);
                  }}
                  className="w-full text-left px-4 py-2 text-sm hover:bg-gray-50"
                >
                  Edit
                </button>
                <button
                  onClick={() => {
                    deletePostMutation.mutate();
                    setOpenMenu(false);
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
              onClick={() => editPostMutation.mutate(editContent)}
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
                src={optimizeCloudinaryUrl(post.imageUrl, 700, 394)}
                fetchPriority="high"
                alt={`Post by ${post.username}`}
                className="rounded-lg w-full h-full"
              />
            </div>
          )}
        </>
      )}

      <button
        onClick={() => toggleLikeMutation.mutate()}
        className={`text-sm px-3 py-1 rounded-full border ${post.isLikedByCurrentUser ? "bg-blue-500 text-white border-blue-500" : "text-gray-500 border-gray-300"}`}
      >
        ♥ {post.likesCount}
      </button>

      <div className="mt-4">
        <input
          value={commentText}
          placeholder="Add a comment..."
          className="w-full border rounded p-2 text-sm"
          onChange={(e) => setCommentText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleAddComment();
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
              <p className="text-sm text-gray-700">{comment.content}</p>
            </div>
            <span className="text-gray-400 text-xs ml-auto">
              {new Date(comment.createdAt).toLocaleDateString()}
            </span>
          </div>
        ))}
        {comments.length > 2 && (
          <button
            onClick={() => setIsExpanded(!isExpanded)}
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
}
