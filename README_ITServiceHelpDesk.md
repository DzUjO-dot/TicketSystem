# ITServiceHelpDesk

ASP.NET Core MVC application for managing IT help desk tickets.  
Project includes:
- ticket management,
- user roles (`Admin`, `Agent`, `User`),
- local technical admin login,
- Microsoft SSO login,
- Entity Framework Core with SQL Server,
- ASP.NET Core Identity,
- audit logs, notifications, comments, attachments,
- Serilog logging to console and files.

## Tech stack
- .NET 9
- ASP.NET Core MVC
- Entity Framework Core 9
- SQL Server / LocalDB
- ASP.NET Core Identity
- OpenID Connect (Microsoft SSO)
- Serilog

## Solution structure

```text
ITServiceHelpDesk.sln
src/
  ITServiceHelpDesk.Web/
    Controllers/         # MVC controllers
    Core/                # shared app logic / core abstractions
    Data/                # DbContext and database layer
    Extensions/          # extension methods
    Helpers/             # helper classes
    Infrastructure/      # identity, middleware, DI setup
    Middleware/          # custom middleware
    Migrations/          # EF Core migrations
    Models/              # entities, enums, view models
    Services/            # application services + interfaces
    ViewComponents/      # reusable MVC components
    Views/               # Razor views
    wwwroot/             # css, js, static assets
    Program.cs           # app startup and DI configuration
    appsettings.json     # main configuration (do not commit secrets)
    appsettings.Development.json
```

## What is what

### Controllers
- `HomeController` - landing page, privacy, error page.
- `AccountController` - local admin login, Microsoft SSO login, logout, external login callback.
- `DashboardController` - redirects users to dashboard based on role and serves `Admin`, `Agent`, `User` dashboards.
- `TicketsController` - ticket list, my tickets, assigned tickets, unassigned tickets, create/edit/details workflow.
- `AdminController` - user management, roles, categories, administration actions.

### Data layer
- `ApplicationDbContext` - main EF Core context.
- Database stores users, roles, tickets, comments, attachments, histories, notifications, categories and audit logs.

### Main entities
- `ApplicationUser` - app user integrated with ASP.NET Identity.
- `Ticket` - main help desk ticket.
- `TicketComment` - comments under ticket.
- `TicketAttachment` - uploaded files.
- `TicketHistory` - change history.
- `Category` - ticket category.
- `Notification` - in-app notifications.
- `AuditLog` - activity log.

### Services
- `TicketService` - business logic for tickets.
- `CategoryService` - category operations.
- `NotificationService` - notifications.
- `AuditService` - audit entries.
- `EmailService` - email sending.
- `FileService` - uploaded file handling.

### Infrastructure
- `IdentitySeeder` - creates roles, technical admin and default categories on startup.
- custom middleware handles global exceptions.
- extension methods register app services in DI.

## Authentication

Application supports two login methods:

1. **Local login** - only for the technical admin account:
   - login: `admin@helpdesk.local`
   - password seeded in code during first startup.

2. **Microsoft SSO** - for normal users through OpenID Connect.

## Default seeded data
On startup the app:
- applies pending EF migrations,
- creates roles: `Admin`, `Agent`, `User`,
- creates technical admin account,
- seeds ticket categories.

## Requirements
- Visual Studio 2022 / 2026 or VS Code
- .NET 9 SDK
- SQL Server LocalDB or SQL Server
- optional: Azure App Registration for Microsoft SSO

## How to run locally

### 1. Clone repository
```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.git
cd YOUR_REPOSITORY
```

### 2. Go to project folder
```bash
cd src/ITServiceHelpDesk.Web
```

### 3. Restore packages
```bash
dotnet restore
```

### 4. Configure appsettings
Create your own configuration values in:
- `appsettings.json`
- or `appsettings.Development.json`

At minimum configure:
- `ConnectionStrings:DefaultConnection`
- `AzureAd:TenantId`
- `AzureAd:ClientId`
- `AzureAd:ClientSecret`
- `AzureAd:CallbackPath`

### 5. Update database
From the project directory:
```bash
dotnet ef database update
```

If `dotnet ef` is missing:
```bash
dotnet tool install --global dotnet-ef
```

### 6. Run application
```bash
dotnet run
```

Application should start on URLs from `Properties/launchSettings.json`, for example:
- `https://localhost:61291`
- `http://localhost:61292`

## Recommended configuration approach
Do **not** store real secrets in GitHub.
Use one of these options:
- `appsettings.json` with placeholder values,
- environment variables,
- .NET user secrets in local development,
- Azure App Service configuration / Key Vault in production.

### Example safe `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ITServiceHelpDesk;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-microsoft"
  }
}
```

## Database notes
Current configuration uses SQL Server with LocalDB in development.
Default development database names seen in the project:
- `ITServiceHelpDesk`
- `ITServiceHelpDesk_Dev`

## Logging
Serilog is configured to log:
- to console,
- to `Logs/log-.txt` with daily rolling files.

## Important before publishing to GitHub
This repository should **not** contain:
- `bin/`
- `obj/`
- `.vs/`
- `Logs/`
- real secrets in `appsettings*.json`
- user-specific files like `*.csproj.user`

## Suggested first commit workflow
1. remove unnecessary generated folders,
2. replace secrets with placeholders,
3. add `.gitignore`,
4. verify app builds locally,
5. commit and push.

## GitHub publish steps

### Option A - Visual Studio
1. Open solution in Visual Studio.
2. Right click solution.
3. Choose **Add Solution to Source Control** if needed.
4. Open **Git Changes**.
5. Stage files.
6. Write commit message.
7. Click **Commit All**.
8. Click **Push** or **Publish to GitHub**.
9. Select the repository you already created.

### Option B - Git command line
From project root where `.sln` is located:
```bash
git init
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.git
git add .
git commit -m "Initial commit - ITServiceHelpDesk"
git push -u origin main
```

If repository is already initialized locally, use only:
```bash
git add .
git commit -m "Initial commit - ITServiceHelpDesk"
git push -u origin main
```

## Recommended repository cleanup before push
Delete or exclude generated folders if they exist:
```text
obj/
bin/
.vs/
Logs/
```

Also check if the repo accidentally contains duplicated static folders outside the main web project.
Main working app appears to be inside:
```text
src/ITServiceHelpDesk.Web
```

## Suggested repo name
- `ITServiceHelpDesk`
- `it-service-helpdesk`
- `helpdesk-dotnet`

## Future improvements
- move secrets to user-secrets / env vars,
- add `appsettings.Example.json`,
- add screenshots to README,
- add CI workflow for build validation,
- add deployment instructions for Azure / IIS.
