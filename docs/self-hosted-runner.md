# Self-Hosted Runner

This repository needs a Windows self-hosted runner because the mod build depends on a local `Slay the Spire 2` install.

## Required Label Set

The build and release workflows target:

```yaml
runs-on: [self-hosted, windows, x64, sts2-build]
```

The dedicated custom label is `sts2-build`.

## Why The Old Workflows Queued Forever

GitHub only schedules a job to a self-hosted runner when a runner exists with every requested label.

If the repository has no registered runner, or no runner with the matching labels, the workflow stays queued and never starts.

## Security Boundary

This repository is public.

Do not run public pull requests on the self-hosted runner.

That is why:

- metadata checks run on `windows-latest`
- build and release jobs run only on the dedicated `sts2-build` runner

## One-Time Setup

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Register-RepositoryRunner.ps1
```

Default install root:

```text
%USERPROFILE%\actions-runner\sts2-trainer-mod
```

Default runner name:

```text
<COMPUTERNAME>-sts2-build
```

## Verify

After registration:

1. Open the repository on GitHub.
2. Go to `Actions -> Runners`.
3. Confirm the runner is `Idle` and has labels:
   - `self-hosted`
   - `windows`
   - `x64`
   - `sts2-build`

## Service Management

The setup script installs the runner as a Windows service by default.

Useful checks:

```powershell
Get-Service | Where-Object { $_.Name -like 'actions.runner*' }
gh api repos/yynps737/sts2-trainer-mod/actions/runners
```
