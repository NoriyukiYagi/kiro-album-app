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

### 管理者設定
- 管理者ユーザーは `appsettings.json` の `AdminUsers` セクションで設定
- Google OAuth設定が必要

## 参照すべきファイル

#[[file:README.md]] - 詳細なセットアップ手順とトラブルシューティング