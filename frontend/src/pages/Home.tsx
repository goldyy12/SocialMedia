import { useState, useEffect } from "react";
import api from "../Api";
import type { Post } from "../types/Home";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../context/useAuth";
import type { SearchUser, Friend } from "../types/UserProfile";
import PostCard from "../components/PostCard";

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
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState(searchQuery);

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

  async function uploadImage(file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post("/post/upload", formData);
    return response.data.url;
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

        {posts.length === 0 ? (
          <p className="text-gray-500">No posts yet.</p>
        ) : (
          posts.map((post) => (
            <PostCard
              key={post.id}
              post={post}
              queryKey={["posts"]}
              currentUserId={user?.userId}
            />
          ))
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
