import { Component, OnInit, signal } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ChatHubService } from './services/chat-hub.service';
import { AuthService } from 'src/api-authorization/auth.service';

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  isChatPage = false;
  isAuthenticated = false;

  showPreview   = signal(true);
  consentChecked = signal(false);
  hasConsent    = signal(typeof localStorage !== 'undefined' && localStorage.getItem('ab-chat-consent') === 'true');

  constructor(
    public chatHub: ChatHubService,
    private router: Router,
    private authService: AuthService
  ) {
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => {
        this.isChatPage = e.urlAfterRedirects === '/chat';
      });
  }

  ngOnInit() {
    this.authService.isAuthenticated$.subscribe(async isAuth => {
      this.isAuthenticated = isAuth;
      if (isAuth) {
        await this.chatHub.connect();
      } else {
        await this.chatHub.disconnect();
        this.chatHub.clear();
      }
    });
  }

  giveConsent(): void {
    localStorage.setItem('ab-chat-consent', 'true');
    this.hasConsent.set(true);
  }
}
