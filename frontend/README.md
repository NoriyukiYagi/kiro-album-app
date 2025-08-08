# Album App Frontend

Angular 17 frontend application for the Album App with Google OAuth authentication.

## Features Implemented

### Task 10: Angular Frontend Project Setup

✅ **Angular Material and Dependencies Setup**
- Angular 17 with standalone components
- Angular Material UI components
- Angular CDK for advanced functionality
- RxJS for reactive programming
- SCSS styling support

✅ **Google OAuth Library and Authentication Service**
- Google Identity Services integration
- AuthService with JWT token management
- User authentication state management
- Automatic token refresh handling
- Local storage for session persistence

✅ **HTTP Interceptors**
- **AuthInterceptor**: Automatic JWT token injection and error handling
- **LoadingInterceptor**: Global loading state management
- Centralized error handling with user-friendly messages
- Automatic logout on 401 responses

## Architecture

### Services
- **AuthService**: Google OAuth authentication and JWT management
- **ErrorHandlerService**: Centralized error handling with Material Snackbar
- **LoadingService**: Global loading state management

### Guards
- **AuthGuard**: Protects routes requiring authentication
- **AdminGuard**: Protects admin-only routes

### Interceptors
- **AuthInterceptor**: JWT token handling and HTTP error management
- **LoadingInterceptor**: Automatic loading indicators

### Components
- **AppComponent**: Main application shell with navigation
- **LoadingComponent**: Global loading spinner overlay
- **AdminUserManagementComponent**: Placeholder for admin functionality

## Configuration

### Environment Variables
- `environment.apiUrl`: Backend API URL
- `environment.googleClientId`: Google OAuth Client ID

### Google OAuth Setup
1. Configure Google OAuth credentials in environment files
2. Update `environment.googleClientId` with your client ID
3. Add authorized domains in Google Console

## Development

### Build
```bash
podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm run build
```

### Test
```bash
podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm test -- --watch=false --browsers=ChromeHeadless
```

### Lint
```bash
podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm run lint
```

## Next Steps

The following components need to be implemented in subsequent tasks:
- LoginComponent (Task 11)
- UploadComponent (Task 12) 
- AlbumListComponent (Task 13)
- MediaViewerComponent (Task 14)
- Complete AdminUserManagementComponent (Task 15)

## Security Features

- JWT token automatic injection
- Secure token storage
- Automatic logout on authentication errors
- Route protection with guards
- Admin role-based access control
- CSRF protection through Angular's built-in mechanisms