# RaceDay-Part-2
name: .NET Build and Tests

on:
  push:
  pull_request:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Find solution file
        id: sln
        run: |
          SLN=$(find . -name '*.sln' | head -n 1)
          if [ -z "$SLN" ]; then echo "No .sln file found"; exit 1; fi
          echo "Using $SLN"
          echo "path=$SLN" >> "$GITHUB_OUTPUT"

      - name: Restore
        run: dotnet restore "${{ steps.sln.outputs.path }}"

      - name: Build
        run: dotnet build "${{ steps.sln.outputs.path }}" --no-restore --configuration Release

      - name: Test
        run: dotnet test "${{ steps.sln.outputs.path }}" --no-build --configuration Release --verbosity normal
