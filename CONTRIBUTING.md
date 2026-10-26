# Contributing to AuraORM

Thank you for your interest. AuraORM is being rebuilt from its requirements. The design is `docs/design.ja.md` (Japanese), the order of work is `docs/roadmap.md`, and the conventions are `docs/conventions.md`. Agents start at `AGENTS.md`.

## Workflow

1. If the design still branches, it is settled first (a Grill, then a `design` Issue that updates `docs/design.ja.md`).
2. Every change starts from an Issue opened with a template in `.github/ISSUE_TEMPLATE/`. Blank issues are disabled. Pull requests without an Issue are not accepted.
3. Branch from `main` as `type/<issue-number>-<slug>` (for example `feat/12-select-joins`). Contributors outside the maintainers work from a fork.
4. Run `./build.ps1` before opening a pull request. It checks formatting (CSharpier), builds with warnings as errors in Release and Debug, and runs every test project.
5. Open the pull request with the template. Put `Closes #N` alone on a line under `## Related`.
6. CI must be green on every job. CI is the merge gate; a local run is a note, not a substitute.
7. A maintainer reviews and squash-merges.

## Commits and pull requests

Titles follow Conventional Commits: `type(scope): subject`, in English. Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `build`, `ci`.

A pull request that adds or changes generated SQL lists the builder calls and the SQL for each dialect under `## SQL`, and updates the golden tests and `docs/design.ja.md`.

## Requirements

- .NET 10 SDK (`global.json`) and the .NET 8 runtime.
- `dotnet tool restore` once after cloning (CSharpier).
- Integration tests against PostgreSQL, MySQL, MariaDB, and SQL Server need those databases. CI provides them as service containers, at the minimum and the latest version of each (`tools/databases.json`). Locally, start them with WSL containers (`wslc`, WSL 2.9.3 or later): `./tools/db.ps1 up`, `./tools/db.ps1 test`, `./tools/db.ps1 down`. Without them, those tests are skipped locally. Docker is not used in this repository.

## Language

Code, comments, tests, commit subjects, pull request titles, and `README.md` are in English. The design documents, Issue bodies, and pull request bodies may be in Japanese. `README.ja.md` is the Japanese translation of `README.md` and changes in the same pull request.

## Versioning

Packages are versioned with SemVer from `VersionPrefix` in `Directory.Build.props`. Before 1.0, a minor version may break the public API. Nothing is published to NuGet yet; publishing is done only by a release workflow, which comes with its roadmap row.

Every pull request packs the `AuraORM` package and checks what a consumer receives: the library and its XML docs, the generator under `analyzers/dotnet/cs`, the README, no package dependency, and the symbol package. Run the same check with `./tools/package.ps1`. CI publishes the NativeAOT sample from that package.

## Security

See `SECURITY.md`. Do not report vulnerabilities in public Issues.
