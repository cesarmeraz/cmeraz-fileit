# cmeraz-fileit

a service bus test

## Quickstart (Github Codespace instructions)

1. Launch a Codespace from Confirm in VS Code that the image is loaded and the build is complete.
2. Run bash script to submit a test file through the program, scripts/crons_local/simple-test.sh

## Introduction

This repository is a small example project demonstrating a service-bus-focused workflow using .NET and Azure Functions. It contains an API (Azure Functions), a shared application library with models and providers, unit tests, and a small command-line tool. The code and scripts are organized to make local development and testing straightforward, including helpers for local storage emulation and sample data generation.

## Top-level projects and folders

- `api/` — Azure Functions project (contains `Api.csproj`, function classes under `Functions/`, configuration files like `host.json` and `local.settings.json`, and deployment/build outputs).
- `app/` — Shared application library (contains `App.csproj`, `Models/`, `Providers/`, and `Services/`). Business logic and reusable components live here.
- `test/` — Unit test project (`Test.csproj`) containing tests for providers and services.
- `tool/` — Small CLI/tool project (`Tool.csproj`) with helper utilities or local tooling.
- `scripts/` — Collection of shell scripts used for local setup, azcopy helpers, Azurite startup, cron job examples, and provisioning helpers.
- `endpoints/` — Example HTTP request files (useful with the VS Code REST Client extension) for exercising APIs (e.g., `UploadFile.http`).
- `templates/` — Infrastructure templates (Bicep) used for deploying resources to Azure.
- `principals/` and `resources/` — Scripts for creating service principals and provisioning cloud resources.

Each folder includes project files and/or scripts to make local dev and CI runs reproducible. If you are exploring the code, start with `api/` and `app/` to see how functions and business logic interact.

## Notes and tips for Codespaces

- Use the VS Code Tasks panel; the workspace contains a `start` task that runs clean, test, build, and then `func start` for the `api` project. Running the single `start` task will reproduce the behavior above.
- Expose and inspect forwarded ports in the Codespaces Ports view (useful for Functions runtime, local storage emulators, or web UIs).
- Local configuration: prefer `appsettings.Local.json` and `local.settings.json` for developer overrides. Do not commit secrets — use Codespaces secrets or environment variables.
- Storage and blobs: for local storage emulation, use `scripts/azurite/start.sh` to run Azurite, and use `scripts/crons_local/simple-source.sh` to create the sample containers and example blobs used by the project.
