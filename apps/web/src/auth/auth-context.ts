
import { createContext } from 'react';
import type {
  AuthUser,
  LoginCredentials,
} from '../api/auth';

export interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
}

export const AuthContext =
  createContext<AuthContextValue | null>(null);
