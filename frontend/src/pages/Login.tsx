import React, { useEffect, useRef, useState } from "react";
import api from "../Api";
import { useAuth } from "../context/useAuth";
import { useNavigate } from "react-router-dom";
import axios from "axios";

declare global {
  interface Window {
    google: any;
  }
}

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const { login } = useAuth();
  const [isError, setIsError] = useState<string | null>(null);
  const navigate = useNavigate();
  const googleButtonRef = useRef<HTMLDivElement>(null);

  const handleGoogleResponse = async (response: { credential: string }) => {
    try {
      setPending(true);
      const res = await api.post("/auth/google", {
        idToken: response.credential,
      });
      login(res.data.accessToken);
      setPending(false);
      navigate("/");
    } catch (error) {
      console.error("Google login failed:", error);
      if (axios.isAxiosError(error)) {
        setIsError(
          error.response?.data?.error ??
            "Google sign-in failed. Please try again.",
        );
      } else {
        setIsError("Google sign-in failed. Please try again.");
      }
      setPending(false);
    }
  };

  useEffect(() => {
    if (!window.google || !googleButtonRef.current) return;

    window.google.accounts.id.initialize({
      client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
      callback: handleGoogleResponse,
    });

    window.google.accounts.id.renderButton(googleButtonRef.current, {
      theme: "outline",
      size: "large",
      width: 320,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    try {
      setPending(true);
      if (!email || !password) {
        alert("Please enter both email and password.");
        return;
      }
      if (password.length < 6) {
        alert("Password must be at least 6 characters long.");
        return;
      }

      const response = await api.post("/auth/login", { email, password });
      login(response.data.accessToken);
      setPending(false);

      navigate("/");
    } catch (error) {
      console.error("Login failed:", error);
      if (axios.isAxiosError(error)) {
        setIsError(
          error.response?.data?.error ?? "Login failed. Please try again.",
        );
        setPending(false);
      } else {
        setIsError("Login failed. Please try again.");
        setPending(false);
      }
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-4rem)]">
      <form
        onSubmit={handleSubmit}
        className="bg-white dark:bg-slate-900 border border-gray-100 dark:border-slate-800 p-8 rounded-xl shadow-sm w-full max-w-sm transition-colors duration-200"
      >
        <h2 className="text-2xl font-bold mb-6 text-center text-gray-900 dark:text-white">
          Login
        </h2>

        <div className="mb-4">
          <label
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
            htmlFor="email"
          >
            Email
          </label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full border border-gray-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-gray-900 dark:text-white rounded-lg py-2 px-4 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-colors"
            placeholder="name@example.com"
          />
        </div>

        <div className="mb-6">
          <label
            className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
            htmlFor="password"
          >
            Password
          </label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full border border-gray-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-gray-900 dark:text-white rounded-lg py-2 px-4 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-colors"
            placeholder="••••••••"
          />
        </div>

        <button
          type="submit"
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-2.5 px-4 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-slate-900 transition-colors"
          disabled={pending}
        >
          {pending ? "Logging in..." : "Login"}
        </button>

        <div className="flex items-center gap-3 my-5">
          <div className="flex-1 h-px bg-gray-200 dark:bg-slate-700" />
          <span className="text-xs text-gray-400 dark:text-gray-500">OR</span>
          <div className="flex-1 h-px bg-gray-200 dark:bg-slate-700" />
        </div>

        <div ref={googleButtonRef} className="flex justify-center" />

        {isError && (
          <p className="text-sm text-red-500 dark:text-red-400 mt-4 text-center bg-red-50 dark:bg-red-950/30 py-2 rounded-lg">
            {isError}
          </p>
        )}
        <p className="text-sm text-gray-600 dark:text-gray-400 mt-4">
          Don't have an account?{" "}
          <a
            href="/register"
            className="text-blue-600 dark:text-blue-400 hover:underline"
          >
            Register here
          </a>
        </p>
      </form>
    </div>
  );
}
