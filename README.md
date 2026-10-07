# AcxiomCRM

AcxiomCRM is a role-based customer relationship management application built with ASP.NET Core MVC, Entity Framework Core, and ASP.NET Core Identity.

## Features

- Customer, lead, opportunity, activity, and follow-up management
- Role-based access for Admin, Manager, and SalesExecutive users
- Reports and sales pipeline analytics
- Audit logging and access control
- Security headers, health checks, and rate limiting
- Responsive Bootstrap interface

## Local development

1. Install .NET 8 SDK and SQL Server LocalDB.
2. Create the `AcxiomCRM` database with LocalDB.
3. Configure the connection string locally using `ConnectionStrings__DefaultConnection`.
4. Run:

   ```powershell
   dotnet restore
   dotnet build
   dotnet test
   dotnet run --project AcxiomCRM.Web
   ```

## Production configuration

The application reads its production database connection from the `ConnectionStrings__DefaultConnection` environment variable. Set the following GitHub repository secrets before deploying:

- `AZURE_CREDENTIALS`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`

The deployment workflow runs on pushes to the `main` branch or can be started manually.

## Security notes

- Never commit database passwords or connection strings.
- The application uses secure HTTP-only authentication cookies in production.
- Database migrations are applied automatically when the application starts.
- Identity is backed by SQL Server.
