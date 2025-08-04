import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">
        <mat-card-header>
          <mat-card-title>Album App にログイン</mat-card-title>
        </mat-card-header>
        
        <mat-card-content>
          <p>Google アカウントでログインしてください</p>
        </mat-card-content>
        
        <mat-card-actions>
          <button mat-raised-button color="primary" (click)="loginWithGoogle()">
            <mat-icon>account_circle</mat-icon>
            Google でログイン
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 60vh;
    }
    
    .login-card {
      max-width: 400px;
      width: 100%;
      text-align: center;
    }
    
    mat-card-actions {
      display: flex;
      justify-content: center;
    }
  `]
})
export class LoginComponent {
  loginWithGoogle() {
    // TODO: Implement Google OAuth login
    console.log('Google login clicked');
  }
}