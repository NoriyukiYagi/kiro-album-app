import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Add JWT token to requests
    const token = this.authService.getToken();
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

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        return this.handleError(error);
      })
    );
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Client Error: ${error.error.message}`;
    } else {
      // Server-side error
      switch (error.status) {
        case 401:
          // Unauthorized - redirect to login
          this.authService.logout().subscribe();
          this.router.navigate(['/login']);
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
}