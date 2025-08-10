# AuthInterceptor Fix - Angular 17+ Function-based Interceptors

## 問題の概要

AuthInterceptorが動作していない原因は、Angular 17以降で推奨される新しい関数型インターセプター（`HttpInterceptorFn`）と古いクラス型インターセプター（`HttpInterceptor`）の設定方法が混在していたためです。

## 問題の詳細

### 1. 設定方法の不整合

**main.ts での問題**:
```typescript
// 新しい方式をインポートしているが使用していない
import { provideHttpClient, withInterceptors } from '@angular/common/http';

// 古い方式を使用
{
  provide: HTTP_INTERCEPTORS,
  useClass: AuthInterceptor,
  multi: true
}
```

### 2. インターセプターの実装方式

- **古い方式**: クラス型 `implements HttpInterceptor`
- **新しい方式**: 関数型 `HttpInterceptorFn`

Angular 17以降では関数型が推奨され、`withInterceptors`と組み合わせて使用します。

## 修正内容

### 1. main.ts の修正

**修正前**:
```typescript
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';
import { LoadingInterceptor } from './app/interceptors/loading.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoadingInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ]
});
```

**修正後**:
```typescript
import { authInterceptor } from './app/interceptors/auth.interceptor';
import { loadingInterceptor } from './app/interceptors/loading.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(
      withInterceptors([loadingInterceptor, authInterceptor])
    )
  ]
});
```

### 2. AuthInterceptor の修正

**修正前** (クラス型):
```typescript
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // ...
  }
}
```

**修正後** (関数型):
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // JWT token を追加
  const token = authService.getToken();
  let authReq = req;

  if (token) {
    authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      return handleError(error, authService, router);
    })
  );
};
```

### 3. LoadingInterceptor の修正

**修正前** (クラス型):
```typescript
@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  constructor(private loadingService: LoadingService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // ...
  }
}
```

**修正後** (関数型):
```typescript
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  const skipLoading = req.url.includes('/auth/logout') || 
                     req.headers.has('X-Skip-Loading');
  
  if (!skipLoading) {
    loadingService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (!skipLoading) {
        loadingService.hide();
      }
    })
  );
};
```

## 新しい関数型インターセプターの利点

### 1. 依存性注入の簡素化
- `inject()`関数を使用してサービスを注入
- コンストラクターが不要

### 2. 関数型プログラミング
- よりシンプルで読みやすいコード
- テストが容易

### 3. Tree-shaking の改善
- 未使用のコードがより効率的に除去される

### 4. Angular の推奨方式
- Angular 17以降の標準的な実装方法

## インターセプターの実行順序

```typescript
provideHttpClient(
  withInterceptors([loadingInterceptor, authInterceptor])
)
```

実行順序:
1. **loadingInterceptor**: ローディング状態を管理
2. **authInterceptor**: JWTトークンを追加、エラーハンドリング

## 機能確認

### AuthInterceptor の機能:
- ✅ JWTトークンの自動付与
- ✅ Content-Typeヘッダーの設定
- ✅ HTTPエラーハンドリング
- ✅ 401エラー時の自動ログアウト・リダイレクト

### LoadingInterceptor の機能:
- ✅ API呼び出し中のローディング表示
- ✅ 特定のリクエスト（logout等）のローディングスキップ

## テスト結果

✅ ビルドが正常に完了
✅ AuthServiceのテストが通過
✅ 型エラーが解消
✅ インターセプターが正常に登録

## 次のステップ

これで、AuthInterceptorが正常に動作し、以下の機能が提供されます:

1. **自動認証**: すべてのAPIリクエストにJWTトークンが自動付与
2. **エラーハンドリング**: 401エラー時の自動ログアウト
3. **ローディング管理**: API呼び出し中の視覚的フィードバック

Google OAuth Client IDを設定すれば、完全な認証フローをテストできます。