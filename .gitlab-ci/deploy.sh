#!/bin/sh

current_version="2.1.0-$(date +"%Y%m%d%H%M")"

houston pack Api/Api.csproj --version $current_version --build-timeout 00:03:00
houston pack GitLabJobsCrawler/GitLabJobsCrawler.csproj --version $current_version --build-timeout 00:03:00
houston push binaries -a $HOUSTON2_API_TOKEN -t $HOUSTON2_TEAM_ID -p Api/Kontur.TestAnalytics.Api.$current_version.nupkg
houston push binaries -a $HOUSTON2_API_TOKEN -t $HOUSTON2_TEAM_ID -p GitLabJobsCrawler/Kontur.TestAnalytics.GitLabJobsCrawler.$current_version.nupkg
houston create releases -a $HOUSTON2_API_TOKEN --scope Экстерн.Формы/TestAnalytics --binaries-version $current_version -v $current_version --path-to-settings-folder .houston-configuration
houston deploy -a $HOUSTON2_API_TOKEN --scope Экстерн.Формы/TestAnalytics -v $current_version -e Cloud --wait --created "$GITLAB_USER_NAME"