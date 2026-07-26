import { z } from "zod";

export const MessageContentSchema = z
  .string()
  .trim()
  .min(1, "Message can't be empty")
  .max(1000, "Message is too long (max 1000 characters)");

export const IncomingMessageSchema = z.object({
  id: z.number(),
  content: z.string(),
  createdAt: z.string(),
  senderId: z.number(),
  isRead: z.boolean(),
  conversationId: z.number(),
});
