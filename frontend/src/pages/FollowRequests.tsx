import { useQueryClient, useMutation, useQuery } from "@tanstack/react-query";
import { type FollowRequest } from "../types/Follow";
import api from "../Api";

async function fetchFollowRequests(): Promise<FollowRequest[]> {
  const response = await api.get("/follow/requests");
  return response.data;
}

export default function FollowRequests() {
  const queryClient = useQueryClient();

  const {
    data: followRequests = [],
    isLoading,
    isError,
  } = useQuery<FollowRequest[]>({
    queryKey: ["followRequests"],
    queryFn: fetchFollowRequests,
  });

  const acceptMutation = useMutation({
    mutationFn: (followerId: number) =>
      api.patch(`/follow/${followerId}/accept`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["followRequests"] });
    },
  });

  if (isLoading)
    return <p className="text-center mt-8 text-gray-500">Loading...</p>;
  if (isError)
    return (
      <p className="text-center mt-8 text-red-500">Failed to load requests.</p>
    );

  return (
    <div className="max-w-2xl mx-auto p-4">
      <h1 className="text-2xl font-medium mb-6">Follow Requests</h1>
      {followRequests.length === 0 ? (
        <p className="text-gray-500">No follow requests found.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {followRequests.map((request) => (
            <li
              key={request.id}
              className="flex items-center gap-4 bg-white border border-gray-100 rounded-xl p-4"
            >
              <img
                src={request.profilePic || "/default-avatar.png"}
                alt={request.username}
                className="w-12 h-12 rounded-full object-cover"
              />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-gray-900 truncate">
                  <a href={`/user/${request.id}`}>{request.username}</a>
                </p>
                <p className="text-sm text-gray-500">
                  {new Date(request.createdAt).toLocaleDateString()}
                </p>
              </div>
              <button
                onClick={() => acceptMutation.mutate(request.followerId)}
                disabled={acceptMutation.isPending}
                className="px-4 py-1.5 text-sm font-medium bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50"
              >
                {acceptMutation.isPending ? "Accepting..." : "Accept"}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
