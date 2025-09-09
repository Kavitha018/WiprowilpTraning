import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  Reservation, 
  CreateReservationRequest, 
  UpdateReservationStatusRequest 
} from '../models/reservation.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReservationService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) { }

  createReservation(reservation: CreateReservationRequest): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.API_URL}/reservations`, reservation);
  }

  getReservation(id: number): Observable<Reservation> {
    return this.http.get<Reservation>(`${this.API_URL}/reservations/${id}`);
  }

  updateReservationStatus(id: number, status: UpdateReservationStatusRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/reservations/${id}/status`, status);
  }

  getMyReservations(): Observable<Reservation[]> {
    return this.http.get<Reservation[]>(`${this.API_URL}/reservations/my-reservations`);
  }

  cancelReservation(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/reservations/${id}`);
  }
}
