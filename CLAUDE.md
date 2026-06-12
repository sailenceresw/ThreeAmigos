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

`dotnet-ef` is pinned as a local tool in `.config/dotnet-tools.json` (v8.0.27,
kept in lockstep with the EF Core runtime packages). Restore it before use:

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
- **Services/Payments/** — Pluggable payment subsystem. `IPaymentProvider` has
  three implementations (`InAppBalance`, `StripeCard`, `CryptoManual`) registered
  as a multi-binding (`AddScoped<IPaymentProvider, …>` ×3, resolved by `Method`
  property). `IPaymentService` (`PaymentService`) owns the order-finalize and
  refund logic (stock decrement/restock, statement entry for in-app-balance,
  status transition, confirmation/refund email) and is the only thing allowed to
  mark a `Payment` as Succeeded/Failed/Refunded. `PaymentController` exposes
  `Select/Begin`, the Stripe webhook (signature-verified, idempotent via
  `PaymentEvent` unique index on `Source+ExternalEventId`), `SubmitCryptoTx`,
  manual-crypto Confirm/Reject endpoints, and an admin payments view with a
  Refund action that calls Stripe's Refunds API (or credits back the in-app
  balance, or records intent for manual crypto refunds).

### Crypto on-chain verification

When a user picks crypto at checkout, `CryptoManualPaymentProvider` locks the
USD→crypto rate via `IPriceOracle` (`CoinGeckoPriceOracle`, free tier, no key)
and stores `Payment.CryptoExpectedAmount` so the Crypto checkout view shows the
user an exact amount to send. After the user submits a tx hash,
`CryptoVerificationHostedService` (a `BackgroundService` registered in
`Program.cs`) polls every `Payments:Crypto:Verification:PollIntervalMinutes`
minutes:

- `BitcoinChainVerifier` calls mempool.space (`/api/tx/{hash}` and tip height)
  to verify recipient + sum of outputs + confirmations. No API key required.
- `EthereumChainVerifier` calls Etherscan (`eth_getTransactionByHash`,
  `eth_getTransactionReceipt`, `eth_blockNumber`) — **requires** an API key in
  `Payments:Crypto:Verification:EtherscanApiKey`. Native ETH only; ERC-20
  tokens are not parsed.
- `IChainVerifierResolver` picks the verifier by `Payment.CryptoCurrency`.
- Outcomes: `Confirmed` → `MarkSucceededAsync`; `Rejected` (wrong recipient,
  amount drift beyond tolerance, on-chain revert) → `MarkFailedAsync`;
  `Pending` / `TxNotFound` / `Unknown` → leave status alone and record
  `CryptoReceivedAmount`, `CryptoConfirmations`, `LastVerifiedAt`,
  `VerificationAttempts` so the admin review page sees the latest state.

Auto-verification is off by default (`AutoVerifyEnabled: false`) and the admin
Confirm/Reject controls remain available as a manual fallback for unsupported
chains or stuck verifications.

### Refunds

`PaymentService.RefundAsync(paymentId, reason, adminUserId)` only operates on
`Succeeded` payments. It calls `IPaymentProvider.RefundAsync` (which is a no-op
for in-app balance, calls Stripe's `RefundService` for cards, and just signals
"requires manual settlement" for crypto), then in a single DB transaction:
restocks the order's items, credits the in-app balance back (with a positive
`Statement`) for `InAppBalance` payments, sets `Order.Status="REFUNDED"`, and
stamps `Payment.Refunded*` fields. The Stripe webhook also handles
`charge.refunded` / `charge.refund.updated` so out-of-band refunds initiated
from the Stripe Dashboard reconcile through `MarkRefundedFromExternalAsync`
(same domain effects, no second Stripe API call).

### Checkout / payment flow

`OrderController.checkout` POST no longer debits balance or decrements stock
inline. It validates **available** stock (Product.Quantity minus active
`StockReservation` holds via `IStockReservationService.GetAvailableQuantityAsync`),
creates the `Order` in `Status="AWAITING_PAYMENT"`, snapshots `OrderItem`
prices, creates the `Shipment`, **reserves stock** (TTL from
`Stock:ReservationTtlMinutes`), and redirects to `Payment/Select?orderId=…`.
The payment provider chosen there debits / charges through
`IPaymentProvider.InitiateAsync`; finalization runs in
`PaymentService.MarkSucceededAsync` (called inline for in-app balance, via
Stripe webhook for cards, via the admin confirm endpoint or auto-verifier for
crypto). `FinalizeOrderAsync` decrements `Product.Quantity` and consumes the
reservations; `MarkFailedAsync` releases them.

### Stock reservation

`StockReservation` rows hold stock between `AWAITING_PAYMENT` order creation
and payment-success finalization, closing the oversell race. Available
quantity = `Product.Quantity − Σ(active reservations)` where "active" means
`ConsumedAt IS NULL AND ReleasedAt IS NULL AND ExpiresAt > now`.
`StockReservationSweeperHostedService` runs every `Stock:SweepIntervalMinutes`
to release expired reservations (cart abandonment / closed-tab scenarios). A
reservation never alters `Product.Quantity` — the literal stock decrement
happens only in `FinalizeOrderAsync`.

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
- **Payment secrets must not live in `appsettings.json`.** The `Payments`
  section there is placeholders only (`Enabled: false`, empty keys). Real
  Stripe keys / crypto wallet addresses go in user-secrets or environment
  variables and override the section at runtime:

  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "Payments:Stripe:Enabled" "true"
  dotnet user-secrets set "Payments:Stripe:PublishableKey" "pk_test_..."
  dotnet user-secrets set "Payments:Stripe:SecretKey"      "sk_test_..."
  dotnet user-secrets set "Payments:Stripe:WebhookSecret"  "whsec_..."
  dotnet user-secrets set "Payments:Crypto:Enabled" "true"
  # Crypto wallets are an array — use Payments:Crypto:Wallets:0:Currency etc.
  ```

  In Azure / production, set the same keys as App Service configuration. The
  Stripe webhook lives at `POST /Payment/StripeWebhook` and is
  `[AllowAnonymous]` + signature-verified.
- **EF migration covers Payment, PaymentEvent, StockReservation, plus refund
  and crypto-verification columns on Payment.** After pulling:

  ```bash
  dotnet tool restore
  dotnet dotnet-ef migrations add AddPaymentsAndStock
  dotnet dotnet-ef database update
  ```

  If you generated/applied `AddPayments` before the refund / verification /
  reservation work landed, add follow-up migrations instead — EF will detect
  each additive change and emit a delta-only migration.

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
