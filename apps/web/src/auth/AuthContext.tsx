
import {
  useEffect,
  useState,
  type ReactNode,
} from 'react';

import {
  authApi,
  type LoginCredentials,
  type LoginResponse,
} from '../api/auth';

import {
  clearAccessToken,
  setAccessToken,
} from '../api/tokenStore';

import { AuthContext } from './auth-context';

export function AuthProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [session, setSession] =
    useState<LoginResponse | null>(null);

  const logout = () => {
    clearAccessToken();
    setSession(null);
  };

  const login = async (credentials: LoginCredentials) => {
    const response = await authApi.login(credentials);

    const expiry = Date.parse(response.expiresAtUtc);

    if (!Number.isFinite(expiry) || expiry <= Date.now()) {
      throw new Error('The authentication token has expired.');
    }

    setAccessToken(response.token, response.expiresAtUtc);
    setSession(response);
  };


useEffect(() => {
  if (!session) {
    return;
  }

  const remaining =
    Date.parse(session.expiresAtUtc) - Date.now();

  // Schedule expiry handling asynchronously.
  // A zero delay handles tokens that have already expired.
  const timeout = window.setTimeout(() => {
    clearAccessToken();
    setSession(null);
  }, Math.max(0, Math.min(remaining, 2147483647)));

  return () => {
    window.clearTimeout(timeout);
  };
}, [session]);

  return (
    <AuthContext.Provider
      value={{
        user: session?.user ?? null,
        isAuthenticated: session !== null,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
