export interface User {
  id: number;
  googleId?: string;
  email: string;
  name: string;
  isAdmin: boolean;
  createdAt?: Date;
  lastLoginAt?: Date;
}

export interface UserInfo {
  id: number;
  email: string;
  name: string;
  isAdmin: boolean;
}

export interface AuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserInfo;
}

export interface LoginRequest {
  idToken: string;
}