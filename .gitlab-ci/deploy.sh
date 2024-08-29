#!/bin/sh

current_version="2.1.0-$(date +"%Y%m%d%H%M")"

houston pack Api/Api.csproj --version $current_version --build-timeout 00:03:00
houston push binaries -a $HOUSTON2_API_TOKEN -t $HOUSTON2_TEAM_ID -p Api/Kontur.TestAnalytics.Api.$current_version.nupkg
houston create release -a $HOUSTON2_API_TOKEN --application $HOUSTON2_APP_ID --binaries-name Kontur.TestAnalytics.Api --binaries-version $current_version -v $current_version
houston deploy -a $HOUSTON2_API_TOKEN --application $HOUSTON2_APP_ID -v $current_version -e Cloud --wait --created "$GITLAB_USER_NAME"