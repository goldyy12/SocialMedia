import api from "../Api";
import type { Post } from "../types/Home";
import { useAuth } from "../context/useAuth";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";

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
    return <p className="text-center mt-8">Loading...</p>;
  if (profileError || !profile)
    return <p className="text-center mt-8">User not found</p>;

  return (
    <div className="max-w-2xl mx-auto p-4">
      <div className="bg-white rounded-lg shadow p-6 mb-6">
        <div className="flex items-center gap-4">
          <div className="relative">
            <img
              src={imagePreview || profile.profilePic || "/default-avatar.png"}
              className="w-20 h-20 rounded-full object-cover"
            />
            {isEditing && (
              <label className="absolute bottom-0 right-0 bg-blue-500 text-white text-xs rounded-full w-6 h-6 flex items-center justify-center cursor-pointer">
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

          <div className="flex-1">
            <div className="flex items-center justify-between">
              <h1 className="text-2xl font-bold">{profile.username}</h1>
              {!isEditing ? (
                <button
                  onClick={() => {
                    setIsEditing(true);
                    setBio(profile.bio || "");
                  }}
                  className="text-sm text-blue-500 hover:text-blue-600"
                >
                  Edit profile
                </button>
              ) : (
                <div className="flex gap-2">
                  <button
                    onClick={() => editProfileMutation.mutate()}
                    disabled={editProfileMutation.isPending}
                    className="text-sm bg-blue-500 text-white px-3 py-1 rounded-lg disabled:opacity-50"
                  >
                    {editProfileMutation.isPending ? "Saving..." : "Save"}
                  </button>
                  <button
                    onClick={() => {
                      setIsEditing(false);
                      setImageFile(null);
                      setImagePreview(null);
                    }}
                    className="text-sm bg-gray-200 px-3 py-1 rounded-lg"
                  >
                    Cancel
                  </button>
                </div>
              )}
            </div>

            {isEditing ? (
              <textarea
                className="w-full border rounded p-2 text-sm mt-1"
                rows={2}
                placeholder="Write a bio..."
                value={bio}
                onChange={(e) => setBio(e.target.value)}
              />
            ) : (
              <p className="text-gray-500">{profile.bio || "No bio yet"}</p>
            )}

            <div className="flex gap-4 mt-2 text-sm text-gray-600">
              <span>
                <strong>{profile.followersCount}</strong>{" "}
                <a href="/followers">followers</a>
              </span>
              <span>
                <strong>{profile.followingCount}</strong>{" "}
                <a href="/following">following</a>
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
