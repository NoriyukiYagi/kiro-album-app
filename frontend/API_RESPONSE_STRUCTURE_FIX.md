# API Response Structure Fix

## 問題の概要

バックエンドのAPIが`ApiResponse<T>`型でレスポンスを返しているのに対し、フロントエンドでは直接`T`型として扱っていたため、ログイン機能が動作していませんでした。

## バックエンドのレスポンス構造

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
```

### 実際のAPIレスポンス例

**成功時**:
```json
{
  "success": true,
  "data": {
    "accessToken": "jwt_token_here",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": 1,
      "email": "user@example.com",
      "name": "User Name",
      "isAdmin": false
    }
  },
  "message": "Login successful"
}
```

**失敗時**:
```json
{
  "success": false,
  "error": "UNAUTHORIZED",
  "message": "Invalid Google token or user not authorized"
}
```

## 修正内容

### 1. ApiResponse型の追加

`frontend/src/app/models/user.model.ts`に追加:
```typescript
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}
```

### 2. AuthServiceの修正

**修正前**:
```typescript
return this.http.post<AuthResponse>(`${this.API_URL}/auth/google-login`, loginRequest)
  .pipe(
    tap(response => {
      this.setToken(response.accessToken);
      // ...
    })
  );
```

**修正後**:
```typescript
return this.http.post<ApiResponse<AuthResponse>>(`${this.API_URL}/auth/google-login`, loginRequest)
  .pipe(
    map(apiResponse => {
      if (!apiResponse.success || !apiResponse.data) {
        throw new Error(apiResponse.message || 'Login failed');
      }
      return apiResponse.data;
    }),
    tap(response => {
      this.setToken(response.accessToken);
      // ...
    })
  );
```

### 3. エラーハンドリングの改善

- APIレスポンスの`success`フラグをチェック
- 失敗時は`message`または`error`を使用してエラーを投げる
- 成功時のみ`data`プロパティを返す

### 4. 全APIエンドポイントの対応

以下のメソッドを`ApiResponse<T>`構造に対応:

- ✅ `loginWithGoogle()` - `ApiResponse<AuthResponse>`
- ✅ `getUserInfo()` - `ApiResponse<UserInfo>`  
- ✅ `logout()` - `ApiResponse<any>`

## 修正されたファイル

### フロントエンド
1. `frontend/src/app/models/user.model.ts`
   - `ApiResponse<T>`インターフェースを追加

2. `frontend/src/app/services/auth.service.ts`
   - 全APIメソッドを`ApiResponse<T>`構造に対応
   - エラーハンドリングを改善
   - `map`オペレーターを使用してデータを抽出

3. `frontend/src/app/services/auth.service.spec.ts`
   - テストデータを`ApiResponse<T>`構造に更新
   - 失敗ケースのテストを追加

## テスト結果

✅ AuthServiceの全テストが正常に通過
✅ 成功・失敗両方のケースをテスト
✅ ビルドが正常に完了
✅ 型エラーが解消

## API通信の流れ

### 修正後の正しい流れ:

1. **フロントエンド → バックエンド**:
   ```typescript
   POST /api/auth/google-login
   {
     "idToken": "google_id_token_here"
   }
   ```

2. **バックエンド → フロントエンド**:
   ```json
   {
     "success": true,
     "data": {
       "accessToken": "jwt_token_here",
       "tokenType": "Bearer",
       "expiresIn": 3600,
       "user": {
         "id": 1,
         "email": "user@example.com",
         "name": "User Name",
         "isAdmin": false
       }
     },
     "message": "Login successful"
   }
   ```

3. **フロントエンドでの処理**:
   - `ApiResponse<AuthResponse>`として受信
   - `success`フラグをチェック
   - 成功時は`data`プロパティから`AuthResponse`を抽出
   - 失敗時は`message`を使用してエラーを投げる

## 次のステップ

これで、フロントエンドとバックエンド間のAPI通信が正常に動作するはずです。Google OAuth Client IDを設定すれば、実際の認証フローをテストできます。

## セキュリティ考慮事項

- エラーメッセージは適切にハンドリングされ、機密情報が漏洩しない
- 失敗時も適切にログアウト処理が実行される
- JWTトークンは安全にlocalStorageに保存される