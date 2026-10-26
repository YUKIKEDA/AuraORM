# AuraORM: agent entry

A type-safe SQL query builder for .NET in the spirit of Drizzle ORM: SQL's flexibility, types that reject only invalid queries, predictable SQL, NativeAOT. This file is the entry point for agents. Steps for people are in `CONTRIBUTING.md`. Enforced rules are in `.cursor/rules/`.

## Source

| What | Where |
| --- | --- |
| Design (canonical, Japanese) | `docs/design.ja.md` |
| Rejected alternatives | `docs/design-decisions.md` |
| Order and status of work | `docs/roadmap.md` |
| Projects, folders, coding conventions | `docs/conventions.md` |
| Build, format, and test gate | `./build.ps1` locally; CI (`.github/workflows/ci.yml`) is the merge gate |

The current row lives only in `docs/roadmap.md`. `.dev/` is local scratch; nothing there is a decision.

## Required rules

Read the rules that match the work before changing anything. `alwaysApply` rules apply to every change; always read `workflow`, `consent`, `git`, `scope`, and `sql`. After every merge comes the retrospective of `workflow`: what the work taught goes into these files, not into an agent's memory.

| Rule | One line |
| --- | --- |
| `workflow` | Grill, Issue, branch, PR, CI green, human squash merge, retrospective; labels; gates |
| `consent` | A question is not a decision; options need a recommendation; outward actions need an order |
| `git` | Finished work goes to a PR on its branch without asking; destructive git needs a named order; never `main` |
| `scope` | Requirements decide scope; no half-shipped capability, no dialect left behind |
| `sql` | One call is one clause; values are parameters; invalid queries do not compile; golden SQL per dialect |
| `defer` | Deferral only as a named roadmap row |
| `pace` | Split off what is outside the row; limit an investigation; long runs on the side |
| `open-ends` | No "not measured" and no unexplained result in a PR |
| `record-corrections` | A correction lands in the rule, design, or skill it belongs to |
| `review-comments` | A finding is about the head it was written on; a review the owner asks for is posted on the PR |
| `similar-findings` | Fix the same kind of problem where it also occurs (other dialects, other statements) |
| `no-dead-code` | Remove everything that only served a removed path |
| `spec-checks` | A test implements the whole design statement |
| `scratch` | Remove scratch files and containers in the turn that made them |
| `containers` | No Docker; WSL containers (`wslc`) locally, service containers in CI |
| `conventional-commits` | `type(scope): subject`, English |
| `pull-requests` | Title and six-heading body from the template |
| `github-closing` | `Closes #N` alone on a line; never a closing verb before an unfinished Issue |
| `language` | Code, rules, and README in English; the design, Issues, and PR bodies may be Japanese |
| `design-docs` | Where the design lives; design PR before code; claims about databases are cited |
| `powershell-shell` | Shell syntax on this setup |
| `engineering` | Layout, net8.0, warnings as errors, CSharpier, analyzers, banned APIs, the gate |
| `dependencies` | The runtime library has no package dependency |
| `perf` | Rendering, binding, and materialization rules |
| `bench` | How time and allocations are measured; what a claim needs |
| `aot` | No reflection, no expression trees, no dynamic code; the NativeAOT check |
| `csharp` | Style, naming, types, errors, XML docs (`*.cs`) |
| `testing` | xUnit v3, test layers, test docs (`tests/**`) |
| `dev-docs` | Figures, math, SQL blocks, and pseudo-code in `docs/` |
| `readme` | What README.md covers; no other library named in public documents; every csharp block compiled and run against the package |

## Skills

Skills live in `.agents/skills/` (copied to `.claude/skills/`). `skills-lock.json` pins the ones that come from outside, and each of those keeps its upstream `LICENSE` in its folder (both copies). Git tracks `.claude/skills/` as a copy, not as a link: a change to a skill changes both copies in the same commit.

- `grilling`: when the design branches, before an Issue.
- `github-closing`: before `gh pr create`, to check that the PR closes only what it finishes.
- `english-writing`: before writing XML docs, comments, test remarks, diagnostics, or rules.
- `natural-japanese`: write and check Japanese documents.

## Environment

- .NET 10 SDK (`global.json`) and the .NET 8 runtime (the tests target `net8.0` and `net10.0`). If `dotnet --list-sdks` shows none, install it; do not stop to ask.
- Windows is the owner's host; CI runs Windows, Linux, and macOS.
- Databases for integration tests: WSL containers locally through `./tools/db.ps1` (`up`, `test`, `down`; it calls `C:\Program Files\WSL\wslc.exe` by full path), service containers in CI, both from `tools/databases.json`. Never Docker (`.cursor/rules/containers.mdc`).
- The agent shell is PowerShell (`.cursor/rules/powershell-shell.mdc`).
