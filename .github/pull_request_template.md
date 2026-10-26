## Summary

<!-- What changed and why. Keep it short. May be Japanese. -->

-

## Related

<!-- `Closes #N` alone on a line when this PR finishes the Issue; `Part of #N` when it does not. -->

Closes #N

- Design: <!-- docs/design.ja.md §x.y / N/A -->

## Verification

<!-- Commands that ran and their result. CI is the merge gate; local runs are notes. -->

- [ ] CI green on every job (`gh pr checks`)
- [ ] `./build.ps1` locally
- [ ] NativeAOT publish (warnings / executable size): <!-- N/A unless the Issue names it -->
- [ ] Searches behind any "every X does Y" claim: <!-- N/A -->

## SQL

<!-- Queries whose generated SQL this PR adds or changes: the builder call and the SQL per dialect (golden tests). N/A otherwise. -->

N/A

## Risk / Rollback

- Risk:
- Rollback:

## Checklist

- [ ] Title follows Conventional Commits (`type(scope): subject`)
- [ ] CSharpier applied (`dotnet csharpier format .`)
- [ ] New public API has XML docs; new tests have `summary` + `remarks` (Preconditions / Steps / Expected)
- [ ] `CHANGELOG.md` has an entry under `[Unreleased]` for a change a user sees — or N/A
- [ ] `docs/`, `.cursor/rules/`, and skills updated when behavior or workflow changed — or N/A
- [ ] Corrections from the owner written into the file they belong to (`.cursor/rules/record-corrections.mdc`) — or N/A
