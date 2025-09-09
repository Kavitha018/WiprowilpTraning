export interface Property {
  id: number;
  ownerId: number;
  ownerName: string;
  title: string;
  description: string;
  location: string;
  type: string;
  features: string[];
  pricePerNight: number;
  maxGuests: number;
  bedrooms: number;
  bathrooms: number;
  isAvailable: boolean;
  createdAt: string;
  imageUrls: string[];
  primaryImageUrl: string;
}

export interface CreatePropertyRequest {
  title: string;
  description: string;
  location: string;
  type: string;
  features: string[];
  pricePerNight: number;
  maxGuests: number;
  bedrooms: number;
  bathrooms: number;
}

export interface UpdatePropertyRequest {
  title?: string;
  description?: string;
  location?: string;
  type?: string;
  features?: string[];
  pricePerNight?: number;
  maxGuests?: number;
  bedrooms?: number;
  bathrooms?: number;
  isAvailable?: boolean;
}

export interface PropertySearchRequest {
  location?: string;
  type?: string;
  checkInDate?: string;
  checkOutDate?: string;
  maxGuests?: number;
  minPrice?: number;
  maxPrice?: number;
  features?: string[];
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: string;
}

export interface PropertySearchResponse {
  properties: Property[];
  totalCount: number;
  page: number;
  pageSize: number;
}

