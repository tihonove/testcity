#!/bin/bash
pushd ..
  npm run build
popd

docker build -t tihonove/nginx-clickhouse-proxy:$1 .
docker push tihonove/nginx-clickhouse-proxy:$1