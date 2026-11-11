import { useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";
import axios from "axios";

import { AuthContext } from "./AuthContext.js";
import type { AuthContextType, DecodedToken, User } from "../types/Auth.ts";
import { refreshAccessToken } from "../Api";
import { setAccessToken } from "../tokenStore";

function userFromToken(token: string): User {
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
  const [user, setUser] = useState<User | null>(null);
  // The access token is kept in memory only, so on every page load we ask the
  // server for a fresh one using the httpOnly refresh cookie.
  const [loading, setLoading] = useState<boolean>(true);

  const login = (token: string) => {
    setAccessToken(token);
    setUser(userFromToken(token));
  };

  useEffect(() => {
    refreshAccessToken()
      .then((token) => setUser(userFromToken(token)))
      .catch(() => {
        // No valid refresh cookie — the user simply isn't logged in.
        setAccessToken(null);
      })
      .finally(() => setLoading(false));
  }, []);

  const logout = async () => {
    try {
      await axios.post(
        `${import.meta.env.VITE_API_URL}/api/auth/logout`,
        {},
        { withCredentials: true },
      );
    } catch (error) {
      console.error("Logout request failed:", error);
    } finally {
      setAccessToken(null);
      setUser(null);
    }
  };

  const value: AuthContextType = { user, login, logout, setUser, loading };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
