import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { LoadingComponent } from './shared/components/loading/loading.component';
import { AuthService } from './services/auth.service';
import { ErrorHandlerService } from './services/error-handler.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    LoadingComponent
  ],
  template: `
    <mat-toolbar color="primary">
      <span>Album App</span>
      <span class="spacer"></span>
      
      <div *ngIf="authService.isAuthenticated$ | async; else loginButton">
        <button mat-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
          {{ (authService.currentUser$ | async)?.name || 'ユーザー' }}
        </button>
        <mat-menu #userMenu="matMenu">
          <button mat-menu-item (click)="navigateToAlbum()">
            <mat-icon>photo_library</mat-icon>
            <span>アルバム</span>
          </button>
          <button mat-menu-item *ngIf="authService.isAdmin()" (click)="navigateToAdmin()">
            <mat-icon>admin_panel_settings</mat-icon>
            <span>ユーザー管理</span>
          </button>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>ログアウト</span>
          </button>
        </mat-menu>
      </div>
      
      <ng-template #loginButton>
        <button mat-button (click)="navigateToLogin()">
          <mat-icon>login</mat-icon>
          ログイン
        </button>
      </ng-template>
    </mat-toolbar>
    
    <main>
      <router-outlet></router-outlet>
    </main>
    
    <app-loading></app-loading>
  `,
  styles: [`
    .spacer {
      flex: 1 1 auto;
    }
    
    main {
      padding: 20px;
      min-height: calc(100vh - 64px);
    }
  `]
})
export class AppComponent implements OnInit {
  title = 'album-app';

  constructor(
    public authService: AuthService,
    private router: Router,
    private errorHandler: ErrorHandlerService
  ) {}

  ngOnInit(): void {
    // Initialize Google Auth when app starts
    this.authService.initializeGoogleAuth().catch(error => {
      console.error('Failed to initialize Google Auth:', error);
    });
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  navigateToAlbum(): void {
    this.router.navigate(['/album']);
  }

  navigateToAdmin(): void {
    this.router.navigate(['/admin']);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.errorHandler.showSuccess('ログアウトしました');
        this.router.navigate(['/login']);
      },
      error: (error) => {
        this.errorHandler.handleError(error);
        // Still navigate to login even if logout fails
        this.router.navigate(['/login']);
      }
    });
  }
}