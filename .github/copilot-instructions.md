# Tests
Add tests only if I explicitly ask for them.
If I ask you to write tests, follow these rules:
    - Use xUnit framework for writing tests
    - Use GlobalSetup.TestLoggerFactory for loggers in tests
    - Don't create mocks for IGitLabClient unless specifically requested. Use the instance created in SkbKonturGitLabClientProvider and pass GitLabSettings.Default settings to it
    - Create tests in the TestCity.UnitTests project

# Code (English)
Use file-scoped namespaces
All namespaces must begin with TestCity.
Private class fields should be declared at the bottom of the class