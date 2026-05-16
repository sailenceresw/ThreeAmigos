# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ThreeAmigos is an ASP.NET Core 8.0 MVC e-commerce web application written in C#.
The project/assembly is `ecommerce` (see `ecommerce.csproj`) and the root
namespace is `ecommerce`. Persistence is EF Core (code-first) against SQL Server.

## Commands

All commands run from the repository root.

```bash
dotnet restore                       # restore NuGet packages
dotnet build                         # build (Debug)
dotnet build -c Release              # build (Release, as CI does)
dotnet run                           # run; http profile serves http://localhost:3000
dotnet watch run                     # run with hot reload (VS Code "DotNetAutoAttach" launch profile)
dotnet publish -c Release -o ./publish
```

There is no test project in this repository — no `dotnet test` target exists.

### EF Core migrations

`dotnet-ef` is pinned as a local tool in `.config/dotnet-tools.json` (v8.0.6).
Restore it before use:

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name>
dotnet dotnet-ef database update
```

The DbContext is `ecommerce.Models.Context`; migrations live in `Migrations/`
(currently a single baseline migration `20240708155356_All`).

## Architecture

Classic 3-tier MVC with a per-entity layering:

```
Controller  ->  I<Entity>Service / <Entity>Service  ->  I<Entity>Repository / <Entity>Repository  ->  Context (EF Core / SQL Server)
```

- **Models/** — EF entities + `Context` (`IdentityDbContext<ApplicationUser>`).
  Decimal precision and the unique `ApplicationUser.Email` index are configured
  in `Context.OnModelCreating`.
- **Repository/** — Generic `Repository<T>` base implements `IRepository<T>`
  (CRUD + `Save()`). Per-entity repositories extend it and **re-declare/override**
  the CRUD methods (often re-implementing rather than calling `base`); follow that
  existing pattern when adding entities. Note `Get(Func<T,bool> where)` takes a
  `Func` (not `Expression`), so it filters in memory after loading the set —
  preserve or improve deliberately, don't assume DB-side filtering.
- **Services/** — One interface + implementation per entity. Services mostly
  delegate to repositories but also own ViewModel<->entity mapping and
  cross-entity logic (e.g. `ProductService` derives `IsParent` / `StockStatus`).
- **ViewModels/** — Shaped DTOs grouped in per-feature subfolders
  (`ViewModels/Product/`, `ViewModels/Home/`, …). Used to pass composed data to
  Razor views; controllers/services build these rather than passing entities
  directly.
- **Controllers/** + **Views/** — Conventional MVC. View folders mirror
  controller names. Default route is `{controller=Home}/{action=Index}/{id?}`.
  Note the controller/view spelling `Dashbourd` (not "Dashboard") — match
  existing names.
- **Hubs/CommentHub.cs** — SignalR hub mapped at `/CommentHub` for real-time
  product comments. The client library `signalr.js` is restored via
  `libman.json` into `wwwroot/lib/`.
- **Services/BackgroundService.cs** — `TimedHostedService` (registered as a
  hosted service) runs three timer jobs that create their own DI scope:
  product price +10% for items sold the previous day, stock-status refresh, and
  product-visibility dedup (lowest-priced product in a same-name group becomes
  `IsParent`).

### Dependency injection

Everything is wired manually in `Program.cs`. When adding a service or
repository, register the interface→implementation pair there (`AddScoped` is the
norm). The DI block has duplicate registrations and inconsistent ordering — keep
new registrations grouped logically rather than replicating the duplication.

### Identity & auth

ASP.NET Identity with `ApplicationUser` / `IdentityRole`, EF stores, default
token providers. Password complexity is intentionally relaxed in `Program.cs`
for testing (no digit/upper/lower/non-alphanumeric requirement); don't tighten
without reason. Email confirmation (`RequireConfirmedAccount`) is commented out.

## Configuration & known gotchas

- **Connection string**: `ConnectionStrings:cs` in `appsettings.json` (Azure
  SQL). The literal credentials currently committed are a project artifact — do
  not treat them as a secret to rotate unless asked, but never add new secrets.
- **Two mail paths, mismatched config keys**: `MailService` binds
  `IOptions<MailSettings>` from the config section **`MailSettings`**, but
  `appsettings.json` only defines **`EmailSettings`** (consumed directly by
  `EmailService`). `MailService` therefore receives empty settings. Be aware of
  this split when touching email; prefer aligning rather than adding a third path.
- **`RoleSeeder.SeedRoles` is never called** from `Program.cs` — the "Admin"
  role is not auto-seeded at startup despite the seeder existing.

## Deployment

- **Dockerfile** — multi-stage build/publish on .NET 8 images, entrypoint
  `dotnet ecommerce.dll`.
- **GitHub Actions** (`.github/workflows/development_three-amigos-ecommerce.yml`)
  — builds Release and deploys to Azure Web App `three-amigos-ecommerce` on push
  to the `development` branch.

## Conventions

- C# nullable reference types and implicit usings are enabled.
- Front-end/Razor files are formatted with Prettier (`.prettierrc`): 4-space
  indent, single quotes, semicolons, 80-col print width.
