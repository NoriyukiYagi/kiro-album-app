import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { User, UserInfo, AuthResponse } from '../models/user.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should login with Google token', () => {
    const mockUser: UserInfo = {
      id: 1,
      email: 'test@example.com',
      name: 'Test User',
      isAdmin: false
    };

    const mockResponse: AuthResponse = {
      accessToken: 'jwt-token',
      tokenType: 'Bearer',
      expiresIn: 3600,
      user: mockUser
    };

    service.loginWithGoogle('google-token').subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(service.getToken()).toBe('jwt-token');
      expect(service.getCurrentUser()).toEqual(mockUser);
    });

    const req = httpMock.expectOne('http://localhost:5000/api/auth/google-login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ idToken: 'google-token' });
    req.flush(mockResponse);
  });

  it('should logout', () => {
    // Set up initial auth state
    localStorage.setItem('auth_token', 'test-token');
    localStorage.setItem('current_user', JSON.stringify({ id: 1, name: 'Test' }));

    service.logout().subscribe(() => {
      expect(service.getToken()).toBeNull();
      expect(service.getCurrentUser()).toBeNull();
    });

    const req = httpMock.expectOne('http://localhost:5000/api/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('should check if user is authenticated', () => {
    expect(service.isAuthenticated()).toBeFalse();

    localStorage.setItem('auth_token', 'test-token');
    expect(service.isAuthenticated()).toBeTrue();
  });
});