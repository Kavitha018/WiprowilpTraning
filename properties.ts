import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PropertySearchRequest, Property } from '../../core/models/property.model';
import { PropertyService } from '../../core/services/property.service';

@Component({
  selector: 'app-properties',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './properties.html',
  styleUrls: ['./properties.scss']
})
export class PropertiesComponent implements OnInit {
  searchParams: PropertySearchRequest = {
    location: '',
    checkInDate: '',
    checkOutDate: '',
    maxGuests: undefined,
    page: 1,
    pageSize: 12
  };

  properties: Property[] = [];
  isLoading = false;
  totalCount = 0;
  currentPage = 1;
  pageSize = 12;

  constructor(
    private propertyService: PropertyService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Get search parameters from query string
    this.route.queryParams.subscribe(params => {
      this.searchParams.location = params['location'] || '';
      this.searchParams.checkInDate = params['checkInDate'] || '';
      this.searchParams.checkOutDate = params['checkOutDate'] || '';
      this.searchParams.maxGuests = params['maxGuests'] ? +params['maxGuests'] : undefined;
      this.searchParams.page = params['page'] ? +params['page'] : 1;
      this.searchParams.pageSize = this.pageSize;
      
      this.loadProperties();
    });
  }

  loadProperties(): void {
    this.isLoading = true;
    this.propertyService.getProperties(this.searchParams).subscribe({
      next: (response) => {
        this.properties = response.properties || [];
        this.totalCount = response.totalCount || 0;
        this.currentPage = this.searchParams.page || 1;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading properties:', error);
        this.isLoading = false;
      }
    });
  }

  searchProperties(): void {
    this.searchParams.page = 1; // Reset to first page
    this.updateUrl();
    this.loadProperties();
  }

  clearFilters(): void {
    this.searchParams = {
      location: '',
      checkInDate: '',
      checkOutDate: '',
      maxGuests: undefined,
      page: 1,
      pageSize: this.pageSize
    };
    this.updateUrl();
    this.loadProperties();
  }

  onPageChange(page: number): void {
    this.searchParams.page = page;
    this.updateUrl();
    this.loadProperties();
  }

  viewProperty(propertyId: number): void {
    this.router.navigate(['/properties', propertyId]);
  }

  private updateUrl(): void {
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
    if (this.searchParams.page && this.searchParams.page > 1) {
      queryParams.page = this.searchParams.page;
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: queryParams,
      queryParamsHandling: 'merge'
    });
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pages(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }
}
