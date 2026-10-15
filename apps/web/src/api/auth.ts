
import { request } from './client';

export interface AuthUser {
  id: number;
  fullName: string;
  email: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: AuthUser;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export const authApi = {
  login(credentials: LoginCredentials): Promise<LoginResponse> {
    return request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  },
};
