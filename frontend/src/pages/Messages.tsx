import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../Api";
import { useState, useEffect, useRef } from "react";
import { useAuth } from "../context/useAuth";
import * as signalR from "@microsoft/signalr";
import {
  IncomingMessageSchema,
  MessageContentSchema,
} from "../schemas/messages";
import { useLocation } from "react-router-dom";

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
  conversationId: number;
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
  const location = useLocation();
  const queryClient = useQueryClient();
  const [selectedConversationId, setSelectedConversationId] = useState<
    number | null
  >(
    (location.state as { conversationId?: number } | null)?.conversationId ??
      null,
  );
  const [messageText, setMessageText] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { data: conversations = [], isLoading } = useQuery<Conversation[]>({
    queryKey: ["conversations"],
    queryFn: fetchConversations,
  });

  const { data: following = [] } = useQuery<FollowingUser[]>({
    queryKey: ["following"],
    queryFn: getFollowing,
  });

  const { data: messages = [] } = useQuery<Message[]>({
    queryKey: ["messages", selectedConversationId],
    queryFn: () => fetchMessages(selectedConversationId!),
    enabled: !!selectedConversationId,
  });

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const startConversation = async (userId: number) => {
    const response = await api.post(`/conversations/${userId}`);
    setSelectedConversationId(response.data.conversationId);
    queryClient.invalidateQueries({ queryKey: ["conversations"] });
  };
  const selectedConversationIdRef = useRef(selectedConversationId);

  useEffect(() => {
    selectedConversationIdRef.current = selectedConversationId;
  }, [selectedConversationId]);

  useEffect(() => {
    if (!user?.userId) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_API_URL}/hubs/chat`, {
        accessTokenFactory: () => localStorage.getItem("token") ?? "",
      })
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveMessage", (raw: unknown) => {
      const result = IncomingMessageSchema.safeParse(raw);
      if (!result.success) {
        console.error("Malformed message from SignalR:", result.error);
        return;
      }
      const message = result.data;
      if (message.conversationId === selectedConversationIdRef.current) {
        queryClient.setQueryData(
          ["messages", selectedConversationIdRef.current],
          (old: Message[] = []) => [...old, message],
        );
      }

      queryClient.invalidateQueries({ queryKey: ["conversations"] });
    });

    connection.start().then(() => {
      connection.invoke("JoinPersonalGroup", user.userId.toString());
    });

    return () => {
      connection.invoke("LeavePersonalGroup", user.userId.toString());
      connection.stop();
    };
  }, [user?.userId]);

  const sendMessageMutation = useMutation({
    mutationFn: (content: string) =>
      api.post(`/conversations/${selectedConversationId}/messages`, {
        content,
      }),
    onSuccess: () => {
      setMessageText("");
    },
  });
  const handleSend = () => {
    const result = MessageContentSchema.safeParse(messageText);
    if (!result.success) {
      return;
    }
    sendMessageMutation.mutate(result.data);
  };

  const selectedConversation = conversations.find(
    (c) => c.id === selectedConversationId,
  );

  if (isLoading)
    return <p className="mt-8 text-center text-steel">Loading...</p>;

  const showChat = !!selectedConversationId;

  return (
    <div className="mx-auto flex h-[calc(100vh-64px)] max-w-5xl">
      {/* Conversation list */}
      <div
        className={`${showChat ? "hidden md:flex" : "flex"} w-full flex-col border-r border-hairline-soft bg-white md:w-80`}
      >
        <div className="border-b border-hairline-soft p-5">
          <h1 className="text-2xl">Messages</h1>
        </div>

        <div className="flex flex-1 flex-col gap-5 overflow-y-auto p-4">
          {following.length > 0 && (
            <div>
              <p className="mb-3 text-sm font-bold text-steel">New chat</p>
              <div className="flex gap-4 overflow-x-auto pb-1">
                {following.map((u) => (
                  <button
                    key={u.followingId}
                    onClick={() => startConversation(u.followingId)}
                    className="flex shrink-0 flex-col items-center gap-1.5"
                  >
                    <img
                      src={u.profilePic || "/default-avatar.png"}
                      className="h-14 w-14 rounded-full border-2 border-hairline-soft object-cover"
                    />
                    <span className="max-w-14 truncate text-center text-xs text-charcoal">
                      {u.username}
                    </span>
                  </button>
                ))}
              </div>
            </div>
          )}

          <div>
            <p className="mb-3 text-sm font-bold text-steel">Recent</p>
            {conversations.length === 0 ? (
              <p className="text-sm text-steel">
                No conversations yet. Pick someone above to start one.
              </p>
            ) : (
              <div className="flex flex-col gap-1">
                {conversations.map((conv) => (
                  <button
                    key={conv.id}
                    onClick={() => setSelectedConversationId(conv.id)}
                    className={`flex items-center gap-3 rounded-2xl p-3 text-left transition-colors ${
                      selectedConversationId === conv.id
                        ? "bg-surface-soft"
                        : "active:bg-surface-soft"
                    }`}
                  >
                    <img
                      src={conv.otherUser.profilePic || "/default-avatar.png"}
                      className="h-12 w-12 shrink-0 rounded-full object-cover"
                    />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-bold text-ink-deep">
                        {conv.otherUser.username}
                      </p>
                      <p className="truncate text-xs text-steel">
                        {conv.lastMessage
                          ? conv.lastMessage.content
                          : "No messages yet"}
                      </p>
                    </div>
                    {conv.unreadCount > 0 && (
                      <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-ink-deep px-1.5 text-[11px] font-bold text-white">
                        {conv.unreadCount}
                      </span>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Chat pane */}
      <div
        className={`${showChat ? "flex" : "hidden md:flex"} flex-1 flex-col bg-white`}
      >
        {selectedConversation ? (
          <>
            <div className="flex items-center gap-3 border-b border-hairline-soft p-4">
              <button
                onClick={() => setSelectedConversationId(null)}
                aria-label="Back to conversations"
                className="flex h-10 w-10 items-center justify-center rounded-full border border-hairline text-ink md:hidden"
              >
                ←
              </button>
              <img
                src={
                  selectedConversation.otherUser.profilePic ||
                  "/default-avatar.png"
                }
                className="h-10 w-10 rounded-full object-cover"
              />
              <p className="text-base font-bold text-ink-deep">
                {selectedConversation.otherUser.username}
              </p>
            </div>

            <div className="flex flex-1 flex-col gap-2 overflow-y-auto p-5">
              {messages.map((msg) => {
                const isMe = msg.senderId === Number(user?.userId);
                return (
                  <div
                    key={msg.id}
                    className={`flex ${isMe ? "justify-end" : "justify-start"}`}
                  >
                    <div
                      className={`max-w-[75%] rounded-3xl px-4 py-2.5 text-sm ${
                        isMe
                          ? "rounded-br-lg bg-primary text-white"
                          : "rounded-bl-lg bg-surface-soft text-ink-deep"
                      }`}
                    >
                      {msg.content}
                    </div>
                  </div>
                );
              })}
              <div ref={messagesEndRef} />
            </div>

            <div className="flex gap-2 border-t border-hairline-soft p-4">
              <input
                type="text"
                value={messageText}
                onChange={(e) => setMessageText(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && messageText.trim()) {
                    handleSend();
                  }
                }}
                placeholder="Type a message..."
                className="input rounded-full px-5"
              />
              <button
                onClick={handleSend}
                disabled={sendMessageMutation.isPending}
                className="btn-primary shrink-0"
              >
                Send
              </button>
            </div>
          </>
        ) : (
          <div className="flex flex-1 items-center justify-center">
            <p className="text-sm text-steel">
              Select a conversation to start chatting
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
