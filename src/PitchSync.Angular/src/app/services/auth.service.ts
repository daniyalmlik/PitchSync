import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserInfo, LoginRequest, RegisterRequest, TokenResponse } from '../models/user.model';

const TOKEN_KEY = 'pitchsync_token';
const EXPIRES_KEY = 'pitchsync_expires';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.gatewayUrl;

  private readonly _currentUser$ = new BehaviorSubject<UserInfo | null>(null);
  readonly currentUser$ = this._currentUser$.asObservable();

  constructor() {
    this.hydrateFromStorage();
  }

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/api/auth/login`, { email, password } as LoginRequest).pipe(
      tap(res => this.storeSession(res))
    );
  }

  register(email: string, password: string, displayName: string, favoriteTeam?: string): Observable<TokenResponse> {
    const body: RegisterRequest = { email, password, displayName, ...(favoriteTeam ? { favoriteTeam } : {}) };
    return this.http.post<TokenResponse>(`${this.base}/api/auth/register`, body).pipe(
      tap(res => this.storeSession(res))
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    this._currentUser$.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getCurrentUser(): UserInfo | null {
    return this._currentUser$.value;
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const expires = localStorage.getItem(EXPIRES_KEY);
    if (!token || !expires) return false;
    return new Date(expires) > new Date();
  }

  private storeSession(res: TokenResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(EXPIRES_KEY, res.expiresAt);
    const user = this.decodeUser(res.token) ?? res.user;
    this._currentUser$.next(user);
  }

  private hydrateFromStorage(): void {
    if (!this.isAuthenticated()) return;
    const token = this.getToken()!;
    const user = this.decodeUser(token);
    this._currentUser$.next(user);
  }

  private decodeUser(token: string): UserInfo | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        id: payload['sub'] ?? payload['nameid'] ?? '',
        email: payload['email'] ?? '',
        displayName: payload['display_name'] ?? payload['name'] ?? '',
        favoriteTeam: payload['favorite_team'] ?? undefined,
        avatarUrl: payload['avatar_url'] ?? undefined,
      };
    } catch {
      return null;
    }
  }
}
