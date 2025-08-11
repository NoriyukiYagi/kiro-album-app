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
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
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

  navigateToUpload(): void {
    this.router.navigate(['/upload']);
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