import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../Api";
import { useState } from "react";
import { useAuth } from "../context/useAuth";
import { useEffect, useRef } from "react";

interface Conversation {
  id: number;
  otherUser: {
    id: number;
    username: string;
    profilePic: string | null;
  };
  lastMessage: {
    content: string;
    createdAt: string;
    senderId: number;
  } | null;
  unreadCount: number;
}

interface FollowingUser {
  followingId: number;
  username: string;
  profilePic: string | null;
}

interface Message {
  id: number;
  content: string;
  createdAt: string;
  senderId: number;
  isRead: boolean;
}

async function fetchConversations(): Promise<Conversation[]> {
  const response = await api.get("/conversations");
  return response.data;
}

async function getFollowing(): Promise<FollowingUser[]> {
  const response = await api.get("/follow/following");
  return response.data;
}

async function fetchMessages(conversationId: number): Promise<Message[]> {
  const response = await api.get(`/conversations/${conversationId}/messages`);
  return response.data;
}

export default function Messages() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [selectedConversationId, setSelectedConversationId] = useState<
    number | null
  >(null);
  const [messageText, setMessageText] = useState("");

  const { data: conversations = [], isLoading } = useQuery<Conversation[]>({
    queryKey: ["conversations"],
    queryFn: fetchConversations,
  });
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { data: following = [] } = useQuery<FollowingUser[]>({
    queryKey: ["following"],
    queryFn: getFollowing,
  });

  const { data: messages = [] } = useQuery<Message[]>({
    queryKey: ["messages", selectedConversationId],
    queryFn: () => fetchMessages(selectedConversationId!),
    enabled: !!selectedConversationId,
    refetchInterval: 3000,
  });

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]); // runs every time messages updates

  const startConversation = async (userId: number) => {
    const response = await api.post(`/conversations/${userId}`);
    setSelectedConversationId(response.data.conversationId);
    queryClient.invalidateQueries({ queryKey: ["conversations"] });
  };

  const sendMessageMutation = useMutation({
    mutationFn: (content: string) =>
      api.post(`/conversations/${selectedConversationId}/messages`, {
        content,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["messages", selectedConversationId],
      });
      setMessageText("");
    },
  });

  const selectedConversation = conversations.find(
    (c) => c.id === selectedConversationId,
  );

  if (isLoading) return <p className="text-center mt-8">Loading...</p>;

  return (
    <div className="max-w-5xl mx-auto p-4 h-[calc(100vh-56px)] flex gap-4">
      <div className="w-72 shrink-0 flex flex-col gap-2 overflow-y-auto">
        <h1 className="text-xl font-medium mb-2">Messages</h1>

        <p className="text-xs text-gray-400 mb-1">New chat</p>
        <div className="flex gap-2 overflow-x-auto pb-2 mb-4">
          {following.map((user) => (
            <div
              key={user.followingId}
              onClick={() => startConversation(user.followingId)}
              className="flex flex-col items-center gap-1 cursor-pointer shrink-0"
            >
              <img
                src={user.profilePic || "/default-avatar.png"}
                className="w-10 h-10 rounded-full object-cover"
              />
              <p className="text-xs text-gray-600 max-w-[48px] truncate text-center">
                {user.username}
              </p>
            </div>
          ))}
        </div>

        <p className="text-xs text-gray-400 mb-1">Recent</p>
        {conversations.length === 0 ? (
          <p className="text-sm text-gray-400">No conversations yet.</p>
        ) : (
          conversations.map((conv) => (
            <div
              key={conv.id}
              onClick={() => setSelectedConversationId(conv.id)}
              className={`flex items-center gap-3 rounded-xl p-3 cursor-pointer transition-colors ${
                selectedConversationId === conv.id
                  ? "bg-blue-50 border border-blue-100"
                  : "bg-white border border-gray-100 hover:bg-gray-50"
              }`}
            >
              <img
                src={conv.otherUser.profilePic || "/default-avatar.png"}
                className="w-10 h-10 rounded-full object-cover shrink-0"
              />
              <div className="flex-1 min-w-0">
                <p className="font-medium text-sm text-gray-900 truncate">
                  {conv.otherUser.username}
                </p>
                <p className="text-xs text-gray-500 truncate">
                  {conv.lastMessage
                    ? conv.lastMessage.content
                    : "No messages yet"}
                </p>
              </div>
              {conv.unreadCount > 0 && (
                <span className="w-4 h-4 bg-blue-500 text-white text-[10px] font-medium rounded-full flex items-center justify-center shrink-0">
                  {conv.unreadCount}
                </span>
              )}
            </div>
          ))
        )}
      </div>

      <div className="flex-1 flex flex-col bg-white border border-gray-100 rounded-xl overflow-hidden">
        {selectedConversation ? (
          <>
            <div className="flex items-center gap-3 p-4 border-b border-gray-100">
              <img
                src={
                  selectedConversation.otherUser.profilePic ||
                  "/default-avatar.png"
                }
                className="w-8 h-8 rounded-full object-cover"
              />
              <p className="font-medium">
                {selectedConversation.otherUser.username}
              </p>
            </div>

            <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-2">
              {messages.map((msg) => {
                const isMe = msg.senderId === Number(user?.userId);
                return (
                  <div
                    key={msg.id}
                    className={`flex ${isMe ? "justify-end" : "justify-start"}`}
                  >
                    <div
                      className={`max-w-xs px-4 py-2 rounded-2xl text-sm ${
                        isMe
                          ? "bg-blue-500 text-white rounded-br-sm"
                          : "bg-gray-100 text-gray-900 rounded-bl-sm"
                      }`}
                    >
                      {msg.content}
                    </div>
                    <div ref={messagesEndRef} />
                  </div>
                );
              })}
            </div>

            <div className="p-4 border-t border-gray-100 flex gap-2">
              <input
                type="text"
                value={messageText}
                onChange={(e) => setMessageText(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && messageText.trim()) {
                    sendMessageMutation.mutate(messageText.trim());
                  }
                }}
                placeholder="Type a message..."
                className="flex-1 border border-gray-200 rounded-full px-4 py-2 text-sm outline-none focus:border-blue-300"
              />
              <button
                onClick={() => {
                  if (messageText.trim()) {
                    sendMessageMutation.mutate(messageText.trim());
                  }
                }}
                disabled={sendMessageMutation.isPending}
                className="bg-blue-500 text-white px-4 py-2 rounded-full text-sm hover:bg-blue-600 disabled:opacity-50"
              >
                Send
              </button>
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <p className="text-gray-400">
              Select a conversation to start chatting
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
