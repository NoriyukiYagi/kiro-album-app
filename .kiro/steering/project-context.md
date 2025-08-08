---
inclusion: always
---

# プロジェクトコンテキスト

このファイルは、タスク実行時に常に参照される重要なプロジェクト情報を含んでいます。

## プロジェクト概要

Angular フロントエンドと ASP.NET Core バックエンドを使用した写真・動画管理アルバムアプリケーション。

## 主要機能

- Google OAuth 認証
- 写真・動画のアップロード（JPG, PNG, HEIC, MP4, MOV対応）
- 自動サムネイル生成
- 日付ベースのファイル整理
- 管理者によるユーザー管理

## 技術スタック

- **フロントエンド**: Angular
- **バックエンド**: ASP.NET Core (.NET 8.0)
- **データベース**: PostgreSQL
- **認証**: Google OAuth + JWT
- **コンテナ**: Docker/Podman

## 開発環境

### アクセス先
- フロントエンド: http://localhost:4200
- バックエンドAPI: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- PostgreSQL: localhost:5432

### データベース接続情報
- **ホスト**: localhost
- **ポート**: 5432
- **データベース**: albumapp
- **ユーザー**: albumuser
- **パスワード**: albumpass

## 重要なディレクトリ構造

```
album-app/
├── backend/                 # ASP.NET Core バックエンド
│   ├── Controllers/        # API コントローラー
│   ├── Services/           # ビジネスロジック
│   ├── Models/             # データモデル・DTO
│   ├── Data/               # データベースコンテキスト
│   ├── Tests/              # 単体テスト
│   └── Middleware/         # カスタムミドルウェア
├── frontend/               # Angular フロントエンド
├── data/                   # データディレクトリ
│   ├── pict/              # 元画像・動画ファイル
│   └── thumb/             # サムネイル画像
└── .kiro/specs/album-app/  # 仕様書・タスク管理
```

## 開発時の注意事項

### テスト実行
- Podmanコンテナ経由: `podman run --rm --network=host -v ${PWD}/backend:/src -v nuget-cache:/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test`
- ローカル環境でのテスト実行はしないこと

### ビルド
- Podmanコンテナ経由: `podman run --rm --network=host -v ${PWD}/backend:/src -v nuget-cache:/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build`
- ローカル環境でのビルド実行はしないこと

### NuGetキャッシュ
- NuGetパッケージキャッシュ用の名前付きボリューム `nuget-cache` を使用
- 初回実行時にボリュームが自動作成され、以降の実行で再利用される
- キャッシュをクリアする場合: `podman volume rm nuget-cache`

### フロントエンドのビルド・テスト実行
- 開発環境用Dockerイメージを使用: `podman build --network=host -t album-app-frontend-dev -f frontend/Dockerfile.dev frontend/`
- 依存関係のインストール: `podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm install`
- ビルド実行: `podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm run build`
- 単体テスト実行: `podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm run test:ci`
- リント実行: `podman run --rm --network=host -v ${PWD}/frontend:/app -w /app album-app-frontend-dev npm run lint`
- ローカル環境でのビルド・テスト実行はしないこと

### テスト環境の特徴
- **Chrome Headless**: Google Chrome がDockerコンテナ内にインストールされ、ヘッドレスモードでテスト実行
- **--no-sandbox**: Docker環境でのChrome実行に必要なフラグを自動適用
- **CI対応**: 継続的インテグレーション環境での自動テスト実行に最適化

### NPMキャッシュ
- NPMパッケージキャッシュ用の名前付きボリューム `npm-cache` を使用可能
- キャッシュを使用する場合: `-v npm-cache:/root/.npm` オプションを追加
- キャッシュをクリアする場合: `podman volume rm npm-cache`

### 管理者設定
- 管理者ユーザーは `appsettings.json` の `AdminUsers` セクションで設定
- Google OAuth設定が必要

## 参照すべきファイル

#[[file:README.md]] - 詳細なセットアップ手順とトラブルシューティング