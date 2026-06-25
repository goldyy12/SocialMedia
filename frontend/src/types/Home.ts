export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  userId: number;
  username: string;
  profilePic: string | null;
}

export interface Post {
  id: number;
  content: string;
  imageUrl: string | null;
  createdAt: string;
  userId: number;
  username: string;
  profilePic: string | null;
  likesCount: number;
  isLikedByCurrentUser: boolean;
  comments: Comment[];
}
