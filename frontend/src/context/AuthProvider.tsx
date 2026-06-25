import { useState } from "react";
import { jwtDecode } from "jwt-decode";

import { AuthContext } from "./AuthContext.js";
import type { AuthContextType, DecodedToken, User } from "../types/Auth.ts";

function getUserFromToken() {
  const token = localStorage.getItem("token");
  if (!token) return null;
  const decoded: DecodedToken = jwtDecode(token);

  return {
    userId: decoded.userId,
    username: decoded.username,
    email: decoded.email,
  };
}

export default function AuthProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const [user, setUser] = useState<User | null>(getUserFromToken);

  const login = (token: string) => {
    localStorage.setItem("token", token);
    console.log("Token stored in localStorage:", token);
    console.log("User from token:", getUserFromToken());
    const decoded: DecodedToken = jwtDecode(token);
    setUser({
      userId: decoded.userId,
      username: decoded.username,
      email: decoded.email,
    });
  };

  const logout = () => {
    localStorage.removeItem("token");
    setUser(null);
  };
  const value: AuthContextType = { user, login, logout, setUser };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
