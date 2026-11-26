# ADR 0005: NuGet Trusted Publishing for GitHub Actions

## Context

NuGet API keys are long-lived credentials. They must be stored, rotated and
protected. NuGet.org now supports short-lived credentials for GitHub Actions
through OpenID Connect.

## Decision

- The `nuget-publish` workflow runs manually.
- The job requests `id-token: write`.
- `NuGet/login@v1` exchanges the GitHub OIDC token for a temporary NuGet API key.
- The temporary key is used by `dotnet nuget push`.
- A Trusted Publishing policy in NuGet.org is scoped to:
  - owner `Daniel_CB`,
  - repository owner `DanielCB096`,
  - repository `dotnet-pack`,
  - workflow `nuget-publish.yml`.

The local `scripts/publish-nuget.ps1` keeps the API-key path for publishing from
a developer machine.

## Consequences

- No permanent NuGet secret is stored in GitHub.
- The workflow only works from the matching repository and workflow file.
- A policy change in NuGet.org can block publishing until updated.
- The manual trigger prevents accidental publishing on every tag.

## Addendum (2026-11-26)

The repository was renamed from `dotnet-pack` to `pipelinekit`. NuGet Trusted
Publishing validates the immutable GitHub repository ID (`#1470930886`), so the
policy remained valid and no edit was required.
