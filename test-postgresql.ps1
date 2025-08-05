# PostgreSQL接続テストスクリプト

Write-Host "=== PostgreSQL接続テスト ===" -ForegroundColor Green

# TCP接続テスト
Write-Host "`n1. TCP接続テスト (ポート5432)..." -ForegroundColor Yellow
try {
    $tcpTest = Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue
    if ($tcpTest.TcpTestSucceeded) {
        Write-Host "✅ TCP接続: 成功" -ForegroundColor Green
        Write-Host "   リモートアドレス: $($tcpTest.RemoteAddress)" -ForegroundColor Cyan
        Write-Host "   ソースアドレス: $($tcpTest.SourceAddress)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ TCP接続: 失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ TCP接続: エラー" -ForegroundColor Red
    Write-Host "   エラー: $($_.Exception.Message)" -ForegroundColor Red
}

# Podmanコンテナ内からのデータベース接続テスト
Write-Host "`n2. データベース接続テスト..." -ForegroundColor Yellow
try {
    $dbTest = podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "SELECT 'PostgreSQL Connection Success' as status, current_timestamp as timestamp;" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ データベース接続: 成功" -ForegroundColor Green
        Write-Host "   結果:" -ForegroundColor Cyan
        Write-Host $dbTest -ForegroundColor Cyan
    } else {
        Write-Host "❌ データベース接続: 失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ データベース接続: エラー" -ForegroundColor Red
    Write-Host "   エラー: $($_.Exception.Message)" -ForegroundColor Red
}

# データベース情報の取得
Write-Host "`n3. データベース情報..." -ForegroundColor Yellow
try {
    $dbInfo = podman exec album-app-postgres-dev psql -U albumuser -d albumapp -c "\l" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ データベース一覧取得: 成功" -ForegroundColor Green
        Write-Host "   データベース一覧:" -ForegroundColor Cyan
        Write-Host $dbInfo -ForegroundColor Cyan
    } else {
        Write-Host "❌ データベース一覧取得: 失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ データベース一覧取得: エラー" -ForegroundColor Red
}

Write-Host "`n=== テスト完了 ===" -ForegroundColor Green
Write-Host "`nPostgreSQL接続情報:" -ForegroundColor Yellow
Write-Host "- ホスト: localhost" -ForegroundColor Cyan
Write-Host "- ポート: 5432" -ForegroundColor Cyan
Write-Host "- データベース: albumapp" -ForegroundColor Cyan
Write-Host "- ユーザー: albumuser" -ForegroundColor Cyan
Write-Host "- パスワード: albumpass" -ForegroundColor Cyan

Write-Host "`nWindowsからの接続例:" -ForegroundColor Yellow
Write-Host "psql -h localhost -p 5432 -U albumuser -d albumapp" -ForegroundColor Cyan