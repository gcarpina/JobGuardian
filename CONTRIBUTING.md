# Contributing

Thank you for your interest in JobGuardian.

## Before Contributing

Please review:

- README.md
- docs/QUICKSTART.md
- docs/architecture/Architecture-Overview.md
- relevant ADRs under docs/adr

## Development Principles

Contributions should:

- preserve backward compatibility whenever possible
- follow existing architectural decisions
- include automated tests
- maintain clear documentation
- prioritize simplicity over complexity

## Pull Requests

A pull request should include:

- implementation
- tests
- documentation updates when applicable

Keep the pull request description concise and factual. Describe only changes included in the
pull request; do not include planned follow-up work, deferred scope, or choices about what was
not changed.

## Architecture Changes

Significant architectural changes should be discussed before implementation.

Changes affecting:

- lease ownership semantics
- failure handling
- runtime state management
- persistence model
- public APIs

should be documented through a new ADR.

## Coding Standards

- use nullable reference types
- keep public APIs explicit
- prefer readability over cleverness
- follow existing naming conventions

## Testing

All tests must pass before submission.

```bash
dotnet test