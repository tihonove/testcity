# TestCity ![Docker Image Version](https://img.shields.io/docker/v/tihonove/testcity)

A test monitoring and analytics tool similar to TeamCity. TestCity integrates with GitLab to track and analyze test runs, providing clear visualization of testing results.

## Key Features

- Hierarchy: organization by teams, projects, and groups similar to GitLab
- Test runs visualization similar to TeamCity:
  - Branch selection for analysis
  - Test run history
  - Test execution time and failure graphs
  - TODO: Execution trend tracking
  - TODO: Test stability analysis

### Important Information:

- For tests to be visible outside GitLab, they must be included in artifacts, not just in the JUnit report. (For more details, see the [GitLab documentation](https://docs.gitlab.com/api/job_artifacts/#downloading-artifactsreports-files).)
- If your project needs to be accessible to the system account, you must grant access to the service account (check with your system administrator for details).

After adding TestCity, it will begin scanning the specified project and collecting test data.

## Architecture

TestCity consists of several components:
- Crawler: collects data from GitLab for projects that don't work with webhooks
- Worker: extracts data from GitLab, including test artifacts
- Database (ClickHouse): stores structured test data
- API: proxies requests from the frontend to the database/GitLab
- Frontend: web interface for analytics access, with queries directly on pages

## Development

For local development and running TestCity, check the [contributor's guide](DEVGUIDE.md).

The project supports development using DevContainer, allowing you to quickly set up a development environment without installing dependencies locally. Details are in the [guide](DEVGUIDE.md).

Quick start information:

### Full Environment Mode
```bash
# Launch all components: DB, API, and frontend
./start-all.sh
```

### Frontend-Only Mode
```bash
# Navigate to the frontend directory
cd Front

# Start the frontend with connection to the production API
npm run start-prod-api
```

After startup, the web interface will be available at: http://localhost:8080

## System Requirements

- Node.js 20+
- .NET 9 SDK
- Docker
- taskfile.dev

or you can use DevContainer.

## Support

For issue reports or suggestions, use our [Issue Tracker](https://github.com/tihonove/testcity/issues) on GitHub.