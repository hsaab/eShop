---
name: eshop-orchestrator
description: Orchestrates feature work, bug fixes, and investigations in the eShop .NET 10 / Aspire repo. Classifies the task, maps it to the right service and test project, follows the matching track, and enforces build, test, and run checks before reporting done. Use when asked to build a feature, fix a bug, investigate behavior, or make a code change in eShop.
disable-model-invocation: true
---

# eShop orchestrator

## 1. Classify the task

| Track | Signals | Edits code? |
|---|---|---|
| Feature | "add", "support", new endpoint, page, or field | Yes |
| Bug | "broken", "fails", error message, stack trace | Yes |
| Investigation | "why", "how does", "find out", "trace" | No. Report only |

If the track is unclear, ask once. Investigations never edit files; they end with findings and a proposed fix for the user to approve.

## 2. Locate the code

| Area | Projects | Shape | Tests |
|---|---|---|---|
| Catalog | `src/Catalog.API` | Minimal APIs in `Apis/CatalogApi.cs`, EF Core + pgvector | `tests/Catalog.FunctionalTests` |
| Basket | `src/Basket.API` | gRPC service from `Proto/basket.proto`, Redis | `tests/Basket.UnitTests` |
| Ordering | `src/Ordering.API`, `Ordering.Domain`, `Ordering.Infrastructure` | CQRS with MediatR (`Application/Commands`, `Queries`, `Validations`); DDD aggregates in `Ordering.Domain/AggregatesModel` | `tests/Ordering.UnitTests`, `tests/Ordering.FunctionalTests` |
| Storefront | `src/WebApp` (Blazor, `Components/`), `src/WebAppComponents` (shared Razor) | Calls Basket over gRPC, Catalog and Ordering over HTTP (`WebApp/Extensions`) | `e2e/` (Playwright) |
| Identity | `src/Identity.API` | Duende IdentityServer, MVC views | none |
| Messaging | `src/EventBus`, `EventBusRabbitMQ`, `IntegrationEventLogEF`, each service's `IntegrationEvents/` | RabbitMQ integration events | via functional tests |
| Workers | `src/OrderProcessor`, `src/PaymentProcessor` | Background services on the event bus | none |
| Webhooks | `src/Webhooks.API`, `src/WebhookClient` | | none |
| Wiring | `src/eShop.AppHost/Program.cs`, `src/eShop.ServiceDefaults` | Aspire resources, references, env vars | |

- One service: read the code directly.
- Several services: launch parallel `explore` subagents, one per service, asking for file pointers and a short how-it-works note, not file dumps.
- Before writing new code, open the nearest existing example (sibling endpoint, command handler, component) and match its shape.

## 3. Follow the track

### Feature
1. State the smallest slice that delivers the request and the files it touches.
2. Implement it, following the neighboring pattern.
3. Add tests where the behavior lives:
   - New or changed API endpoint: add or update tests in that service's FunctionalTests project.
   - Domain or handler logic: unit tests.
   - UI: check it in the running app; add Playwright coverage only if `e2e/` already covers that flow.
4. Verify (section 4).

### Bug
1. Reproduce. Write a failing test first when a test project can reach the code; otherwise reproduce in the running app using the Aspire dashboard logs and traces.
2. State the root cause in one sentence before editing. Fix the cause, not the symptom.
3. Fix it, and confirm the failing test now passes.
4. Verify (section 4).

### Investigation
1. Read code and run read-only commands: tests, `curl`, app logs. No edits.
2. For runtime behavior, use the Aspire dashboard at `https://localhost:19888`: Structured logs, Traces, and Console logs per resource.
3. Report the answer first, then the evidence with `file:line` references, then a proposed next step.

## 4. Verify before reporting done

```sh
dotnet build eShop.Web.slnf                          # ~12s incremental; warnings fail the build
dotnet test tests/<Project>/<Project>.csproj --no-build
dotnet test tests/<Project>/<Project>.csproj --no-build --filter "FullyQualifiedName~<TypeOrMethod>"
dotnet test eShop.Web.slnf --no-build                # full suite
```

- Unit tests take about 1s per project. Functional tests take about 7s per project and start Postgres through Aspire, so Docker must be running.
- Runtime check: `npm start` serves the shop at `https://localhost:7298` and the dashboard at `https://localhost:19888`. After changing C# or Razor, press Ctrl+C and run `npm start` again; files in `src/WebApp/wwwroot` are served live.
- End-to-end (optional, slow): set `USERNAME1` and `PASSWORD` to the seeded user in `.github/workflows/playwright.yml`, then run `npx playwright test`. It reuses an app already listening on `http://localhost:5045`.
- Never report done with a failing build or test. If a failure was already there before your change, prove it (for example with `git stash` and a rerun) and say so.

## 5. Repo rules that bite

- `TreatWarningsAsErrors` is on for every project (`Directory.Build.props`). Fix the warning; add `NoWarn` only with a stated reason.
- Central package management: versions live in `Directory.Packages.props`, and `<PackageReference>` entries in `.csproj` files have no `Version` (the MAUI `ClientApp` is the exception).
- Build `eShop.Web.slnf`, not `eShop.sln`. The full solution includes the MAUI `ClientApp` and `HybridApp`, which need MAUI workloads.
- The SDK is pinned in `global.json`. `release/8.0` pins .NET 8 and fails with "A compatible .NET SDK was not found" when only .NET 10 SDKs are installed. Check the branch before blaming code.
- Aspire launches every service with `dotnet run --no-build`. A plain `dotnet build` does not change the running app; restart the AppHost.
- Wire new services, databases, or dependencies in `eShop.AppHost/Program.cs` (`AddProject`, `WithReference`, `WithEnvironment`), not in hard-coded connection strings.
- Backend services do not call each other directly. New cross-service behavior goes through integration events on the event bus.

## 6. Report

```markdown
**Outcome:** one sentence on what changed or what was found.
**Changed:** files and why (features and bugs), or **Findings:** with file:line evidence (investigations).
**Verified:** commands run and their results.
**Concern:** one real risk or follow-up, if there is one.
```

Commit only when the user asks, using Conventional Commits.
