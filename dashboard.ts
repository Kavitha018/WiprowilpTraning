import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { User } from '../../core/models/user.model';

interface DashboardStats {
  totalReservations: number;
  activeReservations: number;
  totalProperties: number;
  unreadMessages: number;
}

interface RecentActivity {
  id: number;
  title: string;
  description: string;
  date: string;
  type: 'success' | 'warning' | 'info';
  status: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  currentUser: User | null = null;
  currentDate = new Date();
  stats: DashboardStats = {
    totalReservations: 0,
    activeReservations: 0,
    totalProperties: 0,
    unreadMessages: 0
  };
  recentActivity: RecentActivity[] = [];

  constructor(
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUserValue();
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    if (!this.currentUser) return;

    // For now, just show sample data
    this.stats = {
      totalReservations: 5,
      activeReservations: 2,
      totalProperties: this.authService.isOwner() ? 3 : 0,
      unreadMessages: 1
    };

    this.recentActivity = [
      {
        id: 1,
        title: 'Welcome to RentAPlace!',
        description: 'Your account has been created successfully',
        date: new Date().toISOString(),
        type: 'success',
        status: 'Completed'
      }
    ];
  }
}