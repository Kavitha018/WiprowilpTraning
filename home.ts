import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PropertySearchRequest, Property } from '../../core/models/property.model';
import { PropertyService } from '../../core/services/property.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit {
  searchParams: PropertySearchRequest = {
    location: '',
    checkInDate: '',
    checkOutDate: '',
    maxGuests: undefined,
    page: 1,
    pageSize: 10
  };

  properties: Property[] = [];
  isLoading = false;

  constructor(
    private router: Router,
    private propertyService: PropertyService
  ) {}

  ngOnInit(): void {
    this.loadProperties();
  }

  loadProperties(): void {
    this.isLoading = true;
    this.propertyService.getProperties().subscribe({
      next: (response) => {
        this.properties = response.properties || [];
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading properties:', error);
        this.isLoading = false;
      }
    });
  }

  searchProperties(): void {
    // Build query parameters
    const queryParams: any = {};
    
    if (this.searchParams.location) {
      queryParams.location = this.searchParams.location;
    }
    if (this.searchParams.checkInDate) {
      queryParams.checkInDate = this.searchParams.checkInDate;
    }
    if (this.searchParams.checkOutDate) {
      queryParams.checkOutDate = this.searchParams.checkOutDate;
    }
    if (this.searchParams.maxGuests) {
      queryParams.maxGuests = this.searchParams.maxGuests;
    }

    // Navigate to properties page with search parameters
    this.router.navigate(['/properties'], { queryParams });
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  navigateToRegister(): void {
    this.router.navigate(['/register']);
  }

  viewProperty(propertyId: number): void {
    this.router.navigate(['/properties', propertyId]);
  }
}