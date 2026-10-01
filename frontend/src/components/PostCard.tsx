import { useState } from "react";
import api from "../Api";
import type { Post, Comment } from "../types/Home";
import { useMutation, useQueryClient } from "@tanstack/react-query";

function optimizeCloudinaryUrl(url: string, width = 700, height = 394): string {
  if (!url?.includes("res.cloudinary.com")) return url;
  return url.replace(
    "/upload/",
    `/upload/w_${width},h_${height},c_fill,g_auto,f_auto,q_auto/`,
  );
}

// Every cache that holds a list of posts. Prefix matching means
// ["userPosts"] also covers ["userPosts", 4], ["userPosts", 7], ...
const POST_LIST_KEYS = [["posts"], ["userPosts"]];

interface PostCardProps {
  post: Post;
  /** No longer needed: all post lists are kept in sync. Kept so old callers still compile. */
  queryKey?: unknown;
  currentUserId?: string | number;
}

export default function PostCard({ post, currentUserId }: PostCardProps) {
  const queryClient = useQueryClient();
  const [isExpanded, setIsExpanded] = useState(false);
  const [editingPostId, setEditingPostId] = useState<number | null>(null);
  const [editContent, setEditContent] = useState("");
  const [openMenu, setOpenMenu] = useState(false);
  const [commentText, setCommentText] = useState("");

  const isOwner = currentUserId && post.userId === Number(currentUserId);
  const isEditing = editingPostId === post.id;
  const comments = post.comments || [];
  const visibleComments = isExpanded ? comments : comments.slice(0, 2);

  // Apply a change to this post in every cached post list.
  const updateAllLists = (updater: (posts: Post[]) => Post[]) => {
    POST_LIST_KEYS.forEach((key) =>
      queryClient.setQueriesData<Post[]>({ queryKey: key }, (old) =>
        old ? updater(old) : old,
      ),
    );
  };
  const mapPost = (fn: (p: Post) => Post) => (posts: Post[]) =>
    posts.map((p) => (p.id === post.id ? fn(p) : p));

  // Refetch from the server so counts/flags can't stay stale.
  const refreshAllLists = () =>
    POST_LIST_KEYS.forEach((key) =>
      queryClient.invalidateQueries({ queryKey: key }),
    );

  const toggleLikeMutation = useMutation({
    mutationFn: () =>
      post.isLikedByCurrentUser
        ? api.delete(`/like/${post.id}`)
        : api.post(`/like/${post.id}`),
    onMutate: async () => {
      await Promise.all(
        POST_LIST_KEYS.map((key) =>
          queryClient.cancelQueries({ queryKey: key }),
        ),
      );
      const snapshots = POST_LIST_KEYS.flatMap((key) =>
        queryClient.getQueriesData<Post[]>({ queryKey: key }),
      );
      updateAllLists(
        mapPost((p) => ({
          ...p,
          likesCount: p.isLikedByCurrentUser
            ? p.likesCount - 1
            : p.likesCount + 1,
          isLikedByCurrentUser: !p.isLikedByCurrentUser,
        })),
      );
      return { snapshots };
    },
    onError: (_err, _vars, context) => {
      context?.snapshots.forEach(([key, data]) =>
        queryClient.setQueryData(key, data),
      );
    },
    onSettled: refreshAllLists,
  });

  const deletePostMutation = useMutation({
    mutationFn: () => api.delete(`/post/${post.id}`),
    onSuccess: () => {
      updateAllLists((posts) => posts.filter((p) => p.id !== post.id));
    },
    onSettled: refreshAllLists,
  });

  const editPostMutation = useMutation({
    mutationFn: (content: string) => api.put(`/post/${post.id}`, { content }),
    onSuccess: (response) => {
      updateAllLists(
        mapPost((p) => ({ ...p, content: response.data.content })),
      );
      setEditingPostId(null);
      setEditContent("");
    },
    onSettled: refreshAllLists,
  });

  const addCommentMutation = useMutation({
    mutationFn: (text: string) =>
      api.post(`/comment/${post.id}`, { content: text }),
    onSuccess: (response) => {
      const newComment: Comment = response.data;
      updateAllLists(
        mapPost((p) => ({
          ...p,
          comments: [...(p.comments || []), newComment],
        })),
      );
    },
    onSettled: refreshAllLists,
  });

  const handleAddComment = () => {
    const trimmed = commentText.trim();
    if (!trimmed) return;
    if (trimmed.length > 200) {
      alert("Comment must be less than 200 characters.");
      return;
    }
    addCommentMutation.mutate(trimmed);
    setCommentText("");
  };

  return (
    <article className="card mb-4 p-6">
      <div className="mb-3 flex items-center gap-3">
        <img
          src={
            post.profilePic
              ? optimizeCloudinaryUrl(post.profilePic, 80)
              : "/default-avatar.png"
          }
          width={40}
          height={40}
          alt={`${post.username}'s avatar`}
          className="h-10 w-10 rounded-full object-cover"
        />
        <a
          href={`/user/${post.userId}`}
          className="text-base font-bold text-ink-deep"
        >
          {post.username}
        </a>
        <span className="ml-auto text-xs text-steel">
          {new Date(post.createdAt).toLocaleDateString()}
        </span>
        {isOwner && (
          <div className="relative">
            <button
              onClick={() => setOpenMenu(!openMenu)}
              aria-label="Post options"
              className="flex h-10 w-10 items-center justify-center rounded-full text-steel active:bg-surface-soft"
            >
              •••
            </button>
            {openMenu && (
              <div className="absolute right-0 z-10 mt-1 w-36 overflow-hidden rounded-xl border border-hairline-soft bg-white shadow-panel">
                <button
                  onClick={() => {
                    setEditingPostId(post.id);
                    setEditContent(post.content);
                    setOpenMenu(false);
                  }}
                  className="w-full px-4 py-2.5 text-left text-sm font-bold text-ink active:bg-surface-soft"
                >
                  Edit
                </button>
                <button
                  onClick={() => {
                    deletePostMutation.mutate();
                    setOpenMenu(false);
                  }}
                  className="w-full px-4 py-2.5 text-left text-sm font-bold text-critical active:bg-surface-soft"
                >
                  Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {isEditing ? (
        <div className="mb-3">
          <textarea
            className="input mb-3 h-auto py-3"
            rows={3}
            value={editContent}
            onChange={(e) => setEditContent(e.target.value)}
          />
          <div className="flex gap-2">
            <button
              onClick={() => editPostMutation.mutate(editContent)}
              disabled={editPostMutation.isPending}
              className="btn-primary btn-compact"
            >
              {editPostMutation.isPending ? "Saving..." : "Save"}
            </button>
            <button
              onClick={() => setEditingPostId(null)}
              className="btn-ghost btn-compact"
            >
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <>
          <p className="mb-3 text-base text-ink">{post.content}</p>
          {post.imageUrl && (
            <div
              className="relative mb-3 w-full overflow-hidden rounded-3xl bg-surface-soft"
              style={{ aspectRatio: "16/9" }}
            >
              <img
                src={optimizeCloudinaryUrl(post.imageUrl, 700, 394)}
                fetchPriority="high"
                alt={`Post by ${post.username}`}
                className="h-full w-full object-cover"
              />
            </div>
          )}
        </>
      )}

      <button
        onClick={() => toggleLikeMutation.mutate()}
        className={`pill-tab ${post.isLikedByCurrentUser ? "pill-tab-active" : ""}`}
      >
        ♥ {post.likesCount}
      </button>

      <div className="mt-4">
        <input
          value={commentText}
          placeholder="Add a comment..."
          className="search-pill"
          onChange={(e) => setCommentText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleAddComment();
          }}
        />
      </div>

      {comments.length > 0 && (
        <div className="mt-4 space-y-2 border-t border-hairline-soft pt-4">
          {visibleComments.map((comment) => (
            <div
              key={comment.id}
              className="flex items-start gap-2 rounded-xl bg-surface-soft p-3"
            >
              <img
                src={comment.profilePic || "/default-avatar.png"}
                className="mt-0.5 h-6 w-6 rounded-full object-cover"
              />
              <div className="flex flex-col">
                <span className="text-xs font-bold text-ink-deep">
                  {comment.username}
                </span>
                <p className="text-sm text-charcoal">{comment.content}</p>
              </div>
              <span className="ml-auto text-xs text-steel">
                {new Date(comment.createdAt).toLocaleDateString()}
              </span>
            </div>
          ))}
          {comments.length > 2 && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="mt-1 text-sm font-bold text-primary-deep underline-offset-2 hover:underline"
            >
              {isExpanded
                ? "Show less"
                : `See ${comments.length - 2} more comments`}
            </button>
          )}
        </div>
      )}
    </article>
  );
}
