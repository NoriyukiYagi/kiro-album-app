# Infinite Loop Fix - AuthInterceptor 401 Error Handling

## 問題の概要

認証できないアカウントでログインした際に、AuthInterceptorが401エラーを受け取ってlogout処理を呼び出し、そのlogout APIリクエストでも401エラーが発生して無限ループに陥る問題がありました。

## 問題の詳細

### 無限ループの発生パターン:

1. **認証失敗**: 無効なGoogleトークンでログイン試行
2. **401エラー**: バックエンドから401 Unauthorizedレスポンス
3. **AuthInterceptor**: 401エラーをキャッチして`authService.logout()`を呼び出し
4. **Logout API**: `/auth/logout`エンドポイントにリクエスト送信
5. **再度401エラー**: 未認証状態でlogout APIを呼び出すため401エラー
6. **無限ループ**: 再びAuthInterceptorが401エラーをキャッチして同じ処理を繰り返す

```
認証失敗 → 401エラー → logout() → logout API → 401エラー → logout() → ...
```

## 修正内容

### 1. AuthServiceにローカルログアウト機能を追加

**新しいメソッド**:
```typescript
/**
 * Local logout without server call - used by interceptor to avoid infinite loops
 */
logoutLocal(): void {
  this.clearAuthData();
}
```

**目的**:
- サーバーAPIを呼び出さずにローカルの認証データのみクリア
- 無限ループを防ぐためのセーフティネット

### 2. AuthInterceptorのエラーハンドリング改善

**修正前**:
```typescript
case 401:
  // Unauthorized - redirect to login
  authService.logout().subscribe(); // ← これが無限ループの原因
  router.navigate(['/login']);
  break;
```

**修正後**:
```typescript
case 401:
  // Unauthorized - avoid infinite loop by not calling logout API for logout requests
  if (!requestUrl.includes('/auth/logout')) {
    // Use local logout to avoid infinite loop
    authService.logoutLocal();
    router.navigate(['/login']);
    errorMessage = 'セッションが期限切れです。再度ログインしてください。';
  } else {
    // For logout requests that fail with 401, just clear local data
    authService.logoutLocal();
    errorMessage = 'ログアウト処理中にエラーが発生しましたが、ローカルデータはクリアされました。';
  }
  break;
```

**改善点**:
- リクエストURLをチェックしてlogout APIかどうか判定
- logout API以外の401エラー: `logoutLocal()`でローカルクリアのみ
- logout APIの401エラー: ローカルクリアして適切なメッセージ表示

### 3. Google認証コールバックのエラーハンドリング強化

**修正前**:
```typescript
error: (error) => {
  console.error('Google login failed', error);
  // Error will be handled by the component
}
```

**修正後**:
```typescript
error: (error) => {
  console.error('Google login failed', error);
  // Clear any partial auth state
  this.clearAuthData();
  // Error will be handled by the component or interceptor
}
```

**改善点**:
- ログイン失敗時に部分的な認証状態をクリア
- 一貫性のある認証状態管理

## 修正されたファイル

### 1. `frontend/src/app/services/auth.service.ts`
- `logoutLocal()`メソッドを追加
- `handleGoogleCallback()`のエラーハンドリング改善

### 2. `frontend/src/app/interceptors/auth.interceptor.ts`
- `handleError()`関数にリクエストURL判定を追加
- logout APIリクエストの401エラーを特別処理
- 無限ループを防ぐロジックを実装

## エラーハンドリングの流れ

### 通常のAPIリクエストで401エラー:
```
API Request → 401 Error → AuthInterceptor → logoutLocal() → Navigate to /login
```

### Logout APIリクエストで401エラー:
```
Logout API → 401 Error → AuthInterceptor → logoutLocal() → Show message (no navigation)
```

### Google認証失敗:
```
Google Login → Error → clearAuthData() → Error handled by component
```

## テスト結果

✅ AuthServiceのテストが正常に通過
✅ ビルドが成功
✅ 無限ループが解消
✅ 適切なエラーメッセージ表示

## セキュリティ考慮事項

### 1. 認証状態の一貫性
- 401エラー時に確実にローカル認証データをクリア
- 部分的な認証状態を残さない

### 2. エラー情報の適切な処理
- 機密情報を含まないエラーメッセージ
- ユーザーフレンドリーな日本語メッセージ

### 3. 無限ループの防止
- サーバーAPIを呼び出さないローカルログアウト
- リクエストURL判定による適切な処理分岐

## 使用例

### 認証失敗時の動作:
1. 無効なトークンでAPIリクエスト
2. 401エラー発生
3. AuthInterceptorが`logoutLocal()`を呼び出し
4. ローカル認証データクリア
5. ログインページにリダイレクト
6. **無限ループなし**

### 手動ログアウト時の動作:
1. ユーザーがログアウトボタンをクリック
2. `logout()`メソッドでサーバーAPIを呼び出し
3. 成功時: サーバー・ローカル両方でセッション終了
4. 失敗時: ローカルデータのみクリア（無限ループなし）

これで、認証エラー時の無限ループ問題が完全に解決されました。