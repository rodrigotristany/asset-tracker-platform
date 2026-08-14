import { create } from "zustand";
import { api } from "../api";
import type { AuthState } from "../types";

interface AuthStore extends AuthState {
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  isAuthenticated: false,
  token: undefined,
  login: async (username, password) => {
    const authState = await api.login(username, password);
    set(authState);
  },
  logout: () => {
    set({ isAuthenticated: false, token: undefined });
  },
}));
