import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, take, catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    // First check if we have a token
    const token = this.authService.getToken();
    
    if (!token) {
      return this.router.createUrlTree(['/login']);
    }

    // If we have a token, verify it's still valid by checking user info
    return this.authService.isAuthenticated$.pipe(
      take(1),
      switchMap(isAuthenticated => {
        if (isAuthenticated) {
          // Double-check by fetching user info to validate token
          return this.authService.getUserInfo().pipe(
            map(() => true),
            catchError(() => {
              // Token is invalid, redirect to login
              return of(this.router.createUrlTree(['/login']));
            })
          );
        } else {
          return of(this.router.createUrlTree(['/login']));
        }
      })
    );
  }
}