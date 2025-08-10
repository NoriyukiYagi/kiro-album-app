import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Add JWT token to requests
  const token = authService.getToken();
  let authReq = req;

  if (token) {
    authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
  }

  // Add Content-Type header for non-file uploads
  if (!authReq.headers.has('Content-Type') && !(authReq.body instanceof FormData)) {
    authReq = authReq.clone({
      headers: authReq.headers.set('Content-Type', 'application/json')
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      return handleError(error, authService, router);
    })
  );
};

function handleError(error: HttpErrorResponse, authService: AuthService, router: Router): Observable<never> {
  let errorMessage = 'An unknown error occurred';

  if (error.error instanceof ErrorEvent) {
    // Client-side error
    errorMessage = `Client Error: ${error.error.message}`;
  } else {
    // Server-side error
    switch (error.status) {
      case 401:
        // Unauthorized - redirect to login
        authService.logout().subscribe();
        router.navigate(['/login']);
        errorMessage = 'セッションが期限切れです。再度ログインしてください。';
        break;
      case 403:
        errorMessage = 'このリソースにアクセスする権限がありません。';
        break;
      case 404:
        errorMessage = 'リクエストされたリソースが見つかりません。';
        break;
      case 413:
        errorMessage = 'ファイルサイズが大きすぎます。100MB以下のファイルをアップロードしてください。';
        break;
      case 415:
        errorMessage = 'サポートされていないファイル形式です。JPG、PNG、HEIC、MP4、MOVファイルのみアップロード可能です。';
        break;
      case 500:
        errorMessage = 'サーバーエラーが発生しました。しばらく時間をおいて再試行してください。';
        break;
      default:
        if (error.error?.error?.message) {
          errorMessage = error.error.error.message;
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        } else {
          errorMessage = `Server Error: ${error.status} - ${error.statusText}`;
        }
    }
  }

  console.error('HTTP Error:', error);
  return throwError(() => new Error(errorMessage));
}