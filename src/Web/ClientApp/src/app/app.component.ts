import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ChatHubService } from './services/chat-hub.service';
import { AuthService } from 'src/api-authorization/auth.service';

const EXPRESSIONS = ['happy', 'excited', 'curious', 'surprised', 'winking', 'thinking'] as const;
type Expression = typeof EXPRESSIONS[number];

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit, OnDestroy {
  isChatPage = false;
  isAuthenticated = false;
  faceExpression: Expression = 'happy';

  private expressionTimer?: ReturnType<typeof setTimeout>;

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
        this.scheduleNextExpression();
      } else {
        await this.chatHub.disconnect();
        this.chatHub.clear();
        clearTimeout(this.expressionTimer);
      }
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.expressionTimer);
  }

  private scheduleNextExpression(): void {
    const delay = 3000 + Math.random() * 4000;
    this.expressionTimer = setTimeout(() => {
      const others = EXPRESSIONS.filter(e => e !== this.faceExpression);
      this.faceExpression = others[Math.floor(Math.random() * others.length)];
      this.scheduleNextExpression();
    }, delay);
  }
}
