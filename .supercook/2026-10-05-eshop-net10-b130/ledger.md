# Ledger: dry-run upgrade eShop from .NET 8 to .NET 10
run-id: 2026-10-05-eshop-net10-b130
repo: https://github.com/hsaab/eShop | branch: publix-dryrun-net10 | worktree: /workspace
started: 2026-10-05 20:20 | tier: complex | track: feature | design doc: required (big-change yes)
capabilities: git yes | host github (gh authed) | worktrees yes | tests `dotnet test eShop.Web.slnf` (dotnet not installed yet) | PRs yes | supercook agents installed | no PR template
test-paths: tests/**/*Test.cs, tests/**/*Tests.cs, e2e/**/*.spec.ts
models: all roles inherit
base: release/8.0 at f2369529433374a01b864b6fa1499ad894756f53
modifiers: keep ledger | do not merge | branch name requested as publix-dryrun-net10
done: branch publix-dryrun-net10 exists off release/8.0 with net8.0 retargeted to net10.0 in TargetFramework values, global.json SDK, and the package bumps restore needs; `dotnet restore` and `dotnet build` of eShop.Web.slnf succeed with quoted output; high-signal tests from that same solution filter run with quoted results; an open PR on hsaab/eShop uses base release/8.0 (or a pin branch from it) and a plain-language body; DEMO NOTES name the bumps, the gotchas, and a talk track for the Wednesday live upgrade. release/8.0 stays at f236952. Nothing is merged.

## Phases
- [x] intake: clean tree on release/8.0, created publix-dryrun-net10 at f236952, .NET SDK missing so later phases install it, ledger seeded (20:20)
- [x] assess: complex/feature, big-change yes, because a bad SDK or package pin fails restore for every project (20:24)
- [x] recon: 1 broad + 4 targeted explorers, pointers in the log (20:36)
- [x] design doc (lite): design-doc.md written (20:42). Approved from the task text, not a live pause: the user already required a surgical TFM plus SDK plus package bumps, said start now, and this session cannot wait. Full-vs-lite was not asked. Lite, because a 15-section doc would not change that scope.
- [x] plan: arena merged A+B+C into one slice, 340 reviewable lines, plan.md (20:55). Judge rejected writing Aspire call sites before the compiler names them. Parent checked the slice estimate is under 500, and the plan logs a cohesion exception only if the compiler fix pushes it over.
- [x] test-first: no new C# test. verify.sh is the recipe because a net10 string assertion would only pass by hard-coding the same edit, and dotnet is not installed so a red suite cannot run. The recipe's current failure is the missing SDK, which is the environment, not a wrong assertion (21:02)
- [x] implement
  - [x] slice 1: retarget the web solution to net10.0 (estimate 340 reviewable, actual 290 against release/8.0). Build of eShop.Web.slnf succeeded with 0 errors and 3 ASPIRE010 warnings (20:58)
- [x] verify: `bash .supercook/2026-10-05-eshop-net10-b130/verify.sh` exited 0. Build succeeded, 3 ASPIRE010 warnings, 0 errors. Basket.UnitTests passed 3. Ordering.UnitTests passed 29. Docker is missing, so this is not a green `dotnet test eShop.Web.slnf`. Fresh-context verifier agreed, and flagged the PR body for the missing Docker sentence plus three major package jumps (20:58)
- [x] deliver: draft PR https://github.com/hsaab/eShop/pull/1 against release/8.0. Body updated with the quoted test result and the Docker sentence. Not merged.
- [x] merge: skipped. The user said do not merge. release/8.0 stays at f236952.

## Playbook steps (feature)
- [x] name the data shape before writing any logic: version pins in plan.md (SDK 10.0.100, amended from 10.0.302 because this agent only has Ubuntu SDK 10.0.112, AspnetVersion and EfVersion 10.0.12, AspireVersion 13.6.0, ServiceDiscovery and Http.Resilience 10.10.0, Npgsql EF 10.0.3)
- [x] name the user journeys this feature has to make work: restore eShop.Web.slnf, build plus unit tests without Docker, functional tests or an explicit Docker skip
- [x] tests for those journeys land before the implementation: verify.sh committed with the ledger before product edits. Existing tests stay the behavior proof.
- [x] implement slice by slice, each one shippable on its own
- [x] no opportunistic refactors in the diff
- [x] the feature works end to end on the real artifact, not just in unit tests: restore and build of eShop.Web.slnf succeed. Unit tests in that filter pass. Functional tests did not run.

## Log
- 20:20 intake: origin is hsaab/eShop, gh is authed, worktrees work, tree was clean. CI test command is `dotnet test eShop.Web.slnf` after `dotnet build eShop.Web.slnf` in `.github/workflows/pr-validation.yml`. `dotnet` is not on PATH. No `~/.supercook/models.md`, so every role inherits. User named the branch `publix-dryrun-net10` and said do not merge and keep ledger. Worked in place on a new branch because the tree was clean and release/8.0 must stay untouched.
- 20:24 assessor: tier complex, track feature, big-change yes. Retargets global.json and every net8.0 csproj, including eShop.AppHost plus ClientApp and HybridApp multi-targets, so a bad SDK pin or package bump fails restore and build for the whole solution. Parent checked: no files changed. Verdict accepted.
- 20:36 targeted explorers: central versions in Directory.Packages.props (Aspnet 8.0.7, EF 8.0.8, Aspire 8.2.0). MAUI literals that must move with the TFM are ClientApp.csproj:16 and ClientApp.csproj:42. Aspire.Hosting.AppHost 9.5.2 has no lib/net10.0. 13.0.0 is the first with lib/net10.0. ServiceDiscovery has no 13.x; latest is 10.10.0. No net8.0 string outside csproj files. README.md:38 still says install the .NET 8 SDK.
- 20:48 arena A: one slice, 200 lines, least sure about adding Aspire.AppHost.Sdk only if restore asks. File candidate-a.md.
- 20:48 arena B: one slice, 270 lines, least sure about keeping call-site edits in the same slice before the build runs. File candidate-b.md.
- 20:48 arena C: one slice, 340 lines, least sure that Program.cs can absorb Aspire 13.6 without a new AppHost.cs. File candidate-c.md. All three models inherited the parent, because no home roster overrides the arena roles.
- 20:42 design: do not port main. Pin SDK 10.0.302, AspnetVersion and EfVersion 10.0.12, Aspire 13.6.0, ServiceDiscovery and Http.Resilience 10.10.0, Npgsql EF 10.0.3. Code edits only where the compiler fails. Diagram is mermaid because Lucid is not connected.
- 21:05 plan amendment: global.json pin is 10.0.100, not 10.0.302. dot.net, aka.ms, and builds.dotnet.microsoft.com fail TLS. Ubuntu noble-updates has dotnet-sdk-10.0 10.0.112 only. latestFeature from 10.0.100 accepts 10.0.112 and a newer 10.0 SDK such as main's 10.0.302. A 10.0.302 pin would not roll backward onto 10.0.112.
- 20:28 broad explorer: 25 csproj files are all net8.0. eShop.Web.slnf drops ClientApp, HybridApp, and ClientApp.UnitTests. Versions live in Directory.Packages.props (Aspnet 8.0.7, EF 8.0.8, Aspire 8.2.0). global.json pins SDK 8.0.400 with rollForward latestFeature, so a .NET 10-only SDK will not satisfy it. CI command is `dotnet test eShop.Web.slnf` in .github/workflows/pr-validation.yml:33 after aspire workload install. Test paths recorded in the header. No Dockerfiles.
- 20:58 implement: `dotnet build eShop.Web.slnf -p:LibraryRestore=false` succeeded, 0 errors, 3 ASPIRE010 warnings. Product diff against release/8.0 is 159 additions and 131 deletions, 290 reviewable lines, under the 500 cap, so the cohesion exception was not needed. Restore named Grpc 2.84.0, Polly.Core 8.8.0, HealthChecks.Uris 9.0.0, Yarp 2.3.0, Duende 7.0.8, OpenTelemetry 1.19.x, AutoMapper 15.1.3, Semantic Kernel 1.71.0, Swashbuckle 10.2.3, and Google.Protobuf 3.36.2. Compiler named Aspire.AppHost.Sdk/13.6.0 on the three IsAspireHost projects, RabbitMQ.Client 7 async channel APIs, OpenAPI 2 in ServiceDefaults, a local CS0618 suppression for the Semantic Kernel embedding API, the eventing subscriber in AppHost Extensions.cs, and AddDeployment(name, model, version) in Program.cs. Program.cs stayed. AppHost.cs was not added. Pgvector.EntityFrameworkCore stayed at 0.2.1. docker info failed, so functional tests are still outstanding.
- 21:05 verify: recipe exit 0. Basket.UnitTests passed 3. Ordering.UnitTests passed 29. The recipe printed "Functional tests were not run because Docker is missing." Verifier on a fresh context confirmed the SHA, the 290-line count, and the must-nots. It marked DEMO NOTES partial until the PR body includes that sentence. exception: Swashbuckle 10.2.3, AutoMapper 15.1.3, and AspNetCore.HealthChecks.Uris 9.0.0 cross a major. A same-major pin does not build. Swashbuckle 6 cannot bind Microsoft.OpenApi 2. AutoMapper 13 fails NU1903 (GHSA-rvv3-g6hj-g44x, patched at 15.1.1). HealthChecks.Uris 8.0.1 is an NU1109 downgrade against Aspire 13, which wants >= 9.0.0.
- 21:05 deliver: draft PR 1 opened, then the body is updated with the quoted tests and the Docker sentence. Merge row stays skipped.
