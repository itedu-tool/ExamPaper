# Скрипт для удаления контейнеров, томов и перезагрузки docker compose

Write-Host "⏹️  Остановка и удаление контейнеров docker compose..." -ForegroundColor Yellow
docker compose down -v

Write-Host "🚀 Запуск docker compose..." -ForegroundColor Yellow
docker compose up -d

Write-Host "⏳ Ожидание инициализации PostgreSQL..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "📊 Статус контейнеров:" -ForegroundColor Cyan
docker compose ps

Write-Host "✅ Готово!" -ForegroundColor Green
