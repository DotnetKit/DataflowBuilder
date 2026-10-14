#!/usr/bin/env bash
set -euo pipefail

echo "==> Running dotnet format (whitespace)..."
dotnet format whitespace

echo "==> Running dotnet format (style)..."
dotnet format style

echo "==> Running dotnet format (analyzers)..."
dotnet format analyzers

echo "==> Building with EnforceCodeStyleInBuild..."
dotnet build -p:EnforceCodeStyleInBuild=true

echo "==> Running tests..."
dotnet test --no-build

echo "==> All code formatting and analysis checks passed successfully!"
