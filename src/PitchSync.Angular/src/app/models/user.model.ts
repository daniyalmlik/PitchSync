export interface UserInfo {
  id: string;
  email: string;
  displayName: string;
  favoriteTeam?: string;
  avatarUrl?: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  favoriteTeam?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface TokenResponse {
  token: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UpdateProfileRequest {
  displayName?: string;
  favoriteTeam?: string;
  avatarUrl?: string;
}
