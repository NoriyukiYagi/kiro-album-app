import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  constructor(private snackBar: MatSnackBar) {}

  handleError(error: any): void {
    let message = 'エラーが発生しました';
    
    if (error instanceof Error) {
      message = error.message;
    } else if (typeof error === 'string') {
      message = error;
    } else if (error?.error?.message) {
      message = error.error.message;
    }

    this.showError(message);
    console.error('Error handled:', error);
  }

  showError(message: string): void {
    this.snackBar.open(message, '閉じる', {
      duration: 5000,
      panelClass: ['error-snackbar'],
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }

  showSuccess(message: string): void {
    this.snackBar.open(message, '閉じる', {
      duration: 3000,
      panelClass: ['success-snackbar'],
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }

  showInfo(message: string): void {
    this.snackBar.open(message, '閉じる', {
      duration: 4000,
      panelClass: ['info-snackbar'],
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  }
}