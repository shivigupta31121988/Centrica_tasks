# Renewable Asset System - Tasks 1, 2 & 3

A system for managing master data of renewable energy assets (Task 1), importing their meter data (Task 2), and calculating settlement amounts (Task 3) - built to be easy to extend (new asset types, new file formats) without touching existing code.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned via `global.json` to `10.0.201`; `rollForward: latestFeature` accepts a compatible newer 10.0.2xx patch too)
- [Node.js 20+](https://nodejs.org/) with npm 11.x (tested against npm `11.19.0` - run `npm install -g npm@11.19.0` if your global npm is older)
- A [MongoDB Atlas](https://www.mongodb.com/cloud/atlas) free-tier (M0) cluster - no local Mongo/Docker install needed. See "MongoDB setup" below if you haven't created one yet.

**Note on package versions**: the framework-coupled NuGet packages (`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.*`) are pinned to `10.0.0` in the `.csproj` files. This was built without live access to the NuGet registry to confirm the exact latest `10.0.2xx`-aligned patch versions available at the time you're reading this - if `dotnet restore` complains a pinned version isn't found, bump it to whatever `10.0.x` patch is actually published (`dotnet list package --outdated` after a successful restore will show you what's current). The same caveat applies to `SharpCompress`/`Snappier`/`System.IO.Packaging` in `Assets.Infrastructure.csproj`, pinned there to work around known-vulnerability warnings (NU1902/NU1903) in versions ClosedXML pulls in transitively - run `dotnet list package --vulnerable --include-transitive` after restore to confirm these actually clear the advisories, since that couldn't be verified live either.

## MongoDB setup (Atlas)

1. Create a free account at mongodb.com/cloud/atlas.
2. Create a free (M0) cluster.
3. Under **Database Access**, create a database user (username/password).
4. Under **Network Access**, allow your IP (or `0.0.0.0/0` for local dev only).
5. Click **Connect → Drivers → .NET** and copy the connection string.

## Backend setup

```bash
cd src/Assets.Api
cp appsettings.Development.json.example appsettings.Development.json
```

Edit `appsettings.Development.json` and fill in:
- `Mongo:ConnectionString` - your Atlas connection string
- `Jwt:SigningKey` - any long random string for local dev

Then, from the repo root:

```bash
dotnet restore RenewableAssetSystem.sln
dotnet run --project src/Assets.Api
```

The API starts on `https://localhost:5001` by default, with Swagger UI at `/swagger`.

### Testing the API directly via Swagger

Swagger is enabled by default (not just in Development - see `Swagger:Enabled` in `appsettings.json`, and the comment in `Program.cs` on why you'd restrict this in a real production deployment). Open **`https://localhost:5001/swagger`** in a browser:

1. Try `POST /api/auth/login` with a seeded user's credentials (see "Seeding the first users" below) - copy the `token` from the response.
2. Click the **Authorize** button at the top of the page, paste the token (no need to type "Bearer", just the token itself), and confirm.
3. Every other endpoint now runs with that user's identity - including role-restricted ones like `POST /api/assets` (Admin-only).
4. `POST /api/meter-data/import` renders as a file-upload form directly in Swagger, so you can test the import flow without the React UI at all.

### Seeding the first users

There is **no registration endpoint** anywhere in the system - access is granted purely through database changes. Provision the first Admin and Trader users with the seed script:

```bash
npm install --no-save mongodb bcryptjs   # one-time, from repo root
```

Edit `scripts/seed-users.js` to set real usernames/passwords, then:

```bash
MONGO_CONNECTION_STRING="<your-atlas-connection-string>" node scripts/seed-users.js
```

Discard the plaintext passwords from the file afterwards - don't commit them.

## Frontend setup

```bash
cd assets-ui
cp .env.example .env
npm install
npm start
```

Edit `.env` if your API isn't running on the default `https://localhost:5001`. The UI starts on `http://localhost:3000`.

Sign in with a user created via the seed script. Admins can create assets, import meter data, and see everything Traders see. Traders can only view the asset list.

## Task 2: Meter data import

There are two independent ways to import meter data - both run the exact same pipeline underneath (`MeterDataImportService`), so results are identical either way.

### Option A: directory import (console app)

Drop meter data files (named `<meterPointId>.csv` or `<meterPointId>.xlsx`, e.g. `570715000000088747.csv`) into the `/meterdata` directory, then run:

```bash
cd src/Assets.Importer
cp appsettings.json appsettings.local.json   # or just edit appsettings.json directly
```

Fill in `Mongo:ConnectionString` (same Atlas connection string as the API), then from the repo root:

```bash
dotnet run --project src/Assets.Importer
```

This scans `/meterdata`, imports every valid file, auto-creates a placeholder `Unclassified` asset for any meter point id it doesn't recognise (visible in the UI - an Admin can later re-classify it as a real WindTurbine/SolarPanel by creating the correct asset), and prints a summary with total elapsed time.

### Option B: upload via the UI (CSV or Excel, async with live progress)

As an Admin, use the **"Import meter data"** card on the main page:

1. Choose a `.csv` or `.xlsx` file named `<meterPointId>.csv`/`.xlsx`.
2. Click **Upload**. The file transfers with a real progress bar (tracked via `XMLHttpRequest` upload events).
3. The server queues it as a background job and responds immediately - the UI then polls for status every second, with a continuously ticking "elapsed" stopwatch covering the whole operation (upload + server-side parsing + persisting).
4. Once the job completes, you'll see "Imported in X.Xs - N rows imported, M skipped" (or an error if the file couldn't be processed).

This is deliberately async (upload returns instantly, processing happens in a background worker) so a large file doesn't tie up the HTTP request - see the Task 2 plan doc for the full design rationale, including the documented trade-off of the current in-memory job queue (not durable across an app restart - flagged as a production upgrade path, not built now).

**Note on file structure**: both parsers expect a header row with `Timestamp` and `Production` columns - this was an assumption (documented in the Task 2 plan doc) since the real sample file's structure wasn't available when this was built. Worth verifying against the actual file before this goes anywhere beyond local testing.

## Task 3: Settlement

Settlement amount = Production × Spot Price. Since the real Spot Price API isn't available yet, a deterministic `FakeSpotPriceProvider` stands in for it (same hour always returns the same price, so results are reproducible) - a real `HttpSpotPriceProvider` matching the brief's documented API contract is already written (`Assets.Infrastructure/Settlement/HttpSpotPriceProvider.cs`) but not wired in; swapping it in later is a one-line DI change in `Program.cs`.

Decisions locked in for this implementation:
- **Currency**: fixed DKK.
- **Day/month boundaries**: local time (Europe/Copenhagen), with DST handled via `TimeZoneInfo` - not UTC calendar days.
- **Rounding**: 2 decimal places, banker's rounding (round-half-to-even).
- **Missing data**: an hour with no meter reading or no spot price is excluded from the amount and counted in that bucket's `incompleteHours`, rather than silently guessed at.

### API routes

```
GET /api/settlements/assets/{assetId}?start=2024-01-01&end=2024-01-31
-> [ { "date": "2024-01-01", "amount": 123.45, "currency": "DKK", "incompleteHours": 0 }, ... ]

GET /api/settlements/total?start=2024-01-01&end=2024-01-31
-> [ { "month": "2024-01", "amount": 98765.43, "currency": "DKK", "incompleteHours": 0 }, ... ]
```

Both are read-only and available to both Admin and Trader. In the UI, the **"Settlement"** card lets you pick an asset and a date range and see both the per-asset daily breakdown and the all-assets monthly total side by side.

### Debug-only settlement logging

Every hourly calculation (`production × price = amount`, including the meter point id, the hour, and whether data was missing) is logged at **Debug** level via `SettlementCalculator`. Debug-level logs are suppressed by the default logging configuration (`appsettings.json`'s `Logging:LogLevel:Default` is `Information`), so this detailed formula-and-values trail is silent in a normal/production run. To see it locally, `appsettings.Development.json.example` includes the override that turns it on:

```json
"Logging": {
  "LogLevel": {
    "Assets.Infrastructure.Settlement.SettlementCalculator": "Debug"
  }
}
```

A one-line summary (asset, date range, day count, total amount) is always logged at Information level regardless of environment - only the detailed per-hour trail is dev-only.

## Running tests

**Backend** (from repo root):
```bash
dotnet test RenewableAssetSystem.sln
```
This includes real (ephemeral, in-process) MongoDB integration tests via Mongo2Go for the repository layer - no external database needed to run them.

**Frontend** (from `assets-ui/`):
```bash
npm test              # watch mode
npm run test:ci       # single run with coverage, used in CI
```

**Fuzzy-match regression report** (from `assets-ui/`):
```bash
npm run test:fuzzy-report
```
Prints a match-accuracy percentage against a table of realistic typo cases (e.g. "sloar" → SolarPanel). Independent of the Jest pass/fail gate - useful as the typo dataset grows over time.

## Architecture overview

```
/src
  Assets.Domain          - Asset/WindTurbine/SolarPanel/UnclassifiedAsset, AssetTypeRegistry, MeterReading/ImportJob, IMeterDataParser, Settlement types + ISpotPriceProvider
  Assets.Infrastructure  - MongoDB persistence, password hashing, Csv/ExcelMeterDataParser, MeterDataImportService, job queue, SettlementCalculator, Fake/HttpSpotPriceProvider
  Assets.Api             - Controllers, DTOs, JWT auth, reflection-based AssetMapper, ImportJobWorker (background processor)
  Assets.Importer        - console app: directory-scan meter data import
/tests                   - xUnit tests for all layers, including real (Mongo2Go) integration tests
/assets-ui                - React (TypeScript) SPA: login, asset list + schema-driven create form, Fuse.js fuzzy search, meter data upload with live progress, settlement view
/scripts                 - seed-users.js (user provisioning)
/meterdata               - meter data files live here (both import paths read/write this directory)
buildspec.yml            - AWS CodeBuild pipeline definition
sonar-project.properties - SonarQube scan configuration
```

### Adding a new asset type later

1. Add the domain class in `Assets.Domain/Assets` (inherit `Asset`, add `[AssetField]` properties).
2. Register it in `Assets.Infrastructure/Persistence/MongoContext.cs` (one `BsonClassMap` block) and in `Assets.Domain/Assets/AssetTypeRegistry.cs` (one dictionary entry).
3. Done. The API's `/api/asset-types` endpoint and the React create form pick it up automatically - no frontend code changes required.

### Adding a new meter data format later

1. Implement `IMeterDataParser` (Domain interface) in `Assets.Infrastructure/MeterData` - just `CanParse` and `ParseAsync`.
2. Register it with `builder.Services.AddScoped<IMeterDataParser, YourNewParser>()` in `Assets.Api/Program.cs` (and in `Assets.Importer/Program.cs` if the console app should support it too).
3. Done. `MeterDataImportService` picks the right parser automatically per file - the pipeline, asset auto-creation, and persistence logic never change.

## Security notes (see the full plan docs for detail)

- Passwords are bcrypt-hashed; never stored or logged in plaintext.
- JWT is used for auth; kept in React state (not localStorage) to reduce XSS token-theft exposure.
- All MongoDB queries use the typed `Builders<T>.Filter` API - no raw/string-built queries, preventing NoSQL injection.
- The meter-data importer only ever reads from/writes to its own fixed, configured directory, with strict filename validation (`MeterDataFileNaming`) - an uploaded file's raw filename is never used to build a filesystem path, which is what actually prevents path traversal via a crafted upload.
- Excel parsing reads cell values only - uploaded workbook formulas are never evaluated.
- CORS is restricted to the configured UI origin; CSP/`X-Frame-Options`/`X-Content-Type-Options` headers are set on every API response.
