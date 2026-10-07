# AcxiomCRM

AcxiomCRM is a role-based customer relationship management application built with ASP.NET Core MVC, Entity Framework Core, and ASP.NET Core Identity.

## Project contents

This repository contains:

- ASP.NET Core MVC application
- Entity Framework Core data model and SQL Server migrations
- ASP.NET Core Identity authentication and role-based authorization
- Customer, lead, opportunity, activity, follow-up, and user management
- Dashboard and reporting features
- Audit log viewing for administrators and managers
- Automated tests and Azure App Service deployment workflow
- Local development and production deployment documentation

## Features

- Customer, lead, opportunity, activity, and follow-up management
- Role-based access for Admin, Manager, and SalesExecutive users
- Reports and sales pipeline analytics
- Audit logging and access control
- Security headers, health checks, and rate limiting
- Responsive Bootstrap interface

## Local setup

### Requirements

- .NET 8 SDK
- SQL Server Express or SQL Server LocalDB
- Access to the `SQLEXPRESS` instance used by the default connection string

### Run the project

```powershell
dotnet restore AcxiomCRM.sln
dotnet build AcxiomCRM.sln --configuration Release
dotnet test AcxiomCRM.sln --configuration Release

dotnet run --project AcxiomCRM.Web
```

The application uses `AcxiomCRM` as its database name. The database and required roles are created automatically when the application starts.

### Demo accounts

The seeded accounts are for local development only:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.local` | `Admin@123` |
| Manager | `manager@acxiomcrm.local` | `Manager@123` |
| Sales Executive | `sales@acxiomcrm.local` | `Sales@123` |

Audit Logs are available to Admin and Manager accounts.

## Production deployment

The deployment workflow automatically builds, tests, publishes, and deploys the application when changes are pushed to the `main` branch.

Set the following GitHub repository secrets before enabling deployment:

- `AZURE_CREDENTIALS`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

The production database connection string must be stored in an Azure App Service application setting named `ConnectionStrings__DefaultConnection`.

## Security and privacy

- Do not commit real passwords, connection strings, API keys, or private files.
- The repository includes no production database credentials.
- Authentication cookies are HTTP-only and secure in production.
- Database migrations run automatically when the application starts.
- ASP.NET Core Identity is used for authentication and authorization.
- Security headers, rate limiting, and role-based access controls are configured.

## GitHub repository status

The project source, tests, workflow, database migrations, documentation, and required build configuration are uploaded to GitHub. Personal files were not included.

For the complete production setup, see [DEPLOYMENT.md](DEPLOYMENT.md).
