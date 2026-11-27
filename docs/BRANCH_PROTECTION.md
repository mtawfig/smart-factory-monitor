# Branch protection for `main`

`main` only changes through pull requests that the owner merges. CI must be green first.
This page records the exact settings so they can be recreated or reviewed.

> **Current state (week 2): the rule is not enforced by GitHub.** Rulesets and classic branch protection need a
> public repository or a paid plan (GitHub Pro/Team) for private repositories. While the repository is private on
> the free plan, the workflow is kept by convention: every change arrives on a branch, through a pull request,
> and the owner merges it after CI is green. Re-create the ruleset below when the repository goes public (G6) or
> the plan changes.

## Repository settings (GitHub → Settings)

**General → Pull Requests**

- Allow squash merging: **on** (default message: pull request title and description)
- Allow merge commits: **off**
- Allow rebase merging: **off**
- Always suggest updating pull request branches: **on**
- Automatically delete head branches: **on**

**Rules → Rulesets → New branch ruleset**

| Setting | Value |
|---|---|
| Ruleset name | `protect-main` |
| Enforcement status | Active |
| Bypass list | Empty (the owner merges through the pull request like everyone else) |
| Target branches | Include default branch |
| Restrict deletions | On |
| Block force pushes | On |
| Require linear history | On |
| Require a pull request before merging | On; required approvals **0** (solo owner cannot approve their own pull request); dismiss stale approvals on; require conversation resolution on |
| Require status checks to pass | On; require branches to be up to date before merging on |

**Required status checks** (names as they appear on a pull request; each must have run once before it can be selected):

- `Secret scan (gitleaks)`
- `Conventional Commits`
- `.NET build and test`
- `Web lint, test and build`
- `Python ml-service`
- `Python simulator`
- `Compose and plant spec validation`
- `Container platform-api`
- `Container ingest-worker`
- `Container ml-service`
- `Container simulator`
- `Container web`
- `Gate G1 (compose end-to-end)`

## Why these choices

- **Squash only:** one Conventional Commit per pull request keeps `main` readable and changelog-ready; the
  branch can hold work-in-progress commits.
- **Zero required approvals:** GitHub does not let an author approve their own pull request, so requiring one
  approval would lock out a solo owner. The gate is green CI plus the owner's deliberate merge click.
- **No bypass:** if the owner can skip the rule, CI stops being a gate and becomes a suggestion.
- **Up-to-date branches:** checks run against what `main` will actually contain after the merge.

## Verify it works

1. `git push origin main` from a clone should be rejected with "protected branch".
2. Open a pull request with a failing test: the merge button stays disabled.
3. Fix the test: the button enables once all thirteen checks are green.
