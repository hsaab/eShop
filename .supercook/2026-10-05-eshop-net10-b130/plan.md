# Plan: eShop net8.0 to net10.0 dry run

## Approach

Ship one slice and one unmerged pull request on `publix-dryrun-net10`, based on `release/8.0` at `f236952`. The data shape is the version pins: `global.json` SDK `10.0.100` with `rollForward` left at `latestFeature`, every `net8.0` target string moved to `net10.0`, and the `Directory.Packages.props` values the design already fixed. Logic follows that shape: restore and build `eShop.Web.slnf`, then change only the call sites the compiler names in `src/eShop.AppHost/Program.cs`, `tests/Catalog.FunctionalTests/CatalogApiFixture.cs`, `tests/Ordering.FunctionalTests/OrderingApiFixture.cs`, and `src/eShop.ServiceDefaults` when the build names it. The 10.0 SDK install is a later phase. This slice assumes that SDK is present when verify runs.

## Slices

### 1. Retarget the web solution to net10.0

- scope:
  - `global.json`
  - `Directory.Packages.props`
  - every `*.csproj` that contains `net8.0` (25 files: the 22 in `eShop.Web.slnf`, plus `src/ClientApp/ClientApp.csproj`, `src/HybridApp/HybridApp.csproj`, and `tests/ClientApp.UnitTests/ClientApp.UnitTests.csproj`), including `src/eShop.AppHost/eShop.AppHost.csproj`
  - after restore and build, only files the compiler names. The expected set is `src/eShop.AppHost/Program.cs`, `tests/Catalog.FunctionalTests/CatalogApiFixture.cs`, `tests/Ordering.FunctionalTests/OrderingApiFixture.cs`, and sources under `src/eShop.ServiceDefaults`
  - the pull request body
  - the three `IsAspireHost` projects only if restore says they need an `Aspire.AppHost.Sdk` import
- change:
  - Data shape first. In `global.json`, set `sdk.version` from `8.0.400` to `10.0.100`. Leave `rollForward` at `latestFeature`. Amended from `10.0.302`: this agent cannot download the Microsoft SDK feed, and Ubuntu noble only publishes `10.0.112`. `latestFeature` accepts `10.0.112` and any newer 10.0 SDK, including the `10.0.302` pin on `main`. The reverse pin would not restore here.
  - In `Directory.Packages.props`, set `AspnetVersion` and `EfVersion` to `10.0.12`, `AspireVersion` to `13.6.0`, and `AspireUnstablePackagesVersion` to `13.6.0-preview.1.26479.8`. Leave `MicrosoftExtensionsVersion` at `8.7.0` unless restore names a consumer that cannot stay there.
  - Point `Microsoft.Extensions.ServiceDiscovery` and `Microsoft.Extensions.ServiceDiscovery.Yarp` (lines 26-27) at the literal `10.10.0` instead of `$(AspireVersion)`. Point `Microsoft.Extensions.Http.Resilience` (line 42) at the literal `10.10.0`. Set `Npgsql.EntityFrameworkCore.PostgreSQL` (line 46) to `10.0.3`. Set the inbox `Microsoft.Extensions.*` packages at lines 53-55 to `10.0.12`. Leave `Pgvector.EntityFrameworkCore` at `0.2.1` until the compiler names it, then set `0.3.0` in this same slice. Leave Grpc, Duende, OpenTelemetry, Swashbuckle, Polly, MediatR, and xUnit alone until restore or the compiler names that package.
  - In all 25 csproj files, change every `net8.0` target string to `net10.0`, including MAUI platform suffixes. In `ClientApp.csproj`, also update the windows append (line 5), the commented tizen target (line 7), the `OutputType` condition that compares the literal `net8.0` (line 16) so it compares `net10.0`, and the `Debug|net8.0-ios` condition (line 42). `HybridApp.csproj` has no bare `net8.0` compare. Only its multi-target suffixes change.
  - In `ClientApp.csproj`, `HybridApp.csproj`, and `ClientApp.UnitTests.csproj`, keep `ManagePackageVersionsCentrally` false and bump inline `Microsoft.Maui.Controls` and the other Maui package versions pinned at `8.0.70` or `8.0.80` to `10.0.110`. The central props file does not reach those three projects. This agent does not build or restore them. `eShop.Web.slnf` already excludes them.
  - Logic after the pins. Restore `eShop.Web.slnf`, then run `dotnet build eShop.Web.slnf`. Fix only the errors and the warnings that build prints. `Directory.Build.props` keeps `TreatWarningsAsErrors` true. Fix the emitting lines. Add no global `NoWarn`.
  - Aspire 13 call sites stay in this slice because a pin-only tree does not build, and they are written only after that build names them. Known calls to expect: `DistributedApplication.CreateBuilder` in `CatalogApiFixture` and `OrderingApiFixture`, and the top-level Aspire registration statements in `Program.cs`. If `src/eShop.ServiceDefaults` must change for a `10.0.12` signature, keep the same authentication scheme and the same authority. If the first build of the web solution is already clean, stop. Leave `Program.cs` and both fixtures untouched.
  - If those compiler errors require a new entry-point type, a generated host model, or a file named `AppHost.cs` before the AppHost project will build, stop and report that the surgical fix is not available. `src/eShop.AppHost/Program.cs` stays `Program.cs`. A port of `main`'s `AppHost.cs` is out of this dry run.
  - `Aspire.AppHost.Sdk` stays off the first edit. Add an `Aspire.AppHost.Sdk` 13.6.0 import only when `dotnet restore eShop.Web.slnf` says the three `IsAspireHost` projects no longer get hosting targets from that property. Add it on `eShop.AppHost` and the two functional test projects, in this same slice, and still keep `Program.cs`. If restore succeeds with the existing `IsAspireHost` property, the csproj shape stays as it is on `release/8.0`.
  - If a package the design left alone fails restore or compile, bump a patch in the same major when one restores. For Duende `7.0.6`, a major bump that changes token validation stops the slice. Put the exact error in DEMO NOTES.
  - If additions plus deletions, with each rewritten line counted as 2, cross 500 reviewable lines, keep this one slice and record `exception: splitting the pin change from the call-site fixes leaves a tree that does not build`. A framework-only tree still references `Aspire.Hosting.AppHost` 8.2.0, which has no `lib/net10.0`. A package-only tree is still `net8.0`. A pins-only tree that does not compile is not a shippable slice.
  - Open one pull request from `publix-dryrun-net10` to `release/8.0`. The base stays at `f236952`. The pull request is not merged. The body has a DEMO NOTES section: the pin table, the ServiceDiscovery, Yarp, and Http.Resilience split, the Aspire call-site before and after, whether Pgvector stayed at `0.2.1` or moved to `0.3.0`, the packages left on 8.x, that MAUI target strings were updated and those three projects were not built, that `README.md:38` still says to install the .NET 8 SDK, that migration `ProductVersion` 8 stays as history, the functional-test result below, and a short talk track of the Aspire break and the fix.
- journeys:
  - A developer changes the SDK pin and target frameworks and can restore `eShop.Web.slnf`.
  - The web solution builds, and unit tests in the filter pass without Docker.
  - Functional tests either pass with a container runtime or the result records that Docker was missing.
- verify:
  - After the later phase installs the 10.0 SDK, `dotnet --version` prints a `10.0` version. `rollForward: latestFeature` accepts `10.0.112` and anything newer in 10.0, including `10.0.302`. `git rev-parse release/8.0` is `f236952`.
  - `dotnet restore eShop.Web.slnf`
  - `dotnet build eShop.Web.slnf`
  - `dotnet test eShop.Web.slnf` when `docker info` succeeds.
  - Unit-test fallback when Docker is absent, or when the full filter starts Postgres and cannot finish: `dotnet test eShop.Web.slnf --filter "FullyQualifiedName!~FunctionalTests"`. If that filter still starts a container, run `dotnet test` on each test project in the filter whose csproj does not set `IsAspireHost`, including `tests/Ordering.UnitTests` and `tests/Basket.UnitTests`.
  - When `docker info` fails, DEMO NOTES record the sentence "Functional tests were not run because Docker is missing." That unit run is not a green full suite.
  - `rg -n "net8\\.0" -g "*.csproj" -g "global.json"` finds nothing.
  - `git diff release/8.0 -- README.md .github Directory.Build.props` is empty.
  - `src/eShop.AppHost/Program.cs` exists. `src/eShop.AppHost/AppHost.cs` does not.
- estimate: 340 reviewable lines. Rewritten lines count as 2: `global.json` 2, package props about 12 rewritten lines (24), target strings about 30 rewritten lines (60), inline Maui versions about 12 rewritten lines (24), compiler and warning fixes up to about 115 rewritten lines (230). Pgvector is another 2 only if the compile fails. An `Aspire.AppHost.Sdk` import, if restore asks, is a few lines inside this same total.

## Test-first

This upgrade has no new user-facing behavior to assert in a new test file before the TFM change. The journeys are restore, build, and the existing suite. The test designer writes an executable verification recipe from the verify commands above when a new failing test would not be meaningful. A test that only passes after the TFM edit by hard-coding a version string that the same commit changes is not the proof.

## Risks

- Aspire 13 renames hosting APIs and the call-site diff in `Program.cs` and the two fixtures grows past a short list of methods. Early warning: the first `eShop.AppHost` build reports missing types, or it asks for a new entry-point type, a generated host model, or `AppHost.cs`. Small renames stay in this slice. A required new entry point stops the dry run. The estimate of 340 assumes the compiler fix stays near 115 rewritten lines. That size is the open risk, because the build has not been run.
- `TreatWarningsAsErrors` turns new net10 analyzer warnings into build failures across many projects. Early warning: the same warning code appears in more than a handful of files, in projects that do not reference Aspire. Fix the emitting lines. Leave the property on.
- Restore may say `IsAspireHost` no longer brings Aspire hosting targets. Early warning: the first `dotnet restore eShop.Web.slnf` fails on the AppHost or either functional test project before any C# error. The same slice then adds `Aspire.AppHost.Sdk` 13.6.0 on those three projects. The first edit does not add that import on a guess.
- The two functional fixtures start Postgres and need Docker, which may be absent. Early warning: `docker info` fails. The unit-test fallback is the proof, and DEMO NOTES state the skip.
- `Pgvector.EntityFrameworkCore` 0.2.1 does not compile against EF Core 10. Early warning: the catalog project build names that package. Set `0.3.0` in this slice.
- Npgsql 10.0.3 may not be source-compatible with 8.0.4. Early warning: compile errors in the Postgres DbContext projects after restore has already succeeded.
- A package the design left alone, most plausibly Duende `7.0.6`, fails restore or compile on the net10 shared framework. Early warning: NU1202, NU1102, or an error that names that package id. A same-major patch is in scope. A major that changes token validation stops the work.
- A ServiceDefaults edit made for a `10.0.12` signature also changes token checks. Early warning: the diff touches the authority, the audience, or the HTTPS metadata requirement.
- Maui `10.0.110` is not compiled on this agent. Early warning: `dotnet restore eShop.Web.slnf` tries to load `ClientApp` or `HybridApp`, which the filter excludes. A later Windows build can still fail for a reason this dry run never restored.
- The 10.0 SDK is not installed yet. Early warning: `dotnet` is missing or `dotnet --version` is not `10.0.x`. Verify waits on that later phase.

## Gaps

- The Aspire 13 method names are unknown until `dotnet build eShop.Web.slnf` runs. The slice budgets room for those edits and does not draft them first.
- Whether the existing `IsAspireHost` property still imports hosting targets on Aspire 13.6.0 is unknown until restore.
- Whether `Pgvector.EntityFrameworkCore` 0.2.1 compiles against EF Core 10 is unknown until the catalog project builds.
- Whether this machine has Docker, and whether the later phase has installed a 10.0 SDK, is unknown here.

## Out of scope

- A port of `main`'s `AppHost.cs`, the OpenAPI helper rewrite, and the switch to Microsoft.Testing.Platform.
- EF migration snapshots and their `ProductVersion` 8 history.
- `README.md`, workflows, and identity or auth behavior. `README.md:38` stays a talk-track note.
- Building the MAUI workload on this Linux agent.
- `e2e/**/*.spec.ts`.
- Merging the pull request. `release/8.0` stays at `f236952`.
- Turning off `TreatWarningsAsErrors` or adding a global `NoWarn`.
- Editing `eShop.Web.slnf` or `Directory.Build.props`.

## Provenance

Slice 1 is a merge of A, B, and C. All three already ship one slice and one unmerged pull request on `publix-dryrun-net10`. The pin table and the "leave these packages until restore names them" rule are the shared design. A's line-level csproj notes (windows append, tizen comment, `OutputType`, `Debug|net8.0-ios`) and A's rule to add `Aspire.AppHost.Sdk` only after restore asks are in the change. B's supporting checks (`dotnet --version`, `git rev-parse`, `rg` for leftover `net8.0`) are in verify. C's rewritten-line arithmetic is the 340 estimate, C's per-project unit fallback is the Docker fallback, and C's stop condition (a required `AppHost.cs` ends the dry run) is the guard on call-site size. C's DEMO NOTES list is the pull request body.

The strongest idea not taken is authoring the Aspire 13 call-site diff, and adding an `Aspire.AppHost.Sdk` 13.6.0 import, in the first edit before restore and build have named the errors. B keeps those files in the slice before the build has been run. A is unsure whether the SDK import belongs in that first edit. Writing either from a guess is how a port of `main`'s `AppHost.cs` would enter this dry run. The files stay in this slice as the place compiler-reported edits land, after the build, because splitting them off leaves a tree that does not build.
