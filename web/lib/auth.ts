"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

const TOKEN_KEY = "dt_access_token";
const REFRESH_KEY = "dt_refresh_token";
const USER_KEY = "dt_user";

// ---------------------------------------------------------------------------
// Token helpers (localStorage-based)
// ---------------------------------------------------------------------------

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function setRefreshToken(refreshToken: string): void {
  localStorage.setItem(REFRESH_KEY, refreshToken);
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(REFRESH_KEY);
}

export function setUser(user: { id: string; username: string; roles: string[] }): void {
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function getUser(): { id: string; username: string; roles: string[] } | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
}

export function isAuthenticated(): boolean {
  return !!getToken();
}

// ---------------------------------------------------------------------------
// useAuth hook — redirects to /login if not authenticated
// ---------------------------------------------------------------------------

export function useAuth() {
  const router = useRouter();
  const [checked, setChecked] = useState(false);
  const [user, setUserState] = useState<{
    id: string;
    username: string;
    roles: string[];
  } | null>(null);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
    } else {
      setUserState(getUser());
      setChecked(true);
    }
  }, [router]);

  const logout = () => {
    clearToken();
    router.replace("/login");
  };

  return { checked, user, logout };
}
