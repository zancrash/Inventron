# CLAUDE.md: Inventron

Guidance for Claude Code working in this repository. Read this before making changes.

## What this is

Inventron is a multi-tenant MRP (material requirements planning) SaaS built by a solo developer.

- **Target customer:** small discrete manufacturers and makers who have outgrown spreadsheets or simple COGS tools (e.g. Craftybase): multi-level BOMs, purchased components with lead times, growing order volume, 1–50 staff. Focus on one vertical (leading candidate: small electronics/hardware and audio-gear makers). Avoid features that only regulated verticals need (lot/expiry tracking, compliance) for now.
- **Positioning:** "know what to buy and when," i.e. planning, not just bookkeeping.
- **Ship order:** a narrow paid wedge first (CSV import → BOM explosion → shortage report and buy list), then expand. Prefer the smallest change that gets a paying customer further.
- **Make-or-break feature:** CSV import with clear per-row errors.

## Stack

- **Front end:** React + TypeScript, TanStack Query, TanStack Table (or AG Grid), API client generated from the OpenAPI spec. Never hand-write API types.
- **API:** .NET / C# (`InventronAPI`), ASP.NET Core controllers, EF Core, service layer.
- **Database:** PostgreSQL via Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Runtime:** Docker / Docker Compose for local dev (db, api, web).
- **Background jobs:** Hangfire (`Hangfire.PostgreSql`) or a hosted service.
- **Billing:** Stripe.

## Common commands

> Adjust service names and paths to match `docker-compose.yml` and the solution layout.

```bash
# Start everything
docker compose up -d --build

# Logs
docker compose logs -f api

# API (from the host)
dotnet build
dotnet test                       # needs Docker running (Testcontainers)

# EF Core migrations (run from the host against the compose DB)
dotnet ef migrations add <Name> --project InventronAPI
dotnet ef database update --project InventronAPI

# Front end
cd web && npm install && npm run dev
npm run generate:api              # regenerate the TS client after API changes
npm run lint && npm run typecheck
```

Rules:
- Every schema change gets a migration. Never edit an applied migration; add a new one.
- In development it's fine to wipe the DB volume (`docker compose down -v`). Never do this anywhere else.
- Don't auto-apply migrations at startup in production. Apply them as an explicit step.
- Secrets (DB password, JWT key, Stripe keys) come from environment variables or `.env` (git-ignored). Never commit them.

## Architecture rules (non-negotiable)

### Multi-tenancy
- Shared database, with a `TenantId` (Guid) on every tenant-owned table. `Tenant` is the root and has no `TenantId`.
- All tenant-owned entities inherit `TenantEntity`:
  ```csharp
  public abstract class TenantEntity
  {
      public int Id { get; set; }
      public required Guid TenantId { get; set; }
      public DateTime CreatedAtUtc { get; set; }
      public DateTime? UpdatedAtUtc { get; set; }
  }
  ```
- The JWT `tenant_id` claim flows through `ITenantProvider` into `AppDbContext`. `AppDbContext` applies `HasQueryFilter(x => x.TenantId == _tenant.TenantId)` to every `TenantEntity`. The `SaveChangesAsync` override stamps `TenantId`, `CreatedAtUtc` and `UpdatedAtUtc`.
- `AppUser` and `Subscription` carry `TenantId` but have **no** global filter, because login and Stripe webhooks run before a tenant is known. Query them explicitly.
- Unique indexes **always** include `TenantId`.
- Never use `IgnoreQueryFilters()` outside an audited admin path.
- Raw SQL must filter by `TenantId` explicitly.
- Background jobs take `TenantId` as a parameter, set the tenant context, and throw if it isn't set.
- Cache keys include the tenant.
- Validate foreign keys through filtered queries, so a tenant can't reference another tenant's rows.
- Optional later: PostgreSQL row-level security as a second layer.

### Data types and Postgres specifics
- Use `decimal` for money and quantities, never `double`. Configure precision explicitly (e.g. `HasPrecision(18, 4)`).
- Store all timestamps as UTC (`timestamptz`); Npgsql requires `DateTimeKind.Utc`.
- Concurrency token: map Postgres `xmin` (e.g. a `uint Version` property marked as a row version). On `DbUpdateConcurrencyException`, return **409**.
- Unique-constraint violations (`PostgresException` with `SqlState == "23505"`) map to **409**.
- Use `EF.Functions.ILike` for case-insensitive search.

### Inventory
- On-hand stock is **derived**: `SUM(InventoryTransaction.Quantity)` per item. There is no mutable `Quantity` field.
- `InventoryTransaction` is append-only. Corrections are new, reversing rows; never update or delete.
- Allocated and available quantities are derived, not stored.

### Deletes
- Soft delete with `IsActive = false` (archive). Block archiving items that are on open POs or work orders.
- FK delete behaviour is `Restrict` almost everywhere. Cascade only when the child means nothing without its parent (PO lines, work-order components, planned orders, `ItemSupplier`).

## Domain model

| Child | Parent | FK | Optional | On delete |
|---|---|---|---|---|
| Item | Supplier | `PreferredSupplierId` | yes | Restrict |
| ItemSupplier | Item / Supplier | `ItemId` / `SupplierId` | no | Cascade |
| BomLine | Item | `ParentItemId`, `ComponentItemId` | no | Restrict |
| InventoryTransaction | Item | `ItemId` | no | Restrict |
| InventoryTransaction | PurchaseOrderLine / WorkOrder | `PurchaseOrderLineId` / `WorkOrderId` | yes | Restrict |
| DemandEntry | Item | `ItemId` | no | Restrict |
| PlanRun | AppUser | `CreatedByUserId` | no | Restrict |
| PlannedOrder | PlanRun | `PlanRunId` | no | Cascade |
| PlannedOrder | Item / Supplier | `ItemId` / `SuggestedSupplierId` | no / yes | Restrict |
| PurchaseOrder | Supplier | `SupplierId` | no | Restrict |
| PurchaseOrderLine | PurchaseOrder | `PurchaseOrderId` | no | Cascade |
| PurchaseOrderLine | Item | `ItemId` | no | Restrict |
| PurchaseOrderLine | PlannedOrder | `SourcePlannedOrderId` | yes | SetNull |
| WorkOrder | Item | `ItemId` | no | Restrict |
| WorkOrder | PlannedOrder | `SourcePlannedOrderId` | yes | SetNull |
| WorkOrderComponent | WorkOrder | `WorkOrderId` | no | Cascade |
| WorkOrderComponent | Item | `ComponentItemId` | no | Restrict |
| AppUser | Tenant | `TenantId` | no | Restrict |
| Subscription | Tenant | `TenantId` (1:1) | no | Cascade |

Key constraints:
- **Item:** unique (`TenantId`, `Sku`); SKUs are normalised (trimmed, upper-case). It has planning fields: `Type` (RawMaterial/SubAssembly/FinishedGood/Purchased), `UnitOfMeasure`, `StandardCost`, `SellingPrice?`, `LeadTimeDays`, `SafetyStock`, `MinOrderQty`, `LotSize` (0/1 = lot-for-lot), `PreferredSupplierId?`, `IsActive`.
- **BomLine:** unique (`TenantId`, `ParentItemId`, `ComponentItemId`); check constraint `ParentItemId <> ComponentItemId`; index on `ComponentItemId` for where-used. Its two FKs to `Item` need explicit EF configuration. The service runs **cycle detection on every write**: walk up from the proposed parent and reject if the component is one of its ancestors. No `BomHeader` in the MVP.
- **PO and work-order numbers:** unique per tenant, generated by atomic increment on a per-tenant sequence table.
- **WorkOrderComponent:** a BOM snapshot taken at release, so later BOM edits don't change in-flight orders.

## MRP engine

- Lives in its **own project**: a pure, framework-free class library. DTOs in, planned orders and exceptions out. No EF and no ASP.NET references.
- Deterministic and re-runnable. It runs as a background job with the tenant passed explicitly. Results are stored as a `PlanRun` with its `PlannedOrder` rows, so runs can be compared.
- **Inputs:**
  - Gross requirements: unfulfilled `DemandEntry` + open `WorkOrderComponent` (required − issued)
  - On hand: ledger sum
  - Scheduled receipts: open PO lines (ordered − received) + open work orders (ordered − completed)
  - Structure: `BomLine`
  - Parameters: lead time, safety stock, lot size, MOQ (+ `ItemSupplier`)
- **Exceptions to emit:** expedite, past due, no supplier, negative availability.
- Unit test it heavily against **hand-calculated** examples (e.g. a two-level BOM with lead times and on-hand stock) before building purchasing or work orders on top.

## API conventions

- RESTful routes: `GET /api/items`, `GET /api/items/{id}`, `POST /api/items`, `PUT /api/items/{id}`, `DELETE /api/items/{id}` (archives).
- `[Authorize]` on every controller by default. Opt out explicitly with `[AllowAnonymous]` (login, Stripe webhooks).
- Lists are always paged and searchable (`ItemQuery { Search, Page = 1, PageSize = 50 }`, max 200) and return `PagedResult<T>(Items, Total, Page, PageSize)`. Never load a whole table.
- Use `AsNoTracking()` plus projection for reads. Use `FirstOrDefaultAsync`, not `FindAsync`, so query filters apply.
- Update DTOs (`UpdateItemRequestDto`) have **no `Id`**; the route id is the only source of truth.
- Services return a small result type or throw domain exceptions, not `bool`. One global `IExceptionHandler` maps them to `ProblemDetails`:
  - not found → 404
  - validation → 400 or 422
  - concurrency or duplicate → 409
- Pass and honour a `CancellationToken` on every async endpoint and service method.
- Mapping: prefer hand-written mapping or Mapperly. If AutoMapper is still in use, check its current licence before relying on it.

### CSV import pattern
- `ImportItemRowDto` has no data annotations and loosely typed fields, so `[ApiController]` doesn't short-circuit with its own 400. All validation happens in the service.
- The service normalises SKUs, detects duplicates within the file, checks existing SKUs with one `WHERE IN` query, then saves **all or nothing** in a single `SaveChanges`.
- It returns `ImportResult(int Created, List<ImportRowError> Errors)`. Row numbers assume row 1 is the header (`i + 2`).
- `?dryRun=true` validates without saving. The UI flow is: dry run → show errors → commit.
- Responses: 200 on success, 422 with the errors, 400 for empty or oversized input (limit 5,000 rows).
- Later: accept `IFormFile` CSV, parse with CsvHelper, and merge parse errors into the same `Errors` list.
- The unique index stays as the backstop for race conditions.

## Testing

- One integration-test project using `WebApplicationFactory` + **Testcontainers PostgreSQL**. Never use EF InMemory or SQLite, because behaviour differs from production.
- Required tests (keep them green):
  1. Tenant A cannot read, update or delete tenant B's data.
  2. A duplicate SKU returns 409; the same SKU in two different tenants is allowed.
  3. An import with one bad row creates nothing and reports the correct row number.
- The MRP engine has its own pure unit-test project.
- Any new tenant-owned entity gets a cross-tenant isolation test.

## Build order (current plan)

1. **Foundation:** auth, tenancy (`TenantEntity`, query filter, stamping), reshaping `Product` into `Item` with planning fields and decimals, and a migration.
2. **Items cleanup:** RESTful routes, paging and search, update DTO, `xmin` concurrency, soft delete, exception handler, OpenAPI → generated TypeScript client.
3. `InventoryTransaction` ledger, adjustment endpoint and on-hand query; remove `Quantity`.
4. **BOMs:** line CRUD, cycle detection, indented explosion view, where-used.
5. Demand entries.
6. MRP engine (pure library + tests) → `PlanRun` / `PlannedOrder` as a background job.
7. POs and receiving, then work orders created from planned orders.
8. CSV import UI, dashboard (shortages, late orders, below safety stock), Stripe billing.

**Wedge milestone:** after steps 1–4 plus import and a basic shortage/buy-list report, the product is sellable to pilot customers. Prioritise reaching that over polishing later steps.

## Out of scope for the MVP (don't build unless asked)

Capacity/finite scheduling, routings, MPS, barcode/shop floor, lot/serial tracking, multi-warehouse, multi-currency, costing/accounting, quality, ECO/revisions, integrations, custom roles, report builder.

## Legacy code to watch for

The original `Product` / `Supplier` code predates these rules. When you touch it, migrate it toward them: `double` → `decimal`, a mutable `Quantity`, hard deletes, non-RESTful routes (`Add`, `AddList`, `Update/{id}`, `Delete/{id}`), `bool` service returns, unpaged `GetProductsAsync`, and no `[Authorize]`.

## Working style

- Make small, focused changes; one concern per commit.
- When you change the API, regenerate the TypeScript client and fix the front end in the same change.
- Ask before adding a new dependency, changing tenancy or auth code, or adding anything from the out-of-scope list.
- When unsure between a simple and a "complete" solution, choose simple and leave a `TODO` noting the follow-up.

## Open decisions

- Target vertical (leaning toward electronics/hardware makers).
- Auth: ASP.NET Identity vs a hosted provider.
- Mapping: Mapperly vs hand-written (moving off AutoMapper).
