import api from "../Api";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

interface Friend {
  id: number;
  username: string;
  profilePic: string | null;
  bio: string | null;
  followsYou: boolean; // 👈 add this
}

async function fetchAvailableFriends(): Promise<Friend[]> {
  const response = await api.get("/follow/available-friends");
  return response.data;
}

export default function AddFriends() {
  const queryClient = useQueryClient();

  const {
    data: friends = [],
    isLoading,
    isError,
  } = useQuery<Friend[]>({
    queryKey: ["availableFriends"],
    queryFn: fetchAvailableFriends,
  });

  const followMutation = useMutation({
    mutationFn: (friendId: number) => api.post(`/follow/${friendId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["availableFriends"] });
    },
  });

  if (isLoading)
    return <p className="text-center mt-8 text-gray-500">Loading...</p>;
  if (isError)
    return (
      <p className="text-center mt-8 text-red-500">Failed to load users.</p>
    );

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-2xl font-medium mb-6">Add Friends</h1>
      {friends.length === 0 ? (
        <p className="text-gray-500 text-center mt-8">No users to add.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {friends.map((friend) => (
            <div
              key={friend.id}
              className="flex items-center gap-4 bg-white border border-gray-100 rounded-xl p-4"
            >
              <img
                src={friend.profilePic || "/default-avatar.png"}
                alt={friend.username}
                className="w-12 h-12 rounded-full object-cover shrink-0"
              />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-gray-900 truncate">
                  <a href={`/user/${friend.id}`}>{friend.username}</a>
                </p>
                <p className="text-sm text-gray-500 truncate">
                  {friend.bio || "No bio"}
                </p>
                {friend.followsYou && ( // 👈 "Follows you" badge
                  <span className="text-xs text-blue-500 font-medium">
                    Follows you
                  </span>
                )}
              </div>
              <button
                onClick={() => followMutation.mutate(friend.id)}
                disabled={
                  followMutation.isPending &&
                  followMutation.variables === friend.id
                } // 👈 per-user
                className="shrink-0 px-4 py-1.5 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
              >
                {followMutation.isPending &&
                followMutation.variables === friend.id
                  ? "Sending..."
                  : "Follow"}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
