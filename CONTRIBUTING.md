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

Packages are versioned with SemVer from `VersionPrefix` in `Directory.Build.props`. Before 1.0, a minor version may break the public API. Only the release workflow (`.github/workflows/release.yml`) publishes to NuGet.

## Releases

The owner releases. Once, before the first release:

1. On nuget.org, under the account's **Trusted Publishing**, add a policy for GitHub Actions: owner `YUKIKEDA`, repository `AuraORM`, workflow file `release.yml`, environment `release`.
2. In the repository settings, add the variable `NUGET_USER` with the nuget.org user name. No API key is stored.

For each release:

1. A pull request sets `VersionPrefix`, moves the entries under `## [Unreleased]` of `CHANGELOG.md` to `## [<version>] - <date>`, and updates the status line of `README.md` and `README.ja.md`.
2. After it is merged and CI has passed on `main`, run **Release** from the Actions tab on `main` with **dry-run** checked, and approve the `release` environment. It checks that CI passed on the commit, that `CHANGELOG.md` has the version and that neither nuget.org nor a tag has it (`./tools/release.ps1`), packs and checks the package and the README examples, and logs in to nuget.org.
3. Run it again with **dry-run** unchecked. It pushes the package and its symbols, then creates the tag `v<version>` and a GitHub Release whose notes are the CHANGELOG section.

A version on nuget.org cannot be replaced, only unlisted, so a fix is a new version.

Every pull request packs the `AuraORM` package and checks what a consumer receives: the library and its XML docs, the generator under `analyzers/dotnet/cs`, the README, no package dependency, and the symbol package. Run the same check with `./tools/package.ps1`. CI publishes the NativeAOT sample from that package.

The C# examples of `README.md` are built against that package and run (`./tools/package.ps1 -Suffix local.1`, then `./tools/readme.ps1 -Version 0.1.0-local.1`). A comment that shows SQL (`// SELECT ...`) must be what the example prints, and `README.ja.md` has the same code blocks.

## Security

See `SECURITY.md`. Do not report vulnerabilities in public Issues.
