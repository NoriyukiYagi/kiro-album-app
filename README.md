# Album App

Angular フロントエンドと ASP.NET Core バックエンドを使用した写真・動画管理アルバムアプリケーション。

## 機能

- Google OAuth 認証
- 写真・動画のアップロード（JPG, PNG, HEIC, MP4, MOV対応）
- 自動サムネイル生成
- 日付ベースのファイル整理
- 管理者によるユーザー管理

## 開発環境セットアップ

### 前提条件

- Docker Desktop
- Docker Compose

### 開発環境の起動

#### Docker Composeを使用する場合

```bash
# 開発環境の起動
docker-compose -f docker-compose.dev.yml up --build

# バックグラウンドで起動
docker-compose -f docker-compose.dev.yml up -d --build
```

#### Podmanを使用する場合

```bash
# ネットワークを作成
podman network create album-network

# PostgreSQLコンテナを起動
podman run -d --name album-app-postgres-dev --network album-network \
  -e POSTGRES_DB=albumapp -e POSTGRES_USER=albumuser -e POSTGRES_PASSWORD=albumpass \
  -p 5432:5432 postgres:15

# バックエンドをビルドして起動
podman build -t album-app-backend-dev -f backend/Dockerfile.dev backend/
podman run -d --name album-app-backend-dev --network album-network \
  -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS=http://+:5000 \
  -e "ConnectionStrings__DefaultConnection=Host=album-app-postgres-dev;Database=albumapp;Username=albumuser;Password=albumpass" \
  -p 0.0.0.0:5000:5000 -v ${PWD}/backend:/app -v ${PWD}/data/pict:/data/pict -v ${PWD}/data/thumb:/data/thumb \
  album-app-backend-dev

# フロントエンドをビルドして起動
podman build -t album-app-frontend-dev -f frontend/Dockerfile.dev frontend/
podman run -d --name album-app-frontend-dev --network album-network \
  -p 4200:4200 album-app-frontend-dev
```

### アクセス

- フロントエンド: http://localhost:4200
- バックエンドAPI: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- PostgreSQL: localhost:5432

### 開発環境の停止

#### Docker Composeを使用する場合

```bash
docker-compose -f docker-compose.dev.yml down
```

#### Podmanを使用する場合

```bash
# コンテナを停止・削除
podman stop album-app-frontend-dev album-app-backend-dev album-app-postgres-dev
podman rm album-app-frontend-dev album-app-backend-dev album-app-postgres-dev

# ネットワークを削除（必要に応じて）
podman network rm album-network
```

## 本番環境デプロイ

### 本番環境の起動

```bash
# 本番環境の起動
docker-compose up --build

# バックグラウンドで起動
docker-compose up -d --build
```

### アクセス

- アプリケーション: http://localhost
- HTTPS (SSL設定後): https://localhost

### 本番環境の停止

```bash
docker-compose down
```

## ディレクトリ構造

```
album-app/
├── backend/                 # ASP.NET Core バックエンド
│   ├── Dockerfile          # 本番用Dockerfile
│   ├── Dockerfile.dev      # 開発用Dockerfile
│   ├── AlbumApp.csproj     # プロジェクトファイル
│   └── Program.cs          # エントリーポイント
├── frontend/               # Angular フロントエンド
│   ├── Dockerfile          # 本番用Dockerfile
│   ├── Dockerfile.dev      # 開発用Dockerfile
│   ├── package.json        # NPMパッケージ設定
│   └── nginx.conf          # Nginx設定
├── nginx/                  # リバースプロキシ（本番用）
│   ├── Dockerfile          # Nginx Dockerfile
│   └── nginx.conf          # Nginx設定
├── data/                   # データディレクトリ
│   ├── pict/              # 元画像・動画ファイル
│   └── thumb/             # サムネイル画像
├── backups/               # データベースバックアップ
├── ssl/                   # SSL証明書（本番用）
├── docker-compose.yml     # 本番用Docker Compose
├── docker-compose.dev.yml # 開発用Docker Compose
└── README.md              # このファイル
```

## 設定

### Google OAuth設定

1. Google Cloud Consoleでプロジェクトを作成
2. OAuth 2.0 クライアントIDを作成
3. バックエンドの設定ファイルにクライアントIDとシークレットを設定

### 管理者設定

バックエンドの設定ファイルで管理者のメールアドレスを指定してください。

## 開発

### ホットリロード

開発環境では以下の機能が有効です：

- Angular: ファイル変更時の自動リロード
- ASP.NET Core: `dotnet watch` による自動再起動
- PostgreSQL: データの永続化

### ボリュームマウント

- `./backend` → `/app` (バックエンドソースコード)
- `./frontend` → `/app` (フロントエンドソースコード)
- `./data/pict` → `/data/pict` (画像・動画ファイル)
- `./data/thumb` → `/data/thumb` (サムネイル)

## トラブルシューティング

### ポートが使用中の場合

```bash
# 使用中のポートを確認
netstat -an | findstr :4200
netstat -an | findstr :5000
netstat -an | findstr :5432

# プロセスを終了してから再起動
```

### データベース接続エラー

```bash
# PostgreSQLコンテナの状態確認
docker-compose -f docker-compose.dev.yml ps postgres

# ログの確認
docker-compose -f docker-compose.dev.yml logs postgres
```

## PostgreSQL接続情報

### 接続パラメータ

- **ホスト**: localhost
- **ポート**: 5432
- **データベース**: albumapp
- **ユーザー**: albumuser
- **パスワード**: albumpass

### WindowsからPostgreSQLに接続する方法

#### psqlクライアントを使用する場合

```bash
# PostgreSQLクライアントがインストールされている場合
psql -h localhost -p 5432 -U albumuser -d albumapp
```

#### Podmanコンテナ経由で接続する場合

```bash
# コンテナ内のpsqlを使用
podman exec -it album-app-postgres-dev psql -U albumuser -d albumapp

# SQLクエリを直接実行
podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "SELECT version();"
```

#### 接続テスト

```powershell
# TCP接続テスト
Test-NetConnection -ComputerName localhost -Port 5432

# データベース接続テスト
podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "SELECT 'Connection Success' as status, current_timestamp;"
```

### データベース管理

#### データベース一覧表示

```bash
podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "\l"
```

#### テーブル一覧表示

```bash
podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "\dt"
```

#### データベースバックアップ

```bash
# バックアップ作成
podman exec album-app-postgres-dev pg_dump -U albumuser albumapp > ./backups/albumapp_backup_$(date +%Y%m%d_%H%M%S).sql

# バックアップ復元
podman exec -i album-app-postgres-dev psql -U albumuser -d albumapp < ./backups/albumapp_backup.sql
```