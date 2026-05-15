import { Component, OnInit } from '@angular/core';
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
  isChatOpen = false;
  isChatPage = false;
  isAuthenticated = false;

  constructor(
    private chatHub: ChatHubService,
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
        this.isChatOpen = false;
      }
    });
  }

  toggleChat() {
    this.isChatOpen = !this.isChatOpen;
  }
}
