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

  beforeEach(async () => {
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

    mockAuthService = jasmine.createSpyObj('AuthService', [
      'initializeGoogleAuth',
      'loginWithGoogle'
    ], {
      isAuthenticated$: isAuthenticatedSubject.asObservable()
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
    expect(component).toBeTruthy();
  });

  it('should navigate to album if already authenticated', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    fixture.detectChanges();

    // Simulate user being authenticated
    isAuthenticatedSubject.next(true);

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/album']);
  });

  it('should initialize Google Auth on init', async () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    fixture.detectChanges();

    expect(mockAuthService.initializeGoogleAuth).toHaveBeenCalled();
  });

  it('should handle Google Auth initialization failure', (done) => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.reject(new Error('Init failed')));

    // Spy on console.error to capture the error
    spyOn(console, 'error');

    fixture.detectChanges();

    // Wait for the promise to resolve/reject
    setTimeout(() => {
      expect(console.error).toHaveBeenCalledWith('Failed to initialize Google Auth:', jasmine.any(Error));
      done();
    }, 100);
  });

  it('should show error when Google auth is not available', () => {
    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());

    // Mock google as undefined
    (window as any).google = undefined;

    fixture.detectChanges();

    component.loginWithGoogle();

    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Google認証が利用できません',
      '閉じる',
      jasmine.any(Object)
    );
  });
});