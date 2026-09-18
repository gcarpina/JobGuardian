# ADR-001 - Protocol First

## Status

Accepted

## Context

JobGuardian is designed to support multiple implementation languages.

Planned SDKs:

- .NET
- Java
- Python

## Decision

The protocol is the primary artifact.

SDKs are implementations of the protocol.

The protocol must be defined before implementation begins.

## Consequences

All SDKs must comply with the protocol.

Contract tests will validate compliance.