import { useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";
import axios from "axios";

import { AuthContext } from "./AuthContext.js";
import type { AuthContextType, DecodedToken, User } from "../types/Auth.ts";

function getUserFromToken() {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const decoded: DecodedToken = jwtDecode(token);
    const currentTime = Date.now() / 1000;

    if (decoded.exp < currentTime) {
      localStorage.removeItem("token");
      return null;
    }

    return {
      userId: decoded.userId,
      username: decoded.username,
      email: decoded.email,
    };
  } catch (error) {
    console.error("Failed to decode token:", error);
    return null;
  }
}

export default function AuthProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const [user, setUser] = useState<User | null>(getUserFromToken);

  const login = (token: string) => {
    localStorage.setItem("token", token);
    const decoded: DecodedToken = jwtDecode(token);
    setUser({
      userId: decoded.userId,
      username: decoded.username,
      email: decoded.email,
    });
  };

  useEffect(() => {
    if (!user) {
      axios
        .post(
          `${import.meta.env.VITE_API_URL}/api/auth/refresh`,
          {},
          { withCredentials: true },
        )
        .then((res) => {
          const decoded: DecodedToken = jwtDecode(res.data.accessToken);
          localStorage.setItem("token", res.data.accessToken);
          setUser({
            userId: decoded.userId,
            username: decoded.username,
            email: decoded.email,
          });
        })
        .catch((error) => {
          console.error("Failed to refresh token:", error);
          // no valid refresh cookie either — genuinely logged out
        });
    }
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
      localStorage.removeItem("token");
      setUser(null);
    }
  };

  const value: AuthContextType = { user, login, logout, setUser };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
