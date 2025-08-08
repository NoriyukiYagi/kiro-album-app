export interface User {
  id: number;
  googleId: string;
  email: string;
  name: string;
  isAdmin: boolean;
  createdAt: Date;
  lastLoginAt: Date;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LoginRequest {
  googleToken: string;
}