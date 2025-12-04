// src/context/AuthContext.jsx
import React, { createContext, useEffect, useState, useCallback } from "react";
import * as authService from "../services/auth";
import { setOnUnauthorized } from "../api/http";

export const AuthContext = createContext({ isAuthenticated: false, user: null, initializing: true, login: async ()=>{}, logout: ()=>{} });

export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);
  const [initializing, setInitializing] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const me = await authService.fetchMe();
        if (!mounted) return;
        setIsAuthenticated(true);
        setUser({ id: me.id, name: me.userName, email: me.email, roles: me.roles ?? [] });
      } catch {
        setIsAuthenticated(false);
        setUser(null);
      } finally {
        if (mounted) setInitializing(false);
      }
    })();
    return () => { mounted = false; };
  }, []);

  const login = useCallback(async (credentials) => {
    const data = await authService.login(credentials);
    setIsAuthenticated(true);
    setUser({ id: data.id, name: data.userName, email: data.email, roles: data.roles ?? [] });
    return data;
  }, []);

  const logout = useCallback(async () => {
    try {
      await authService.logoutRequest?.();
    } catch {
      // ignore backend errors; continue clearing client state
    }
    setIsAuthenticated(false);
    setUser(null);
  }, []);

  useEffect(() => {
    setOnUnauthorized(() => {
      // не редиректим, если мы уже на странице логина — чтобы избежать цикла
      const path = window.location.pathname;
      if (path === "/login" || path === "/register") {
        // просто почистим состояние, без редиректа
        logout();
        return;
      }
      logout();
      window.location.href = "/login";
    });
    return () => setOnUnauthorized(null);
  }, [logout]);

  return <AuthContext.Provider value={{ isAuthenticated, user, initializing, login, logout }}>{children}</AuthContext.Provider>;
}
