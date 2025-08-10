import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { AuthGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { UserInfo } from '../models/user.model';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let isAuthenticatedSubject: BehaviorSubject<boolean>;

  beforeEach(() => {
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
    
    mockAuthService = jasmine.createSpyObj('AuthService', [
      'getToken',
      'getUserInfo'
    ], {
      isAuthenticated$: isAuthenticatedSubject.asObservable()
    });

    mockRouter = jasmine.createSpyObj('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ]
    });
    
    guard = TestBed.inject(AuthGuard);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should redirect to login when no token exists', () => {
    const loginUrlTree = {} as UrlTree;
    mockAuthService.getToken.and.returnValue(null);
    mockRouter.createUrlTree.and.returnValue(loginUrlTree);

    const result = guard.canActivate();

    expect(result).toBe(loginUrlTree);
    expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login']);
  });

  it('should allow access when user is authenticated and token is valid', (done) => {
    const mockUser: UserInfo = {
      id: 1,
      email: 'test@example.com',
      name: 'Test User',
      isAdmin: false
    };

    mockAuthService.getToken.and.returnValue('valid-token');
    mockAuthService.getUserInfo.and.returnValue(of(mockUser));
    
    // Set authenticated state
    isAuthenticatedSubject.next(true);

    const result = guard.canActivate();
    
    if (result instanceof Promise) {
      result.then((canActivate) => {
        expect(canActivate).toBe(true);
        done();
      });
    } else if (typeof result === 'object' && 'subscribe' in result) {
      result.subscribe((canActivate) => {
        expect(canActivate).toBe(true);
        done();
      });
    }
  });

  it('should redirect to login when token is invalid', (done) => {
    const loginUrlTree = {} as UrlTree;
    mockAuthService.getToken.and.returnValue('invalid-token');
    mockAuthService.getUserInfo.and.returnValue(throwError(() => new Error('Invalid token')));
    mockRouter.createUrlTree.and.returnValue(loginUrlTree);
    
    // Set authenticated state
    isAuthenticatedSubject.next(true);

    const result = guard.canActivate();
    
    if (result instanceof Promise) {
      result.then((canActivate) => {
        expect(canActivate).toBe(loginUrlTree);
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login']);
        done();
      });
    } else if (typeof result === 'object' && 'subscribe' in result) {
      result.subscribe((canActivate) => {
        expect(canActivate).toBe(loginUrlTree);
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login']);
        done();
      });
    }
  });

  it('should redirect to login when user is not authenticated', (done) => {
    const loginUrlTree = {} as UrlTree;
    mockAuthService.getToken.and.returnValue('some-token');
    mockRouter.createUrlTree.and.returnValue(loginUrlTree);
    
    // Set unauthenticated state
    isAuthenticatedSubject.next(false);

    const result = guard.canActivate();
    
    if (result instanceof Promise) {
      result.then((canActivate) => {
        expect(canActivate).toBe(loginUrlTree);
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login']);
        done();
      });
    } else if (typeof result === 'object' && 'subscribe' in result) {
      result.subscribe((canActivate) => {
        expect(canActivate).toBe(loginUrlTree);
        expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/login']);
        done();
      });
    }
  });
});