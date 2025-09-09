import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { tap } from "rxjs";

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = 'http://localhost:5000/api/auth';

  constructor(private http: HttpClient) {}

  login(username: string, password: string) {
    return this.http.post<any>(`${this.apiUrl}/login`, { username, password })
      .pipe(tap((response: { token: string; role: string; }) => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('role', response.role);
      }));
  }

  getToken() {
    return localStorage.getItem('token');
  }

  isAdmin() {
    return localStorage.getItem('role') === 'Admin';
  }

  logout() {
    localStorage.clear();
  }
}
