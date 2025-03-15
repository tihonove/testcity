FROM mcr-proxy.kontur.host/dotnet/sdk:9.0

RUN apt-get -y update \
    && apt-get install -y curl \
    && apt-get install -y wget \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm config --location=global set registry=https://nexus.kontur.host/repository/kontur-npm-group/ \
    && echo 'node verions:' $(node -v) \
    && echo 'npm version:' $(npm -v) 

# Пока houston cli не умеет работать с .net 9 доставим 8 версию
RUN wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --version 8.0.100 --install-dir /usr/share/dotnet && \
    rm dotnet-install.sh

RUN apt-get install -y \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null && \
    apt-get update && \
    apt-get install -y docker-ce-cli

RUN dotnet tool install -g Houston.CommandLine --add-source https://nexus.kontur.host/repository/nuget/index.json

ENV PATH="${PATH}:/root/.dotnet/tools"