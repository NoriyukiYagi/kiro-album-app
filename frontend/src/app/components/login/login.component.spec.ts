import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BehaviorSubject, of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let isAuthenticatedSubject: BehaviorSubject<boolean>;
  let loginErrorSubject: BehaviorSubject<string | null>;

  beforeEach(async () => {
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
    loginErrorSubject = new BehaviorSubject<string | null>(null);

    mockAuthService = jasmine.createSpyObj('AuthService', [
      'initializeGoogleAuth',
      'loginWithGoogle',
      'clearLoginError'
    ], {
      isAuthenticated$: isAuthenticatedSubject.asObservable(),
      loginError$: loginErrorSubject.asObservable()
    });

    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, NoopAnimationsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
        { provide: MatSnackBar, useValue: mockSnackBar }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should navigate to album if already authenticated', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    // Set user as authenticated before component initialization
    isAuthenticatedSubject.next(true);
    
    fixture.detectChanges();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/album']);
  });

  it('should initialize Google Auth on init', async () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    fixture.detectChanges();

    expect(mockAuthService.initializeGoogleAuth).toHaveBeenCalled();
  });

  it('should handle Google Auth initialization failure', async () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.reject(new Error('Init failed')));

    // Spy on console.error to capture the error
    spyOn(console, 'error');
    // Spy on the private showError method
    spyOn(component as any, 'showError');

    fixture.detectChanges();

    // Wait for the promise to resolve/reject
    await new Promise(resolve => setTimeout(resolve, 200));

    expect(console.error).toHaveBeenCalledWith('Failed to initialize Google Auth:', jasmine.any(Error));
    expect(component.hasError).toBeTrue();
    expect((component as any).showError).toHaveBeenCalledWith('Google認証の初期化に失敗しました');
  });

  it('should show error when Google auth is not available', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    fixture.detectChanges();

    // Spy on the private showError method
    spyOn(component as any, 'showError');

    // Mock google as undefined
    (window as any).google = undefined;

    component.loginWithGoogle();

    expect(component.hasError).toBeTrue();
    expect((component as any).showError).toHaveBeenCalledWith('Google認証が利用できません');
  });

  it('should handle login errors from auth service', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());
    
    // Spy on the private showError method
    spyOn(component as any, 'showError');
    
    fixture.detectChanges();

    // Simulate login error
    loginErrorSubject.next('認証に失敗しました');

    expect(component.hasError).toBeTrue();
    expect(component.isLoading).toBeFalse();
    expect((component as any).showError).toHaveBeenCalledWith('認証に失敗しました');
  });

  it('should clear errors on retry', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());
    
    fixture.detectChanges();

    // Set error state
    component.hasError = true;

    // Call retry
    component.retryLogin();

    expect(component.hasError).toBeFalse();
    expect(mockAuthService.clearLoginError).toHaveBeenCalled();
    expect(mockAuthService.initializeGoogleAuth).toHaveBeenCalledTimes(2); // Once in ngOnInit, once in retry
  });
});