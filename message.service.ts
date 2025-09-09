import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Message, SendMessageRequest, Conversation } from '../models/message.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) { }

  sendMessage(message: SendMessageRequest): Observable<Message> {
    return this.http.post<Message>(`${this.API_URL}/messages`, message);
  }

  getMessage(id: number): Observable<Message> {
    return this.http.get<Message>(`${this.API_URL}/messages/${id}`);
  }

  getConversations(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.API_URL}/messages/conversations`);
  }

  getConversation(otherUserId: number): Observable<Conversation> {
    return this.http.get<Conversation>(`${this.API_URL}/messages/conversation/${otherUserId}`);
  }

  markMessageAsRead(id: number): Observable<any> {
    return this.http.put(`${this.API_URL}/messages/${id}/read`, {});
  }
}
