// src/components/ProtectedRoute.jsx
import React, { useContext } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

export default function ProtectedRoute({ children, requiredRole }) {
  const { isAuthenticated, user, initializing } = useContext(AuthContext);
  const location = useLocation();

  if (initializing) return <div className="d-flex justify-content-center py-5">Загрузка...</div>;
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />;

  if (requiredRole) {
    const roles = user?.roles ?? [];
    if (!roles.includes(requiredRole)) return <Navigate to="/" replace />;
  }

  return children;
}
