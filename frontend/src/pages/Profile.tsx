import api from "../Api";
import type { Post } from "../types/Home";
import { useAuth } from "../context/useAuth";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import PostCard from "../components/PostCard";

interface UserProfile {
  id: number;
  username: string;
  profilePic: string | null;
  bio: string | null;
  createdAt: string;
  followersCount: number;
  followingCount: number;
}

async function fetchUserProfile(id: number): Promise<UserProfile> {
  const response = await api.get(`/user/${id}`);
  return response.data;
}

async function fetchUserPosts(id: number): Promise<Post[]> {
  const response = await api.get(`/post/user/${id}`);
  return response.data;
}

export default function Profile() {
  const { user } = useAuth();
  const id = user?.userId;
  const queryClient = useQueryClient();

  const [isEditing, setIsEditing] = useState(false);
  const [bio, setBio] = useState("");
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  const {
    data: profile,
    isLoading: profileLoading,
    isError: profileError,
  } = useQuery<UserProfile>({
    queryKey: ["user", id],
    queryFn: () => fetchUserProfile(id!),
    enabled: !!id,
  });

  const { data: posts = [], isLoading: postsLoading } = useQuery<Post[]>({
    queryKey: ["userPosts", id],
    queryFn: () => fetchUserPosts(id!),
    enabled: !!id,
  });

  async function uploadImage(file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post("/post/upload", formData);
    return response.data.url;
  }

  const editProfileMutation = useMutation({
    mutationFn: async () => {
      let profilePic = profile?.profilePic ?? null;
      if (imageFile) {
        profilePic = await uploadImage(imageFile);
      }
      const response = await api.patch("/user", { bio, profilePic });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["user", id] });
      setIsEditing(false);
      setImageFile(null);
      setImagePreview(null);
    },
  });

  if (profileLoading || postsLoading)
    return <p className="mt-8 text-center text-steel">Loading...</p>;
  if (profileError || !profile)
    return <p className="mt-8 text-center text-steel">User not found</p>;

  return (
    <div className="mx-auto max-w-2xl p-4 md:py-8">
      <div className="card mb-8 rounded-3xl p-8">
        <div className="flex items-start gap-6">
          <div className="relative shrink-0">
            <img
              src={imagePreview || profile.profilePic || "/default-avatar.png"}
              className="h-24 w-24 rounded-full object-cover"
            />
            {isEditing && (
              <label className="absolute bottom-0 right-0 flex h-8 w-8 cursor-pointer items-center justify-center rounded-full border-2 border-white bg-ink-deep text-sm font-bold text-white">
                +
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={(e) => {
                    const file = e.target.files?.[0] || null;
                    setImageFile(file);
                    setImagePreview(file ? URL.createObjectURL(file) : null);
                  }}
                />
              </label>
            )}
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h1 className="text-3xl">{profile.username}</h1>
              {!isEditing ? (
                <button
                  onClick={() => {
                    setIsEditing(true);
                    setBio(profile.bio || "");
                  }}
                  className="btn-secondary btn-compact"
                >
                  Edit profile
                </button>
              ) : (
                <div className="flex gap-2">
                  <button
                    onClick={() => editProfileMutation.mutate()}
                    disabled={editProfileMutation.isPending}
                    className="btn-primary btn-compact"
                  >
                    {editProfileMutation.isPending ? "Saving..." : "Save"}
                  </button>
                  <button
                    onClick={() => {
                      setIsEditing(false);
                      setImageFile(null);
                      setImagePreview(null);
                    }}
                    className="btn-ghost btn-compact"
                  >
                    Cancel
                  </button>
                </div>
              )}
            </div>

            {isEditing ? (
              <textarea
                className="input mt-3 h-auto py-3"
                rows={2}
                placeholder="Write a bio..."
                value={bio}
                onChange={(e) => setBio(e.target.value)}
              />
            ) : (
              <p className="mt-1 text-base text-charcoal">
                {profile.bio || "No bio yet"}
              </p>
            )}

            <div className="mt-4 flex gap-6 text-sm text-steel">
              <span>
                <strong className="text-ink-deep">
                  {profile.followersCount}
                </strong>{" "}
                <a href="/followers" className="underline-offset-2 hover:underline">
                  followers
                </a>
              </span>
              <span>
                <strong className="text-ink-deep">
                  {profile.followingCount}
                </strong>{" "}
                <a href="/following" className="underline-offset-2 hover:underline">
                  following
                </a>
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
            queryKey={["userPosts", id]}
            currentUserId={user?.userId}
          />
        ))
      )}
    </div>
  );
}
