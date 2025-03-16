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
   - Обеспечивает навигацию по проектам и 
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

Для сообщений о проблемах используйте [Issue Tracker](https://git.skbkontur.ru/forms/test-analytics/-/issues) в GitLab.

## Разработка с использованием DevContainer

Проект поддерживает разработку с использованием [DevContainer](https://containers.dev/), что позволяет быстро начать разработку без установки зависимостей на локальной машине.

### Требования для работы с DevContainer

- [Docker](https://www.docker.com/products/docker-desktop/) 
- [Visual Studio Code](https://code.visualstudio.com/)
- Расширение [Remote Development](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack)

или

- [JetBrains Rider](https://www.jetbrains.com/rider/) (2022.3+)

### Начало работы с DevContainer

#### Visual Studio Code

1. Установите необходимые инструменты, перечисленные выше
2. Клонируйте репозиторий:
   ```bash
   git clone git@git.skbkontur.ru:forms/test-analytics.git
   ```
3. Откройте клонированный репозиторий в VS Code
4. VS Code автоматически определит наличие конфигурации DevContainer и предложит открыть проект в контейнере
5. Нажмите на кнопку "Reopen in Container" или выберите команду "Remote-Containers: Open Folder in Container" через палитру команд (F1)
6. Дождитесь завершения сборки и настройки контейнера

#### JetBrains Rider

0. Тоже может, но как по мне -- сплошная боль
1. Если кто-то пробует -- опишите тут инструкцию

После этого вы окажетесь в полностью настроенной среде разработки, где можно сразу начать работу с проектом. Для запуска проекта надо запустить ./start-all.sh в контейнере через консоль в среде или через подключение к контейнеру.

### Дополнительная информация

- [Официальная документация по DevContainer](https://code.visualstudio.com/docs/devcontainers/containers)
- [Документация JetBrains по Dev Containers](https://www.jetbrains.com/help/rider/dev-environments-devcontainers.html)
- [Спецификация DevContainer](https://containers.dev/)
- [Коллекция готовых DevContainer конфигураций](https://github.com/microsoft/vscode-dev-containers)