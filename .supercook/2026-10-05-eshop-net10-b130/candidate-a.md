# Candidate A: eShop net8.0 to net10.0 dry run

## Approach

Ship the dry run as one slice and one PR on `publix-dryrun-net10` (base `release/8.0` at `f236952`). Do not merge. The SDK pin, every `net8.0` framework string, the package pins the design already chose, and the compiler errors those pins produce have to land together, because no prefix of that set restores `eShop.Web.slnf`.

The 10.0 SDK install is a later phase. This slice assumes that SDK is present when verify runs. `rollForward` stays `latestFeature`. `TreatWarningsAsErrors` stays true. `Program.cs` stays `Program.cs`.

## Why this over the obvious alternative

The obvious alternative is three slices: framework strings, then `Directory.Packages.props`, then whatever the compiler rejects. A framework-only tree still references `Aspire.Hosting.AppHost` 8.2.0, which has no `lib/net10.0`, so restore of the AppHost fails. A package-only tree is still `net8.0` under SDK `10.0.302`. A pins-only tree that restores and does not compile is not a shippable slice. The mechanical edits are about 120 reviewable lines and the expected call-site fixes fit in the rest of a 200-line slice, under the 500 cap, so splitting would only add broken intermediate trees.

## Slices

### 1. Retarget the web solution to net10.0

- scope:
  - `global.json`
  - `Directory.Packages.props`
  - `src/ClientApp/ClientApp.csproj`
  - `src/HybridApp/HybridApp.csproj`
  - `tests/ClientApp.UnitTests/ClientApp.UnitTests.csproj`
  - `src/eShop.AppHost/eShop.AppHost.csproj`
  - `src/eShop.AppHost/Program.cs`
  - `tests/Catalog.FunctionalTests/CatalogApiFixture.cs`
  - `tests/Ordering.FunctionalTests/OrderingApiFixture.cs`
  - every other `*.csproj` that contains `net8.0` (25 csproj files in total)
  - ServiceDefaults sources, and any other file, only when `dotnet build eShop.Web.slnf` names that file
- change:
  - In `global.json`, set `sdk.version` to `10.0.302`. Leave `rollForward` at `latestFeature`.
  - In `Directory.Packages.props`, set `AspnetVersion` and `EfVersion` to `10.0.12`, `AspireVersion` to `13.6.0`, and `AspireUnstablePackagesVersion` to `13.6.0-preview.1.26479.8`. Leave `MicrosoftExtensionsVersion` at `8.7.0` unless restore names a consumer that cannot stay there.
  - Point `Microsoft.Extensions.ServiceDiscovery` and `Microsoft.Extensions.ServiceDiscovery.Yarp` (lines 26-27) at `10.10.0` instead of `$(AspireVersion)`. Point `Microsoft.Extensions.Http.Resilience` (line 42) at `10.10.0`. Set `Npgsql.EntityFrameworkCore.PostgreSQL` (line 46) to `10.0.3`. Set the inbox `Microsoft.Extensions.*` packages at lines 53-55 to `10.0.12`. Leave `Pgvector.EntityFrameworkCore` at `0.2.1` until the compiler names it, then set `0.3.0` in this same slice. Leave Grpc, Duende, OpenTelemetry, Swashbuckle, Polly, MediatR, and xUnit alone until restore or the compiler names that package.
  - In all 25 csproj files, change every `net8.0` target string to `net10.0`, including MAUI platform suffixes. In `ClientApp.csproj`, also update the `OutputType` condition that compares the literal `net8.0` (line 16), the `Debug|net8.0-ios` condition (line 42), the windows append (line 5), and the commented tizen target (line 7). `HybridApp.csproj` has no bare `net8.0` compare. Only its multi-target suffixes change.
  - In `ClientApp.csproj`, `HybridApp.csproj`, and `ClientApp.UnitTests.csproj`, bump inline `Microsoft.Maui.Controls` and the other Maui package versions that are pinned at `8.0.70` or `8.0.80` to `10.0.110`. Those three projects set `ManagePackageVersionsCentrally` to false, so the central props file does not reach them. Do not build or restore those three projects.
  - Restore and build `eShop.Web.slnf`. Fix only the errors and the warnings that build prints. Expected sites are `Program.cs`, the two functional fixtures, and ServiceDefaults. Keep the same auth scheme and authority if a ServiceDefaults call site must change. If restore says the three `IsAspireHost` projects need `Aspire.AppHost.Sdk` 13.6.0, add that import there and keep `Program.cs`. Do not copy `main`'s `AppHost.cs`, do not edit EF migration snapshots, do not edit `README.md` or workflows, and do not set `TreatWarningsAsErrors` to false or add a `NoWarn`.
  - Open one PR from `publix-dryrun-net10` to `release/8.0`. Do not merge. The body has a DEMO NOTES section: the pin table, the Aspire call-site before and after, that MAUI was not built, that `README.md:38` still says install .NET 8 on purpose, whether Pgvector stayed at `0.2.1` or moved to `0.3.0`, and the functional-test result below.
- journeys:
  - A developer on `release/8.0` who takes this branch has SDK pin `10.0.302` and `net10.0` targets, and can restore the web solution.
  - The web solution builds, and unit tests in the `eShop.Web.slnf` filter pass without Docker.
  - Functional tests pass when a container runtime is available. When Docker is missing, the PR says they were not run because Docker is missing, and it does not call the unit run a green full suite.
- verify:
  - `dotnet restore eShop.Web.slnf`
  - `dotnet build eShop.Web.slnf`
  - `dotnet test eShop.Web.slnf --filter "FullyQualifiedName!~FunctionalTests"`
  - `docker info`: if it succeeds, `dotnet test eShop.Web.slnf --filter "FullyQualifiedName~FunctionalTests"`. If it fails, record the sentence "Functional tests were not run because Docker is missing" in the PR. Do not run `e2e/**/*.spec.ts`.
- estimate: 200 reviewable lines

## Risks

- Aspire 13 renames hosting APIs and the call-site diff grows past a few methods. Early warning: the first `eShop.AppHost` build reports missing types, not a short list of renamed methods. Keep the single slice anyway and mark `exception: a pin-only tree does not restore` if the running total crosses 500.
- `TreatWarningsAsErrors` turns new net10 analyzer warnings into build failures across many projects. Early warning: the same warning code appears in more than a handful of files. Fix the emitting lines. Do not turn the property off.
- The two functional fixtures start Postgres and need Docker, which may be absent. Early warning: `docker info` fails. The unit filter is the proof, and the PR states the skip.
- Maui `10.0.110` is not compiled on this agent. Early warning: a later Windows build of `ClientApp` or `HybridApp` fails on a package this dry run never restored.
- A package the design left alone, most plausibly Duende `7.0.6`, fails restore or compile on the net10 shared framework. Early warning: the error names that package id. Bump a patch in the same major if one restores. Do not take a major bump that changes token validation. If only a major works, stop and put the exact error in DEMO NOTES.
- `Pgvector.EntityFrameworkCore` 0.2.1 does not compile against EF Core 10. Early warning: the catalog project build names that package. Set `0.3.0` in this slice.

## Least sure about

Waiting to add an `Aspire.AppHost.Sdk` 13.6.0 import until restore asks for it, instead of putting that SDK on `eShop.AppHost` and the two functional test projects in the first edit. I would change my mind if the first `dotnet restore eShop.Web.slnf` fails with an error that `IsAspireHost` no longer brings the hosting targets. Then this same slice adds the SDK import to those three projects and still does not rename `Program.cs`. I would also change my mind if restore succeeds with the existing `IsAspireHost` property, which means the csproj shape stays as it is on `release/8.0`.
