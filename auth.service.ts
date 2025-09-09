import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { User, AuthResponse, LoginRequest, RegisterRequest } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = environment.apiUrl;
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  public isAuthenticated = signal(false);

  constructor(private http: HttpClient) {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    if (typeof window === 'undefined' || !window.localStorage) {
      return; // Skip if running on server side
    }
    
    const token = localStorage.getItem('token');
    const user = localStorage.getItem('user');
    
    if (token && user) {
      try {
        const userObj = JSON.parse(user);
        // Validate token is not expired
        if (this.isTokenValid(token)) {
          this.currentUserSubject.next(userObj);
          this.isAuthenticated.set(true);
        } else {
          this.clearAuthData();
        }
      } catch (error) {
        console.error('Error loading user from storage:', error);
        this.clearAuthData();
      }
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp > currentTime;
    } catch (error) {
      return false;
    }
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/login`, credentials)
      .pipe(
        tap(response => {
          this.setAuthData(response);
        })
      );
  }

  register(userData: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/register`, userData)
      .pipe(
        tap(response => {
          this.setAuthData(response);
        })
      );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.API_URL}/auth/logout`, {})
      .pipe(
        tap(() => {
          this.clearAuthData();
        })
      );
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.API_URL}/auth/me`);
  }

  getToken(): string | null {
    if (typeof window === 'undefined' || !window.localStorage) {
      return null;
    }
    return localStorage.getItem('token');
  }

  getCurrentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  isOwner(): boolean {
    const user = this.getCurrentUserValue();
    return user?.role === 'Owner';
  }

  isRenter(): boolean {
    const user = this.getCurrentUserValue();
    return user?.role === 'Renter';
  }

  // Method to check if user is authenticated (for guard)
  isUserAuthenticated(): boolean {
    return this.isAuthenticated();
  }

  private setAuthData(response: AuthResponse): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.setItem('token', response.token);
      localStorage.setItem('user', JSON.stringify(response.user));
    }
    this.currentUserSubject.next(response.user);
    this.isAuthenticated.set(true);
  }

  private clearAuthData(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
    this.currentUserSubject.next(null);
    this.isAuthenticated.set(false);
  }
}
