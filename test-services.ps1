# Album App サービステストスクリプト

Write-Host "=== Album App サービステスト ===" -ForegroundColor Green

# PostgreSQL接続テスト
Write-Host "`n1. PostgreSQL接続テスト..." -ForegroundColor Yellow
try {
    $pgTest = Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue
    if ($pgTest.TcpTestSucceeded) {
        Write-Host "✅ PostgreSQL (ポート5432): 接続成功" -ForegroundColor Green
    } else {
        Write-Host "❌ PostgreSQL (ポート5432): 接続失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ PostgreSQL (ポート5432): 接続失敗" -ForegroundColor Red
}

# バックエンドAPI健康チェック
Write-Host "`n2. バックエンドAPI健康チェック..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        $healthData = $response.Content | ConvertFrom-Json
        Write-Host "✅ バックエンドAPI (ポート5000): 正常" -ForegroundColor Green
        Write-Host "   ステータス: $($healthData.status)" -ForegroundColor Cyan
        Write-Host "   タイムスタンプ: $($healthData.timestamp)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ バックエンドAPI (ポート5000): 異常 (ステータス: $($response.StatusCode))" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ バックエンドAPI (ポート5000): 接続失敗" -ForegroundColor Red
    Write-Host "   エラー: $($_.Exception.Message)" -ForegroundColor Red
}

# Swagger UIテスト
Write-Host "`n3. Swagger UIテスト..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "http://localhost:5000/swagger" -Method GET -TimeoutSec 10
    if ($swaggerResponse.StatusCode -eq 200 -and $swaggerResponse.Content -like "*Swagger UI*") {
        Write-Host "✅ Swagger UI: 利用可能" -ForegroundColor Green
    } else {
        Write-Host "❌ Swagger UI: 利用不可" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Swagger UI: 接続失敗" -ForegroundColor Red
}

# フロントエンド接続テスト
Write-Host "`n4. フロントエンド接続テスト..." -ForegroundColor Yellow
try {
    $frontendTest = Test-NetConnection -ComputerName localhost -Port 4200 -WarningAction SilentlyContinue
    if ($frontendTest.TcpTestSucceeded) {
        Write-Host "✅ Angular フロントエンド (ポート4200): 接続成功" -ForegroundColor Green
    } else {
        Write-Host "❌ Angular フロントエンド (ポート4200): 接続失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Angular フロントエンド (ポート4200): 接続失敗" -ForegroundColor Red
}

# Podmanコンテナ状態確認
Write-Host "`n5. Podmanコンテナ状態確認..." -ForegroundColor Yellow
try {
    $containers = podman ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    Write-Host $containers -ForegroundColor Cyan
} catch {
    Write-Host "❌ Podmanコンテナ情報の取得に失敗" -ForegroundColor Red
}

Write-Host "`n=== テスト完了 ===" -ForegroundColor Green
Write-Host "`nアクセス情報:" -ForegroundColor Yellow
Write-Host "- フロントエンド: http://localhost:4200" -ForegroundColor Cyan
Write-Host "- バックエンドAPI: http://localhost:5000" -ForegroundColor Cyan
Write-Host "- Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "- PostgreSQL: localhost:5432" -ForegroundColor Cyan