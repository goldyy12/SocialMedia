import api from "../Api";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { Follower } from "../types/Follow";

async function fetchFollowers(): Promise<Follower[]> {
  const response = await api.get("/follow/followers");
  return response.data;
}

export default function Followers() {
  const queryClient = useQueryClient();

  const {
    data: followers = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["followers"],
    queryFn: fetchFollowers,
  });

  const unfollowMutation = useMutation({
    mutationFn: (followerId: number) =>
      api.delete(`/follow/followers/${followerId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["followers"] });
    },
  });

  if (isLoading)
    return <p className="text-center mt-8 text-gray-500">Loading...</p>;
  if (isError)
    return <p className="text-center mt-8 text-red-500">Failed to load.</p>;

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">Followers</h1>
      {followers.length === 0 ? (
        <p className="text-gray-500">You have no followers yet.</p>
      ) : (
        <ul className="space-y-3">
          {followers.map((follower) => (
            <li
              key={follower.followerId}
              className="bg-white p-4 rounded-lg shadow flex items-center gap-4"
            >
              <img
                src={follower.profilePic || "/default-avatar.png"}
                alt={follower.username}
                className="w-12 h-12 rounded-full object-cover"
              />
              <span className="font-medium flex-1">
                {" "}
                <a href={`/user/${follower.followerId}`}>{follower.username}</a>
              </span>
              <button
                onClick={() => unfollowMutation.mutate(follower.followerId)}
                disabled={unfollowMutation.isPending}
                className="px-3 py-1.5 text-sm font-medium bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50"
              >
                {unfollowMutation.isPending ? "Removing..." : "Remove"}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
