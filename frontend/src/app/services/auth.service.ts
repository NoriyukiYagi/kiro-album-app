import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { User, UserInfo, AuthResponse, LoginRequest, ApiResponse } from '../models/user.model';
import { environment } from '../../environments/environment';

declare const google: any;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = environment.apiUrl;
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'current_user';

  private currentUserSubject = new BehaviorSubject<UserInfo | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    const token = this.getToken();
    const user = this.getStoredUser();

    if (token && user) {
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
    }
  }

  initializeGoogleAuth(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (typeof google !== 'undefined' && google.accounts) {
        google.accounts.id.initialize({
          client_id: environment.googleClientId,
          callback: (response: any) => this.handleGoogleCallback(response),
          auto_select: false,
          cancel_on_tap_outside: true
        });
        resolve();
      } else {
        // Load Google Identity Services script
        const script = document.createElement('script');
        script.src = 'https://accounts.google.com/gsi/client';
        script.onload = () => {
          google.accounts.id.initialize({
            client_id: environment.googleClientId,
            callback: (response: any) => this.handleGoogleCallback(response),
            auto_select: false,
            cancel_on_tap_outside: true
          });
          resolve();
        };
        script.onerror = () => reject(new Error('Failed to load Google Identity Services'));
        document.head.appendChild(script);
      }
    });
  }

  private handleGoogleCallback(response: any): void {
    if (response.credential) {
      this.loginWithGoogle(response.credential).subscribe({
        next: (authResponse) => {
          console.log('Google login successful', authResponse);
          // Navigation will be handled by the component listening to isAuthenticated$
        },
        error: (error) => {
          console.error('Google login failed', error);
          // Clear any partial auth state
          this.clearAuthData();
          // Error will be handled by the component or interceptor
        }
      });
    }
  }

  loginWithGoogle(googleToken: string): Observable<AuthResponse> {
    const loginRequest: LoginRequest = { idToken: googleToken };

    return this.http.post<ApiResponse<AuthResponse>>(`${this.API_URL}/auth/google-login`, loginRequest)
      .pipe(
        map(apiResponse => {
          if (!apiResponse.success || !apiResponse.data) {
            throw new Error(apiResponse.message || 'Login failed');
          }
          return apiResponse.data;
        }),
        tap(response => {
          this.setToken(response.accessToken);
          this.setUser(response.user);
          this.currentUserSubject.next(response.user);
          this.isAuthenticatedSubject.next(true);
        }),
        catchError(error => {
          console.error('Login failed:', error);
          throw error;
        })
      );
  }

  logout(): Observable<any> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/auth/logout`, {})
      .pipe(
        map(apiResponse => {
          if (!apiResponse.success) {
            console.warn('Logout warning:', apiResponse.message);
          }
          return apiResponse;
        }),
        tap(() => {
          this.clearAuthData();
        }),
        catchError(error => {
          // Even if logout fails on server, clear local data
          this.clearAuthData();
          return of(null);
        })
      );
  }

  /**
   * Local logout without server call - used by interceptor to avoid infinite loops
   */
  logoutLocal(): void {
    this.clearAuthData();
  }

  getUserInfo(): Observable<UserInfo> {
    return this.http.get<ApiResponse<UserInfo>>(`${this.API_URL}/auth/user-info`)
      .pipe(
        map(apiResponse => {
          if (!apiResponse.success || !apiResponse.data) {
            throw new Error(apiResponse.message || 'Failed to get user info');
          }
          return apiResponse.data;
        }),
        tap(user => {
          this.setUser(user);
          this.currentUserSubject.next(user);
        }),
        catchError(error => {
          console.error('Failed to get user info:', error);
          this.clearAuthData();
          throw error;
        })
      );
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  private setUser(user: UserInfo): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private getStoredUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  }

  private clearAuthData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): UserInfo | null {
    return this.currentUserSubject.value;
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.isAdmin || false;
  }
}