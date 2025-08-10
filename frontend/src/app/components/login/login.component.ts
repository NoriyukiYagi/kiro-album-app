import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  isLoading = false;
  hasError = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    // Check if user is already authenticated
    this.authService.isAuthenticated$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isAuthenticated => {
        if (isAuthenticated) {
          this.router.navigate(['/album']);
        } else {
          // Reset loading state when not authenticated
          this.isLoading = false;
        }
      });

    // Listen for login errors
    this.authService.loginError$
      .pipe(takeUntil(this.destroy$))
      .subscribe(error => {
        if (error) {
          this.isLoading = false;
          this.hasError = true;
          this.showError(error);
        }
      });

    // Initialize Google OAuth
    this.initializeGoogleAuth();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private async initializeGoogleAuth(): Promise<void> {
    try {
      await this.authService.initializeGoogleAuth();
      this.renderGoogleButton();
    } catch (error) {
      console.error('Failed to initialize Google Auth:', error);
      this.hasError = true;
      this.showError('Google認証の初期化に失敗しました');
    }
  }

  private renderGoogleButton(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      // Wait for DOM to be ready
      setTimeout(() => {
        const buttonElement = document.getElementById('google-signin-button');
        if (buttonElement) {
          google.accounts.id.renderButton(buttonElement, {
            theme: 'outline',
            size: 'large',
            text: 'signin_with',
            shape: 'rectangular',
            logo_alignment: 'left'
          });
        }
      }, 100);
    }
  }

  loginWithGoogle(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      // Clear previous errors and start loading
      this.hasError = false;
      this.authService.clearLoginError();
      this.isLoading = true;

      google.accounts.id.prompt((notification: any) => {
        if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
          // Fallback to manual sign-in
          this.isLoading = false;
          this.hasError = true;
          this.showError('Google認証がブロックされました。ポップアップブロッカーを無効にしてください。');
        }
      });
    } else {
      this.hasError = true;
      this.showError('Google認証が利用できません');
    }
  }

  /**
   * Retry login - clear error state and re-initialize
   */
  retryLogin(): void {
    this.hasError = false;
    this.authService.clearLoginError();
    this.initializeGoogleAuth();
  }

  private showError(message: string): void {
    this.snackBar.open(message, '閉じる', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }
}