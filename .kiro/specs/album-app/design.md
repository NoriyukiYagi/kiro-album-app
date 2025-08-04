# 設計文書

## 概要

Angular フロントエンドと ASP.NET Core バックエンドによる写真・動画管理アルバムアプリケーション。Google OAuth 認証、メディアファイル管理、サムネイル生成機能を持つ Docker コンテナベースのアプリケーション。

## アーキテクチャ

### システム構成

```mermaid
graph TB
    subgraph "Docker Environment"
        subgraph "Frontend Container"
            A[Angular App<br/>Port: 4200]
        end
        
        subgraph "Backend Container"
            B[ASP.NET Core API<br/>Port: 5000]
            C[File Storage Service]
            D[Thumbnail Service]
        end
        
        subgraph "Database Container"
            E[PostgreSQL<br/>Port: 5432]
        end
        
        subgraph "Persistent Volumes"
            F[/data/pict/<br/>Original Files]
            G[/data/thumb/<br/>Thumbnails]
        end
    end
    
    H[Google OAuth] --> A
    A --> B
    B --> E
    B --> F
    B --> G
    C --> F
    D --> G
```

### 技術スタック

**フロントエンド:**
- Angular 17+ (最新安定版)
- Angular Material (UI コンポーネント)
- RxJS (リアクティブプログラミング)
- Google OAuth ライブラリ

**バックエンド:**
- ASP.NET Core 8.0
- Entity Framework Core (ORM)
- PostgreSQL (データベース)
- ImageSharp (画像処理)
- FFMpegCore (動画処理)
- Google OAuth 認証

**インフラ:**
- Docker & Docker Compose
- Nginx (リバースプロキシ、本番環境用)

## コンポーネントとインターフェース

### フロントエンド コンポーネント

#### 1. 認証コンポーネント
- **LoginComponent**: Google OAuth ログイン画面
- **AuthGuard**: 認証状態の管理とルートガード
- **AuthService**: 認証状態の管理とトークン処理

#### 2. メディア管理コンポーネント
- **AlbumListComponent**: サムネイル一覧表示
- **MediaViewerComponent**: 個別メディアファイル表示
- **UploadComponent**: ファイルアップロード機能
- **AdminUserManagementComponent**: ユーザー管理（管理者のみ）

#### 3. 共通コンポーネント
- **HeaderComponent**: ナビゲーションヘッダー
- **LoadingComponent**: ローディング表示
- **ErrorComponent**: エラー表示

### バックエンド コンポーネント

#### 1. コントローラー
- **AuthController**: 認証関連API
- **MediaController**: メディアファイル管理API
- **UserController**: ユーザー管理API（管理者のみ）
- **ThumbnailController**: サムネイル配信API

#### 2. サービス
- **AuthService**: Google OAuth 認証処理
- **MediaService**: メディアファイル処理
- **ThumbnailService**: サムネイル生成
- **FileStorageService**: ファイル保存・取得
- **MetadataService**: ファイルメタデータ抽出

#### 3. リポジトリ
- **UserRepository**: ユーザーデータアクセス
- **MediaRepository**: メディアファイル情報アクセス

### API インターフェース

#### 認証API
```
POST /api/auth/google-login
GET  /api/auth/user-info
POST /api/auth/logout
```

#### メディアAPI
```
GET    /api/media                    # メディア一覧取得
POST   /api/media/upload             # ファイルアップロード
GET    /api/media/{id}               # 個別メディア取得
DELETE /api/media/{id}               # メディア削除
GET    /api/media/thumbnail/{id}     # サムネイル取得
```

#### ユーザー管理API（管理者のみ）
```
GET    /api/users                    # ユーザー一覧
POST   /api/users                    # ユーザー追加
DELETE /api/users/{id}               # ユーザー削除
```

## データモデル

### User エンティティ
```csharp
public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
```

### MediaFile エンティティ
```csharp
public class MediaFile
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
    public string FilePath { get; set; }
    public string ThumbnailPath { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime TakenAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedBy { get; set; }
    public User User { get; set; }
}
```

### 設定ファイル構造
```json
{
  "AdminUsers": [
    "admin@example.com",
    "manager@example.com"
  ],
  "GoogleOAuth": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "FileStorage": {
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": ["jpg", "jpeg", "png", "heic", "mp4", "mov"],
    "PictureDirectory": "/data/pict",
    "ThumbnailDirectory": "/data/thumb"
  }
}
```

## エラーハンドリング

### フロントエンド エラーハンドリング
- **GlobalErrorHandler**: 全体的なエラーハンドリング
- **HttpInterceptor**: HTTP エラーの統一処理
- **Toast/Snackbar**: ユーザーフレンドリーなエラー表示

### バックエンド エラーハンドリング
- **GlobalExceptionMiddleware**: 未処理例外のキャッチ
- **ValidationFilter**: モデル検証エラーの処理
- **AuthorizationFilter**: 認証・認可エラーの処理

### エラーレスポンス形式
```json
{
  "error": {
    "code": "INVALID_FILE_SIZE",
    "message": "ファイルサイズが上限を超えています",
    "details": "最大100MBまでアップロード可能です"
  }
}
```

## テスト戦略

### フロントエンド テスト
- **Unit Tests**: Jasmine + Karma
  - コンポーネントロジック
  - サービス機能
  - パイプ・ディレクティブ
- **Integration Tests**: Angular Testing Library
  - コンポーネント間の連携
  - HTTP 通信のモック
- **E2E Tests**: Cypress
  - ユーザーフロー全体
  - 認証フロー
  - ファイルアップロードフロー

### バックエンド テスト
- **Unit Tests**: xUnit
  - サービスロジック
  - リポジトリ機能
  - ユーティリティ関数
- **Integration Tests**: ASP.NET Core Test Host
  - API エンドポイント
  - データベース連携
  - ファイル操作
- **Performance Tests**: NBomber
  - ファイルアップロード性能
  - サムネイル生成性能

### Docker テスト
- **Container Tests**: Testcontainers
  - マルチコンテナ環境での統合テスト
  - データベース連携テスト
  - ボリュームマウントテスト