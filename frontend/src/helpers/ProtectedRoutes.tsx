import { Navigate, useLocation } from "react-router-dom";
import { type JSX } from "react";
import { useAuth } from "../context/useAuth.js";

export default function ProtectedRoute({
  children,
}: {
  children: JSX.Element;
}) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return <p className="text-center mt-8">Loading...</p>;
  }

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
}
