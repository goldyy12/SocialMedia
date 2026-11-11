interface LoadMoreButtonProps {
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  onClick: () => void;
}

export default function LoadMoreButton({
  hasNextPage,
  isFetchingNextPage,
  onClick,
}: LoadMoreButtonProps) {
  if (!hasNextPage) return null;
  return (
    <div className="mb-4 flex justify-center">
      <button
        onClick={onClick}
        disabled={isFetchingNextPage}
        className="btn-secondary"
      >
        {isFetchingNextPage ? "Loading..." : "Load more"}
      </button>
    </div>
  );
}
