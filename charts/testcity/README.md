# TestCity Helm Chart

Этот Helm chart позволяет установить приложение TestCity в Kubernetes кластер.

## Предварительные требования

- Kubernetes 1.19+
- Helm 3.2.0+

## Установка чарта

Для установки чарта с именем релиза `testcity`:

```bash
# Обновляем репозиторий Helm (если чарт находится в репозитории)
# helm repo update

# Устанавливаем чарт
helm install testcity ./charts/testcity \
  --set secrets.gitlab.token=$GITLAB_TOKEN \
  --set secrets.otlp.headers=$OTEL_EXPORTER_OTLP_HEADERS \
  --set global.tag=0.1.0-test.<commit_hash>
```

## Обновление чарта

Для обновления установленного релиза `testcity`:

```bash
helm upgrade testcity ./charts/testcity \
  --set secrets.gitlab.token=$GITLAB_TOKEN \
  --set secrets.otlp.headers=$OTEL_EXPORTER_OTLP_HEADERS \
  --set global.tag=0.1.0-test.<new_commit_hash>
```

## Удаление чарта

Для удаления установленного релиза `testcity`:

```bash
helm uninstall testcity
```

## Параметры

В таблице ниже представлены основные параметры чарта и их значения по умолчанию.

| Параметр | Описание | Значение по умолчанию |
|-----|-----|-----|
| `global.tag` | Глобальный тег образа для всех компонентов | `latest` |
| `global.environment` | Окружение для телеметрии | `cloud` |
| `front.replicaCount` | Количество реплик Frontend | `2` |
| `front.image.repository` | Репозиторий образа Frontend | `tihonove/testcity-front` |
| `api.replicaCount` | Количество реплик API | `2` |
| `api.image.repository` | Репозиторий образа API | `tihonove/testcity-api` |
| `crawler.replicaCount` | Количество реплик Crawler | `1` |
| `crawler.image.repository` | Репозиторий образа Crawler | `tihonove/testcity-crawler` |
| `worker.replicaCount` | Количество реплик Worker | `2` |
| `worker.image.repository` | Репозиторий образа Worker | `tihonove/testcity-worker` |
| `secrets.gitlab.token` | GitLab токен для доступа к API | `""` |
| `secrets.otlp.headers` | Заголовки для OpenTelemetry | `""` |
| `ingress.enabled` | Включить Ingress | `true` |
| `ingress.host` | Хост для Ingress | `testcity.kube.testkontur.ru` |

Полный список параметров смотрите в файле `values.yaml`.

## CI/CD Pipeline

Для автоматизации деплоя в CI/CD pipeline рекомендуется использовать следующие шаги:

1. Сборка и публикация Docker образов
2. Установка/обновление Helm чарта

Пример для GitHub Actions:

```yaml
- name: Deploy to Kubernetes
  run: |
    helm upgrade --install testcity ./charts/testcity \
      --set secrets.gitlab.token=${{ secrets.GITLAB_TOKEN }} \
      --set secrets.otlp.headers=${{ secrets.OTLP_HEADERS }} \
      --set global.tag=0.1.0-test.${{ env.SHORT_SHA }}
```
