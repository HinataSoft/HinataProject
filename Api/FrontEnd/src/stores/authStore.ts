import { create } from 'zustand';
import { User } from '../types';

const getStoredUser = (): User | null => {
  const stored = localStorage.getItem('user');
  return stored ? JSON.parse(stored) : null;
};

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
}

const storedUser = getStoredUser();
const storedToken = localStorage.getItem('access_token');

export const useAuthStore = create<AuthState>((set) => ({
  user: storedUser,
  token: storedToken,
  isAuthenticated: !!storedToken,
  isAdmin: storedUser?.rights === 'Admin' || false,
  login: (token: string, user: User) => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('user', JSON.stringify(user));
    // Check if user has Admin rights
    const isAdmin = user.rights === 'Admin';
    set({ token, user, isAuthenticated: true, isAdmin });
  },
  logout: () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    set({ token: null, user: null, isAuthenticated: false, isAdmin: false });
  },
}));
