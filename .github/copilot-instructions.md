# Тесты
Для написания тестов используюй NUnit, если я прошу написать тесты
Для логгеров в тестах используй GlobalSetup.TestLoggerFactory
Для ассертов в тестах используй новый синтаксис через Assert.That
Не делай mock-ов для IGitLabClient, если не попросят отдельно. Используй экземпляр, создаваемый в SkbKonturGitLabClientProvider, а ему передавай настройки GitLabSettings.Default
Тесты создавай в проекте TestCity.UnitTests

# Код
Используй file-scoped namespaces
Все неймспейсы должны начинаться с Kontur.
Приватные поля класса должны быть объявлены в самом низу класса