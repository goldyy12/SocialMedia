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
    return <p className="mt-8 text-center text-steel">Loading...</p>;
  if (isError)
    return (
      <p className="mt-8 text-center text-critical">
        Couldn't load posts. Refresh the page to try again.
      </p>
    );

  return (
    <div className="mx-auto flex max-w-5xl gap-8 p-4 md:py-8">
      <div className="min-w-0 flex-1">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-4xl">Feed</h1>
          <button
            onClick={() => setShowForm(!showForm)}
            className={showForm ? "btn-secondary" : "btn-primary"}
          >
            {showForm ? "Cancel" : "New post"}
          </button>
        </div>

        {showForm && (
          <div className="card mb-6 p-6">
            <textarea
              className="input mb-3 h-auto py-3"
              rows={3}
              placeholder="What's on your mind?"
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
            <input
              type="file"
              accept="image/*"
              className="mb-3 block text-sm text-steel file:mr-3 file:rounded-full file:border-2 file:border-ink-deep/10 file:bg-white file:px-4 file:py-1.5 file:text-sm file:font-bold file:text-ink-deep"
              onChange={(e) => {
                const file = e.target.files?.[0] || null;
                setImageFile(file);
                setImagePreview(file ? URL.createObjectURL(file) : null);
              }}
            />
            {imagePreview && (
              <div className="relative mb-3">
                <img
                  src={imagePreview}
                  className="max-h-64 w-full rounded-3xl object-cover"
                />
                <button
                  onClick={() => {
                    setImageFile(null);
                    setImagePreview(null);
                  }}
                  className="absolute right-3 top-3 rounded-full bg-ink-deep/70 px-3 py-1 text-xs font-bold text-white"
                >
                  Remove
                </button>
              </div>
            )}
            <button
              onClick={() => addPostMutation.mutate()}
              disabled={addPostMutation.isPending}
              className="btn-primary"
            >
              {addPostMutation.isPending ? "Posting..." : "Post"}
            </button>
          </div>
        )}

        {posts.length === 0 ? (
          <p className="text-steel">No posts yet. Be the first to share something.</p>
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

      <aside className="hidden w-80 shrink-0 lg:block">
        <div className="sticky top-24">
          <div className="relative mb-6">
            <input
              type="text"
              placeholder="Search users..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-pill"
            />
            {searchedUsers.length > 0 && searchQuery.length > 0 && (
              <div className="absolute left-0 top-full z-20 mt-2 w-full overflow-hidden rounded-2xl border border-hairline-soft bg-white shadow-panel">
                {searchedUsers.map((u) => (
                  <a
                    key={u.id}
                    href={`/user/${u.id}`}
                    className="flex items-center gap-3 px-4 py-2.5 active:bg-surface-soft"
                  >
                    <img
                      src={u.profilePic || "/default-avatar.png"}
                      className="h-8 w-8 rounded-full object-cover"
                    />
                    <p className="text-sm font-bold text-ink-deep">
                      {u.username}
                    </p>
                  </a>
                ))}
              </div>
            )}
          </div>

          <div className="card p-6">
            <h2 className="mb-4 text-xl">Who to follow</h2>
            <div className="flex flex-col gap-4">
              {availableFriends.slice(0, 5).map((friend: Friend) => (
                <div key={friend.id} className="flex items-center gap-3">
                  <img
                    src={friend.profilePic || "/default-avatar.png"}
                    className="h-10 w-10 shrink-0 rounded-full object-cover"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-bold text-ink-deep">
                      {friend.username}
                    </p>
                    <p className="truncate text-xs text-steel">
                      {friend.bio || "No bio"}
                    </p>
                  </div>
                  <button
                    onClick={() => followMutation.mutate(friend.id)}
                    className="btn-secondary btn-compact shrink-0"
                  >
                    Follow
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
      </aside>
    </div>
  );
}
