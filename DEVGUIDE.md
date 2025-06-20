# TestCity Architecture

## Component Overview

TestCity consists of several key components that interact to provide full functionality:

1. **Crawler** - a component that:
   - Periodically reads job data for projects that don't work with webhooks
   - Sends collected data to Kafka

2. **API** - a .NET service that:
   - Proxies requests from the frontend to the database or GitLab
   - Interacts with GitLab API to obtain additional data
   - Receives data from GitLab via WebHooks
   - Sends the received data to Kafka

3. **Kafka** - message queue:
   - Provides asynchronous data transfer between components
   - Receives data from the Crawler and API
   - Delivers data to the Worker for processing

4. **Worker** - a component that:
   - Reads data from Kafka
   - Extracts data from GitLab (projects, pipelines, jobs)
   - Downloads artifacts with test results
   - Parses results and transforms them into a structured format
   - Saves processed data to the database

5. **Database (ClickHouse)**:
   - Stores data of job runs and individual tests
   - Documentation: [ClickHouse](https://clickhouse.com/docs)

6. **Frontend** - user web interface:
   - Provides interactive dashboards and charts
   - Enables navigation across projects
   - In production, it's served via Nginx

In the production environment, requests to static files and API are separated by a load balancer (Ingress in Kubernetes).

## Interaction Diagram

```
GitLab  <----+ -------------+
    |                       |
    v                       |
 Crawler     +---------- WebHooks
    |        |              |
    v        |              v
   Kafka <---+             API <----> Frontend <----> User
    |                       |
    v                       |
  Worker                    |
    |                       |
    v                       |
ClickHouse <----------------+
```

## Development Environment

### Required Software:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) - for backend development
- [Node.js 20+](https://nodejs.org/en/download/) - for frontend development
- [Docker](https://www.docker.com/products/docker-desktop/) - for running databases and other services
- [Task](https://taskfile.dev/installation/) (taskfile.dev) - for command automation

### Setting up Local Environment

1. Clone the repository:
   ```bash
   git clone git@github.com:tihonove/testcity.git
   cd testcity
   ```

2. Create a `.env` file based on `.env.example`:
   ```bash
   cp .env.example .env
   ```

3. Edit the `.env` file:
   - Get a GitLab Token at: {GITLAB_URL}/-/user_settings/personal_access_tokens

## Running the Project

There are two main development modes:

### 1. Full Environment (API + DB + Kafka + Worker + Frontend)

This mode launches all components:
- ClickHouse via Docker Compose
- Kafka via Docker Compose
- Backend API on the local host
- Worker on the local host
- Frontend in development mode with hot-reload

```bash
# In the project root directory
./start-all.sh
```

After startup:
- Frontend will be available at: http://localhost:8080
- API will be available at: http://localhost:8124
- ClickHouse will be available at: http://localhost:8123
- Kafka will be available at: localhost:9092

### 2. Frontend Only (connected to production API)

This mode is suitable when you're working only on the Frontend and want to use an existing API:

```bash
# Navigate to the Frontend directory
cd Front

# Start the Frontend that will connect to the production API
npm run start-prod-api
```

**Important**: Make sure that the correct API URL is specified in the `webpack.config.prod-api.js` file.

## Troubleshooting

- If you have problems accessing the GitLab API, check your GitLab Token
- For ClickHouse issues, check connection settings and if the container is running
- For Kafka issues, check container logs and connection settings
- For Worker problems, check logs and Kafka connection
- For Frontend issues, verify the Node.js version (should be 20+)

For issue reports, use our [Issue Tracker](https://github.com/tihonove/testcity/issues) on GitHub.

## Development with DevContainer

The project supports development using [DevContainer](https://containers.dev/), allowing you to quickly start development without installing dependencies on your local machine.

### Requirements for Working with DevContainer

- [Docker](https://www.docker.com/products/docker-desktop/)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Remote Development](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack) extension

or

- [JetBrains Rider](https://www.jetbrains.com/rider/) (2022.3+)

### Getting Started with DevContainer

#### Visual Studio Code

1. Install the required tools listed above
2. Clone the repository:
   ```bash
   git clone git@github.com:tihonove/testcity.git
   ```
3. Open the cloned repository in VS Code
4. VS Code will automatically detect the DevContainer configuration and suggest opening the project in a container
5. Click the "Reopen in Container" button or select "Remote-Containers: Open Folder in Container" via the command palette (F1)
6. Wait for the container to build and configure

#### JetBrains Rider

0. It's also possible, but in my opinion — quite painful
1. If someone tries it — please add instructions here

After this, you'll be in a fully configured development environment where you can immediately start working with the project. To run the project, execute ./start-all.sh in the container through the environment console or via container connection.

### Additional Information

- [Official DevContainer Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [JetBrains Dev Containers Documentation](https://www.jetbrains.com/help/rider/dev-environments-devcontainers.html)
- [DevContainer Specification](https://containers.dev/)
- [Collection of Ready-made DevContainer Configurations](https://github.com/microsoft/vscode-dev-containers)