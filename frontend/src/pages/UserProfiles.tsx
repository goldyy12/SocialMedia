import { useParams, useNavigate } from "react-router-dom";
import api from "../Api";
import type { Post } from "../types/Home";
import { useAuth } from "../context/useAuth";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import PostCard from "../components/PostCard";

interface UserProfile {
  id: number;
  username: string;
  profilePic: string | null;
  bio: string | null;
  createdAt: string;
  followersCount: number;
  followingCount: number;
  isFollowing: string;
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
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const startConversation = async () => {
    const response = await api.post(`/conversations/${numericId}`);
    queryClient.invalidateQueries({ queryKey: ["conversations"] });
    navigate("/messages", {
      state: { conversationId: response.data.conversationId },
    });
  };
  const isOwner = Number(user?.userId) === numericId;
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
    return <p className="mt-8 text-center text-steel">Loading...</p>;
  if (profileError || !profile)
    return <p className="mt-8 text-center text-steel">User not found</p>;

  const followLabel = isPending
    ? "..."
    : profile.isFollowing === "pending"
      ? "Pending"
      : profile.isFollowing === "accepted"
        ? "Unfollow"
        : "Follow";

  return (
    <div className="mx-auto max-w-2xl p-4 md:py-8">
      <div className="card mb-8 rounded-3xl p-8">
        <div className="flex items-start gap-6">
          <img
            src={profile.profilePic || "/default-avatar.png"}
            className="h-24 w-24 shrink-0 rounded-full object-cover"
          />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h1 className="text-3xl">{profile.username}</h1>
              <div className="flex gap-2">
                {!isOwner && (
                  <button
                    onClick={startConversation}
                    className="btn-ghost btn-compact"
                  >
                    Message
                  </button>
                )}
                <button
                  onClick={() => toggleFollow()}
                  disabled={isPending}
                  className={`btn-compact ${
                    profile.isFollowing ? "btn-secondary" : "btn-primary"
                  }`}
                >
                  {followLabel}
                </button>
              </div>
            </div>
            <p className="mt-1 text-base text-charcoal">
              {profile.bio || "No bio yet"}
            </p>
            <div className="mt-4 flex gap-6 text-sm text-steel">
              <span>
                <strong className="text-ink-deep">
                  {profile.followersCount}
                </strong>{" "}
                followers
              </span>
              <span>
                <strong className="text-ink-deep">
                  {profile.followingCount}
                </strong>{" "}
                following
              </span>
              <span>
                <strong className="text-ink-deep">{posts.length}</strong> posts
              </span>
            </div>
          </div>
        </div>
      </div>

      <h2 className="mb-4 text-2xl">Posts</h2>
      {posts.length === 0 ? (
        <p className="text-steel">No posts yet.</p>
      ) : (
        posts.map((post) => (
          <PostCard
            key={post.id}
            post={post}
            queryKey={["userPosts", numericId]}
            currentUserId={user?.userId}
          />
        ))
      )}
    </div>
  );
}
