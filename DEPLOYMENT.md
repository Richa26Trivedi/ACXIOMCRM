# Deployment Guide

## GitHub prerequisites

1. Initialize the project as a Git repository.
2. Add the project to GitHub.
3. Create a production Azure App Service.
4. Configure the repository `main` branch protection rules.
5. Add the required deployment secrets:
   - `AZURE_CREDENTIALS`
   - `AZURE_WEBAPP_NAME`
   - `AZURE_WEBAPP_PUBLISH_PROFILE`

## Azure App Service configuration

Set the following App Service application settings:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=<production-sql-connection-string>`
- `WEBSITE_RUN_FROM_PACKAGE=1`

The production database must be available before the first deployment. The application applies EF Core migrations automatically.

## Deployment

Push to the `main` branch or run the manual workflow from the GitHub Actions tab. The workflow:

1. Restores and builds the solution.
2. Executes all tests.
3. Deploys the published application to Azure App Service.

## Required post-deployment checks

- Visit `/health` and confirm HTTP 200.
- Sign in with an administrator account.
- Create, edit, and delete a customer, lead, and opportunity.
- Open the reports page and confirm chart data loads.
- Confirm security headers and rate limiting behavior.
