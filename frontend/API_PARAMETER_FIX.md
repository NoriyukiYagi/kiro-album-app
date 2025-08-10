# API Parameter Fix - Google Login

## 問題の概要

フロントエンドからバックエンドの`google-login` APIを呼び出す際に、パラメータ名とレスポンス構造が一致していませんでした。

## 修正内容

### 1. パラメータ名の修正

**修正前**:
- フロントエンド: `googleToken`
- バックエンド: `IdToken`

**修正後**:
- フロントエンド: `idToken` ✅
- バックエンド: `IdToken` ✅

### 2. レスポンス構造の修正

**修正前**:
```typescript
// フロントエンド
interface AuthResponse {
  token: string;
  user: User;
}
```

**修正後**:
```typescript
// フロントエンド (バックエンドに合わせて修正)
interface AuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserInfo;
}
```

### 3. ユーザー情報モデルの修正

**新しいUserInfoインターフェース**:
```typescript
interface UserInfo {
  id: number;
  email: string;
  name: string;
  isAdmin: boolean;
}
```

## 修正されたファイル

### フロントエンド
1. `frontend/src/app/models/user.model.ts`
   - `LoginRequest`のパラメータ名を`googleToken` → `idToken`に変更
   - `AuthResponse`の構造をバックエンドに合わせて修正
   - `UserInfo`インターフェースを追加

2. `frontend/src/app/services/auth.service.ts`
   - APIリクエストのパラメータ名を修正
   - レスポンス処理を新しい構造に対応
   - 型定義を`UserInfo`に更新

3. `frontend/src/app/services/auth.service.spec.ts`
   - テストデータを新しい構造に更新
   - モックレスポンスを修正

4. `frontend/src/app/guards/auth.guard.spec.ts`
   - テストで使用する型を`UserInfo`に更新

## API呼び出しの流れ

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
     "accessToken": "jwt_token_here",
     "tokenType": "Bearer",
     "expiresIn": 3600,
     "user": {
       "id": 1,
       "email": "user@example.com",
       "name": "User Name",
       "isAdmin": false
     }
   }
   ```

## テスト結果

✅ AuthServiceのユニットテストが正常に通過
✅ 型エラーが解消
✅ APIパラメータの一致を確認

## 次のステップ

Google OAuth認証を実際にテストするには:

1. Google Cloud Consoleでプロジェクトを作成
2. OAuth 2.0 Client IDを取得
3. `frontend/src/environments/environment.ts`の`googleClientId`を実際の値に更新
4. バックエンドの`appsettings.json`にGoogle OAuth設定を追加

これで、フロントエンドとバックエンド間のAPI通信が正常に動作するはずです。