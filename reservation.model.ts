export interface Reservation {
  id: number;
  userId: number;
  userName: string;
  userEmail: string;
  propertyId: number;
  propertyTitle: string;
  propertyLocation: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfGuests: number;
  totalAmount: number;
  status: string;
  specialRequests?: string;
  createdAt: string;
  confirmedAt?: string;
}

export interface CreateReservationRequest {
  propertyId: number;
  checkInDate: string;
  checkOutDate: string;
  numberOfGuests: number;
  specialRequests?: string;
}

export interface UpdateReservationStatusRequest {
  status: string;
}

