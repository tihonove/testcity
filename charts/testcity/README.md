# TestCity Helm Chart

This Helm chart allows you to install the TestCity application in a Kubernetes cluster.

## Prerequisites

- Kubernetes 1.19+
- Helm 3.2.0+

## Installing the Chart

To install the chart with the release name `testcity`:

```bash
# Update the Helm repository (if the chart is located in a repository)
# helm repo update

# Install the chart
helm install testcity ./charts/testcity \
  --set secrets.gitlab.token=$GITLAB_TOKEN \
  --set secrets.otlp.headers=$OTEL_EXPORTER_OTLP_HEADERS \
  --set front.image.tag=0.1.0-test.<commit_hash> \
  --set api.image.tag=0.1.0-test.<commit_hash> \
  --set crawler.image.tag=0.1.0-test.<commit_hash> \
  --set worker.image.tag=0.1.0-test.<commit_hash>
```

## Upgrading the Chart

To upgrade the installed `testcity` release:

```bash
helm upgrade testcity ./charts/testcity \
  --set secrets.gitlab.token=$GITLAB_TOKEN \
  --set secrets.otlp.headers=$OTEL_EXPORTER_OTLP_HEADERS \
  --set front.image.tag=0.1.0-test.<new_commit_hash> \
  --set api.image.tag=0.1.0-test.<new_commit_hash> \
  --set crawler.image.tag=0.1.0-test.<new_commit_hash> \
  --set worker.image.tag=0.1.0-test.<new_commit_hash>
```

## Uninstalling the Chart

To uninstall the `testcity` release:

```bash
helm uninstall testcity
```

## Parameters

The table below shows the main chart parameters and their default values.

| Parameter | Description | Default Value |
|-----|-----|-----|
| `global.environment` | Environment for telemetry | `cloud` |
| `front.replicaCount` | Number of Frontend replicas | `2` |
| `front.image.repository` | Frontend image repository | `tihonove/testcity-front` |
| `front.image.tag` | Frontend image tag | `""` (uses version from Chart.yaml: `1.4.2`) |
| `api.replicaCount` | Number of API replicas | `2` |
| `api.image.repository` | API image repository | `tihonove/testcity-api` |
| `api.image.tag` | API image tag | `""` (uses version from Chart.yaml: `1.4.2`) |
| `crawler.replicaCount` | Number of Crawler replicas | `1` |
| `crawler.image.repository` | Crawler image repository | `tihonove/testcity-crawler` |
| `crawler.image.tag` | Crawler image tag | `""` (uses version from Chart.yaml: `1.4.2`) |
| `worker.replicaCount` | Number of Worker replicas | `2` |
| `worker.image.repository` | Worker image repository | `tihonove/testcity-worker` |
| `worker.image.tag` | Worker image tag | `""` (uses version from Chart.yaml: `1.4.2`) |
| `clickhouse.host` | ClickHouse host | `vm-ch2-stg.dev.kontur.ru` |
| `clickhouse.port` | ClickHouse port | `8123` |
| `clickhouse.db` | ClickHouse database | `test_analytics` |
| `clickhouse.user` | ClickHouse user | `tihonove` |
| `clickhouse.password` | ClickHouse password | `12487562` |
| `gitlab.url` | GitLab URL | `https://gitlab.com/` |
| `kafka.bootstrapServers` | Kafka servers | `""` |
| `otlp.endpoint` | OpenTelemetry endpoint | `""` |
| `graphite.relay.host` | Graphite Relay host | `graphite-relay.skbkontur.ru` |
| `graphite.relay.port` | Graphite Relay port | `2003` |
| `secrets.gitlab.token` | GitLab token for API access | `""` |
| `secrets.otlp.headers` | OpenTelemetry headers | `""` |
| `ingress.enabled` | Enable Ingress | `true` |
| `ingress.className` | Ingress class | `nginx` |
| `ingress.host` | Ingress host | `testcity.kube.testkontur.ru` |

For a complete list of parameters, see the `values.yaml` file.

## CI/CD Pipeline

To automate deployment in CI/CD pipeline, it is recommended to use the following steps:

1. Build and publish Docker images
2. Install/upgrade Helm chart

Example for GitHub Actions:

```yaml
- name: Deploy to Kubernetes
  run: |
    helm upgrade --install testcity ./charts/testcity \
      --set secrets.gitlab.token=${{ secrets.GITLAB_TOKEN }} \
      --set secrets.otlp.headers=${{ secrets.OTLP_HEADERS }} \
      --set front.image.tag=0.1.0-test.${{ env.SHORT_SHA }} \
      --set api.image.tag=0.1.0-test.${{ env.SHORT_SHA }} \
      --set crawler.image.tag=0.1.0-test.${{ env.SHORT_SHA }} \
      --set worker.image.tag=0.1.0-test.${{ env.SHORT_SHA }}
```
