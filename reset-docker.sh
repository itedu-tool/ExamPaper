#!/bin/bash

# Скрипт для удаления контейнеров, томов и перезагрузки docker compose

echo "⏹️  Остановка и удаление контейнеров docker compose..."
docker compose down -v

echo "🚀 Запуск docker compose..."
docker compose up -d

echo "⏳ Ожидание инициализации PostgreSQL..."
sleep 15

echo "📊 Статус контейнеров:"
docker compose ps

echo "✅ Готово!"
