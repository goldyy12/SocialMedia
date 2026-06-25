import { useParams } from "react-router-dom";
import api from "../Api";
import type { Post } from "../types/Home";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

interface UserProfile {
  id: number;
  username: string;
  profilePic: string | null;
  bio: string | null;
  createdAt: string;
  followersCount: number;
  followingCount: number;
  isFollowing: boolean; // 👈 add this
}

async function fetchUserProfile(id: number): Promise<UserProfile> {
  const response = await api.get(`/user/${id}`);
  return response.data;
}

async function fetchUserPosts(id: number): Promise<Post[]> {
  const response = await api.get(`/post/user/${id}`);
  return response.data;
}

export default function UserProfile() {
  const { id } = useParams<{ id: string }>();
  const numericId = Number(id);
  const queryClient = useQueryClient();

  const {
    data: profile,
    isLoading: profileLoading,
    isError: profileError,
  } = useQuery<UserProfile>({
    queryKey: ["user", numericId],
    queryFn: () => fetchUserProfile(numericId),
    enabled: !!numericId,
  });

  const { data: posts = [], isLoading: postsLoading } = useQuery<Post[]>({
    queryKey: ["userPosts", numericId],
    queryFn: () => fetchUserPosts(numericId),
    enabled: !!numericId,
  });

  const { mutate: toggleFollow, isPending } = useMutation({
    mutationFn: () =>
      profile?.isFollowing
        ? api.delete(`/follow/${numericId}`)
        : api.post(`/follow/${numericId}`),
    onSuccess: () => {
      // refetch the profile so followersCount + isFollowing update
      queryClient.invalidateQueries({ queryKey: ["user", numericId] });
    },
  });

  if (profileLoading || postsLoading)
    return <p className="text-center mt-8">Loading...</p>;
  if (profileError || !profile)
    return <p className="text-center mt-8">User not found</p>;

  return (
    <div className="max-w-2xl mx-auto p-4">
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="flex items-center gap-4">
          <img
            src={profile.profilePic || "/default-avatar.png"}
            className="w-20 h-20 rounded-full object-cover"
          />
          <div className="flex-1">
            <div className="flex items-center justify-between">
              <h1 className="text-2xl font-bold">{profile.username}</h1>
              <button
                onClick={() => toggleFollow()}
                disabled={isPending}
                className={`px-4 py-1.5 rounded-full text-sm font-medium transition-colors ${
                  profile.isFollowing
                    ? "border border-gray-300 text-gray-700 hover:bg-red-50 hover:text-red-500 hover:border-red-300"
                    : "bg-blue-500 text-white hover:bg-blue-600"
                }`}
              >
                {isPending
                  ? "..."
                  : profile.isFollowing
                    ? "Unfollow"
                    : "Follow"}
              </button>
            </div>
            <p className="text-gray-500">{profile.bio || "No bio yet"}</p>
            <div className="flex gap-4 mt-2 text-sm text-gray-600">
              <span>
                <strong>{profile.followersCount}</strong> followers
              </span>
              <span>
                <strong>{profile.followingCount}</strong> following
              </span>
              <span>
                <strong>{posts.length}</strong> posts
              </span>
            </div>
          </div>
        </div>
      </div>

      <h2 className="text-xl font-bold mb-4">Posts</h2>
      {posts.length === 0 ? (
        <p className="text-gray-500">No posts yet.</p>
      ) : (
        posts.map((post) => (
          <div key={post.id} className="bg-white rounded-lg shadow p-4 mb-4">
            <p>{post.content}</p>
            {post.imageUrl && (
              <img
                src={post.imageUrl}
                className="mt-2 rounded-lg w-full object-cover"
              />
            )}
            <p className="text-gray-400 text-sm mt-2">
              {new Date(post.createdAt).toLocaleDateString()}
            </p>
          </div>
        ))
      )}
    </div>
  );
}
