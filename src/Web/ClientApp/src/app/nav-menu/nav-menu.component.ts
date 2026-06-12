import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from 'src/api-authorization/auth.service';
import { InsightsService } from '../services/insights.service';
import { UsersClient } from '../web-api-client';

@Component({
  standalone: false,
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})
export class NavMenuComponent {
  isAuthenticated$ = this.authService.isAuthenticated$;
  userInitials$: Observable<string>;

  constructor(
    private authService: AuthService,
    private router: Router,
    public insightsService: InsightsService,
    private usersClient: UsersClient
  ) {
    this.userInitials$ = this.usersClient.me().pipe(
      map(u => {
        const f = (u.firstName ?? '')[0]?.toUpperCase() ?? '';
        const l = (u.lastName  ?? '')[0]?.toUpperCase() ?? '';
        return f + l || '?';
      }),
      catchError(() => of('?'))
    );
  }

  logout(event: Event): void {
    event.preventDefault();
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login'])
    });
  }
}
