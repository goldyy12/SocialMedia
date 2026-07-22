export interface User {
  userId: number;
  username: string;
  email: string;
}
export interface DecodedToken {
  userId: number;
  username: string;
  email: string;
  exp: number;
}
export interface AuthContextType {
  user: User | null;
  login: (token: string) => void;
  logout: () => void;
  setUser: React.Dispatch<React.SetStateAction<User | null>>;
  loading: boolean;
}
