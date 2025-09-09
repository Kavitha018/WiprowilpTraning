import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  Property, 
  CreatePropertyRequest, 
  UpdatePropertyRequest, 
  PropertySearchRequest, 
  PropertySearchResponse 
} from '../models/property.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getProperties(searchParams?: PropertySearchRequest): Observable<PropertySearchResponse> {
    let params = new HttpParams();
    
    if (searchParams) {
      Object.keys(searchParams).forEach(key => {
        const value = searchParams[key as keyof PropertySearchRequest];
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            value.forEach(item => params = params.append(key, item.toString()));
          } else {
            params = params.set(key, value.toString());
          }
        }
      });
    }

    return this.http.get<PropertySearchResponse>(`${this.API_URL}/properties`, { params });
  }

  getProperty(id: number): Observable<Property> {
    return this.http.get<Property>(`${this.API_URL}/properties/${id}`);
  }

  createProperty(property: CreatePropertyRequest): Observable<Property> {
    return this.http.post<Property>(`${this.API_URL}/properties`, property);
  }

  updateProperty(id: number, property: UpdatePropertyRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/properties/${id}`, property);
  }

  deleteProperty(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/properties/${id}`);
  }

  getMyProperties(): Observable<Property[]> {
    return this.http.get<Property[]>(`${this.API_URL}/properties/my-properties`);
  }
}
