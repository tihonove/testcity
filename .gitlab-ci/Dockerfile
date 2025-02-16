FROM mcr-proxy.kontur.host/dotnet/sdk:9.0

RUN apt-get -y update \
    && apt-get install -y curl \
    && apt-get install -y wget \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm config --location=global set registry=https://nexus.kontur.host/repository/kontur-npm-group/ \
    && echo 'node verions:' $(node -v) \
    && echo 'npm version:' $(npm -v) 

# Загружаем и устанавливаем .NET SDK 8
RUN wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --version 8.0.100 && \
    rm dotnet-install.sh

RUN dotnet tool install -g Houston.CommandLine --add-source https://nexus.kontur.host/repository/nuget/index.json

ENV PATH="${PATH}:/root/.dotnet/tools"