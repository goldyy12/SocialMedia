import { useQueryClient, useMutation, useQuery } from "@tanstack/react-query";
import api from "../Api";

type Notification = {
  id: string;
  message: string;
  isRead: boolean;
  createdAt: string;
};

async function fetchNotifications(): Promise<Notification[]> {
  const response = await api.get("/notifications");
  return response.data;
}

export default function Notifications() {
  const queryClient = useQueryClient();

  const { data: notifications = [] } = useQuery({
    queryKey: ["notifications"],
    queryFn: fetchNotifications,
  });

  // 👇 hook called inside the component, not outside
  const { mutate: markAllRead, isPending } = useMutation({
    mutationFn: () => api.patch("/notifications/read"),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["unreadNotifications"] }); // 👈 clears bell count too
    },
  });

  return (
    <div className="max-w-2xl mx-auto p-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-medium">Notifications</h1>
        {notifications.length > 0 && (
          <button
            onClick={() => markAllRead()}
            disabled={isPending}
            className="text-sm text-blue-500 hover:text-blue-600 disabled:opacity-50"
          >
            Mark all as read
          </button>
        )}
      </div>

      {notifications.length === 0 ? (
        <p className="text-gray-500 text-center mt-8">No notifications.</p>
      ) : (
        <ul className="space-y-3">
          {notifications.map((notification) => (
            <li
              key={notification.id}
              className={`p-4 rounded-lg border ${
                notification.isRead
                  ? "bg-white border-gray-100 text-gray-500"
                  : "bg-blue-50 border-blue-100 text-gray-900"
              }`}
            >
              <p className="text-sm">{notification.message}</p>
              <p className="text-xs text-gray-400 mt-1">
                {new Date(notification.createdAt).toLocaleDateString()}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
