import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
} from "@tanstack/react-query";
import api from "../Api";
import type { Post } from "../types/Home";

export const POSTS_PAGE_SIZE = 20;

export type PostPages = InfiniteData<Post[], number | undefined>;

/**
 * Loads a list of posts page by page from the API's cursor pagination
 * (`?beforeId=<last post id>&limit=N`). Returns the flattened list plus
 * the usual infinite-query controls.
 */
export function usePostList(queryKey: QueryKey, url: string, enabled = true) {
  const query = useInfiniteQuery({
    queryKey,
    enabled,
    initialPageParam: undefined as number | undefined,
    queryFn: async ({ pageParam }) => {
      const response = await api.get<Post[]>(url, {
        params: { beforeId: pageParam, limit: POSTS_PAGE_SIZE },
      });
      return response.data;
    },
    // A short page means we've reached the end.
    getNextPageParam: (lastPage) =>
      lastPage.length < POSTS_PAGE_SIZE
        ? undefined
        : lastPage[lastPage.length - 1].id,
  });

  const posts = query.data?.pages.flat() ?? [];
  return { ...query, posts };
}

/** Apply `updater` to the posts in a cached list, whether it's paged or a plain array. */
export function mapCachedPosts(
  old: PostPages | Post[] | undefined,
  updater: (posts: Post[]) => Post[],
): PostPages | Post[] | undefined {
  if (!old) return old;
  if (Array.isArray(old)) return updater(old);
  return { ...old, pages: old.pages.map((page) => updater(page)) };
}
