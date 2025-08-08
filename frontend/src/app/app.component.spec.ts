import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AppComponent } from './app.component';
import { AuthService } from './services/auth.service';
import { ErrorHandlerService } from './services/error-handler.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

describe('AppComponent', () => {
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockErrorHandler: jasmine.SpyObj<ErrorHandlerService>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', [
      'initializeGoogleAuth',
      'logout',
      'isAdmin'
    ], {
      isAuthenticated$: of(false),
      currentUser$: of(null)
    });

    mockErrorHandler = jasmine.createSpyObj('ErrorHandlerService', [
      'showSuccess',
      'handleError'
    ]);

    mockAuthService.initializeGoogleAuth.and.returnValue(Promise.resolve());
    mockAuthService.logout.and.returnValue(of({}));
    mockAuthService.isAdmin.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        RouterTestingModule,
        BrowserAnimationsModule
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: ErrorHandlerService, useValue: mockErrorHandler }
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should have title album-app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('album-app');
  });

  it('should initialize Google Auth on init', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    app.ngOnInit();
    expect(mockAuthService.initializeGoogleAuth).toHaveBeenCalled();
  });
});