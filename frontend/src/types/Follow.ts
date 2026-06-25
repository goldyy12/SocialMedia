export interface Follower {
  followerId: number;
  username: string;
  profilePic: string | null;
}

export interface Following {
  followingId: number;
  username: string;
  profilePic: string | null;
}

export interface FollowRequest {
  id: number;
  followerId: number;
  username: string;
  profilePic: string | null;
  createdAt: string;
}
