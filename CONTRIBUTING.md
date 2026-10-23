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
- Integration tests against PostgreSQL, MySQL, and SQL Server need those databases. CI provides them as service containers. Locally, start them with WSL containers (`wslc`, WSL 2.9.3 or later); the script for it comes with the integration-test row of the roadmap. Docker is not used in this repository.

## Language

Code, comments, tests, commit subjects, pull request titles, and `README.md` are in English. The design documents, Issue bodies, and pull request bodies may be in Japanese. `README.ja.md` is the Japanese translation of `README.md` and changes in the same pull request.

## Versioning

Packages are versioned with SemVer from `VersionPrefix` in `Directory.Build.props`. Before 1.0, a minor version may break the public API. Nothing is published to NuGet yet; publishing is done only by a release workflow, which comes with its roadmap row.

## Security

See `SECURITY.md`. Do not report vulnerabilities in public Issues.
