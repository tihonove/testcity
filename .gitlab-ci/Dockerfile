FROM mcr-proxy.kontur.host/dotnet/sdk:8.0

RUN apt-get -y update \
    && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm config --location=global set registry=https://nexus.kontur.host/repository/kontur-npm-group/ \
    && echo 'node verions:' $(node -v) \
    && echo 'npm version:' $(npm -v) 

RUN dotnet tool install -g Houston.CommandLine --add-source https://nexus.kontur.host/repository/nuget/index.json

ENV PATH="${PATH}:/root/.dotnet/tools"