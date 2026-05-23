import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'src/api-authorization/auth.service';
import { InsightsService } from '../services/insights.service';

@Component({
  standalone: false,
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})
export class NavMenuComponent {
  isAuthenticated$ = this.authService.isAuthenticated$;

  constructor(
    private authService: AuthService,
    private router: Router,
    public insightsService: InsightsService
  ) {}

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login'])
    });
  }
}
