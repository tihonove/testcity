# Архитектура TestCity

## Обзор компонентов

TestCity состоит из нескольких ключевых компонентов, которые взаимодействуют для обеспечения полной функциональности:

1. **Краулер** - компонент, который:
   - Извлекает данные из GitLab (проекты, пайплайны, джобы)
   - Скачивает артефакты с результатами тестов
   - Парсит результаты и преобразует их в структурированный формат
   - Сохраняет обработанные данные в базу данных

2. **База данных (ClickHouse)** :
   - Хранит данные запусков job-ов и отдельно взятых тестов
   - Документация: [ClickHouse](https://clickhouse.com/docs)

3. **API** - .NET сервис, который:
   - Проксирует запросы от фронтенда к базе данных или GitLab
   - Взаимодействует с GitLab API для получения дополнительных данных

4. **Frontend** - веб-интерфейс пользователя:
   - Предоставляет интерактивные дашборды и графики
   - Обеспечивает навигацию по проектам и тестам
   - В продакшене обслуживается через Nginx

В продакшен-окружении запросы к статике и API разделяются балансировщиком нагрузки (Ingress в Kubernetes).

## Схема взаимодействия

```
GitLab  <----+ ----+
    |              |
    v              |
 Краулер           |
    |              |
    v              |
ClickHouse <----> API <----> Frontend <----> Пользователь
```

## Среда разработки

### Необходимое ПО: 

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) - для разработки backend
- [Node.js 20+](https://nodejs.org/en/download/) - для разработки frontend
- [Docker](https://www.docker.com/products/docker-desktop/) - для запуска баз данных и других сервисов
- [Task](https://taskfile.dev/installation/) (taskfile.dev) - для автоматизации команд

### Настройка локальной среды

1. Клонировать репозиторий:
   ```bash
   git clone git@git.skbkontur.ru:forms/test-analytics.git
   cd test-analytics
   ```

2. Создайте файл `.env` на основе `.env.example`:
   ```bash
   cp .env.example .env
   ```

3. Отредактируйте `.env` файл:
   - Получите GitLab Token по адресу: https://git.skbkontur.ru/-/user_settings/personal_access_tokens

## Запуск проекта

Есть два основных режима разработки:

### 1. Полное окружение (API + БД + Frontend)

Этот режим запускает все компоненты:
- ClickHouse через Docker Compose
- Backend API на локальном хосте
- Frontend в режиме разработки с hot-reload

```bash
# В корневой директории проекта
./start-all.sh
```

После запуска:
- Frontend будет доступен по адресу: http://localhost:8080
- API будет доступен по адресу: http://localhost:8124
- ClickHouse будет доступен по адресу: http://localhost:8123

### 2. Только Frontend (с подключением к продакшен API)

Этот режим подходит, когда вы работаете только над Frontend и хотите использовать существующий API:

```bash
# Перейдите в директорию Frontend
cd Front

# Запустите Frontend, который будет обращаться к API в продакшене
npm run start-prod-api
```

**Важно**: Проверьте, что в файле `webpack.config.prod-api.js` указан корректный URL API.

## Решение проблем

- Если у вас проблемы с доступом к GitLab API, проверьте правильность GitLab Token
- При проблемах с ClickHouse проверьте настройки подключения и запущен ли контейнер
- Для Frontend проблем, проверьте версию Node.js (должна быть 20+)

Для сообщений о проблемах используйте [Issue Tracker](https://git.skbkontur.ru/kontur-web-studios/test-analytics/-/issues) в GitLab.