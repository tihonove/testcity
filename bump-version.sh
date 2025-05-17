#!/bin/bash

# Script for version bumping (major, minor or patch) in Chart.yaml
# Also creates a corresponding git tag
# Usage: ./bump-version.sh <major|minor|patch>

set -e

# Checking arguments
if [ $# -ne 1 ]; then
    echo "Usage: $0 <major|minor|patch>"
    exit 1
fi

VERSION_TYPE=$1

if [[ "$VERSION_TYPE" != "major" && "$VERSION_TYPE" != "minor" && "$VERSION_TYPE" != "patch" ]]; then
    echo "Error: Invalid version type specified. Use major, minor or patch."
    exit 1
fi

# Checking for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo "Error: There are uncommitted changes in the repository. Please commit them first."
    git status
    exit 1
fi

# Path to Chart.yaml file
CHART_FILE="./charts/testcity/Chart.yaml"

# Checking if file exists
if [ ! -f "$CHART_FILE" ]; then
    echo "Error: File $CHART_FILE not found."
    exit 1
fi

# Getting current version
CURRENT_VERSION=$(grep "appVersion:" "$CHART_FILE" | awk '{print $2}')
echo "Current version: $CURRENT_VERSION"

# Splitting version into components
IFS='.' read -r -a VERSION_PARTS <<< "$CURRENT_VERSION"
MAJOR=${VERSION_PARTS[0]}
MINOR=${VERSION_PARTS[1]}
PATCH=${VERSION_PARTS[2]}

# Incrementing the selected version part
case "$VERSION_TYPE" in
    "major")
        MAJOR=$((MAJOR + 1))
        MINOR=0
        PATCH=0
        ;;
    "minor")
        MINOR=$((MINOR + 1))
        PATCH=0
        ;;
    "patch")
        PATCH=$((PATCH + 1))
        ;;
esac

# Forming the new version
NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "New version: $NEW_VERSION"

# Updating version in Chart.yaml file
sed -i "s/^version:.*/version: $NEW_VERSION/" "$CHART_FILE"
sed -i "s/^appVersion:.*/appVersion: $NEW_VERSION/" "$CHART_FILE"

echo "Version in $CHART_FILE updated."

# Committing changes
git add "$CHART_FILE"
git commit -m "Bump version to $NEW_VERSION"

# Creating a tag
TAG_NAME="v$NEW_VERSION"
git tag "$TAG_NAME"

echo "Tag $TAG_NAME created"
echo "To push changes to the server, use:"
echo "git push && git push --tags"
