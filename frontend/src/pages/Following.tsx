import api from "../Api";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

import { type Following } from "../types/Follow";

async function fetchFollowing(): Promise<Following[]> {
  const response = await api.get("/follow/following");
  return response.data;
}

export default function Following() {
  const queryClient = useQueryClient();

  const {
    data: following = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["following"],
    queryFn: fetchFollowing,
  });

  const unfollowMutation = useMutation({
    mutationFn: (followingId: number) => api.delete(`/follow/${followingId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["following"] });
    },
  });

  if (isLoading)
    return <p className="text-center mt-8 text-gray-500">Loading...</p>;
  if (isError)
    return <p className="text-center mt-8 text-red-500">Failed to load.</p>;

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">Following</h1>
      {following.length === 0 ? (
        <p className="text-gray-500">You are not following anyone yet.</p>
      ) : (
        <ul className="space-y-3">
          {following.map((user) => (
            <li
              key={user.followingId}
              className="bg-white p-4 rounded-lg shadow flex items-center gap-4"
            >
              <img
                src={user.profilePic || "/default-avatar.png"}
                alt={user.username}
                className="w-12 h-12 rounded-full object-cover"
              />
              <span className="font-medium flex-1">
                {" "}
                <a href={`/user/${user.followingId}`}>{user.username}</a>
              </span>
              <button
                onClick={() => unfollowMutation.mutate(user.followingId)}
                disabled={unfollowMutation.isPending}
                className="px-3 py-1.5 text-sm font-medium bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50"
              >
                {unfollowMutation.isPending ? "Unfollowing..." : "Unfollow"}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
