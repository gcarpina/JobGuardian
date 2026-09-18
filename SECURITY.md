# Security Policy

## Supported Versions

JobGuardian is currently in active development.

Security fixes are generally applied to the latest supported release.

| Version | Supported |
|----------|----------|
| Latest Release | ✅ |
| Older Releases | ❌ |
| Development Builds | ⚠️ Best effort |

---

# Reporting a Vulnerability

Please do not report security vulnerabilities through public GitHub issues.

Instead, contact the maintainers privately and provide as much information as possible.

Include:

- affected JobGuardian version
- provider configuration
- reproduction steps
- potential impact
- proof of concept if available

Examples of security-related issues include:

- lease ownership bypass
- privilege escalation
- denial of service
- data exposure
- tenant isolation violations
- improper state manipulation
- authentication or authorization bypasses

---

# What Happens After Reporting

When a security report is received:

1. The issue is reviewed and validated
2. Impact and exploitability are assessed
3. A fix is developed
4. A patched release is prepared
5. Public disclosure occurs after a fix is available

We will make a best effort to acknowledge valid reports promptly.

---

# Security Scope

The security model of JobGuardian is primarily concerned with:

- distributed lease ownership
- execution coordination
- state consistency
- persistence integrity
- tenant isolation
- data integrity
- denial-of-service resistance

The framework assumes that:

- the hosting environment is trusted
- database credentials are managed securely
- infrastructure access controls are correctly configured

---

# Responsible Disclosure

We ask security researchers and users to follow responsible disclosure practices.

Please allow maintainers reasonable time to investigate and fix issues before publicly disclosing vulnerabilities.

This helps protect users and allows coordinated remediation.

---

# Security Principles

JobGuardian is designed around the following principles:

## Single Ownership

Only one execution owner may hold a lease at any given time.

## Explicit Trust Boundaries

Ownership verification must not rely on local process state alone.

## Fail Safe Behavior

When ownership cannot be verified, execution should stop.

## Persistence Integrity

Runtime state and lease information must remain consistent even in failure scenarios.

## Least Privilege

Applications should grant only the minimum permissions required for JobGuardian to operate.

---

# Security Reviews

Significant changes affecting:

- lease acquisition
- ownership validation
- heartbeat processing
- runtime state transitions
- persistence providers
- multi-tenancy

should be reviewed from a security perspective before release.

---

# Disclaimer

JobGuardian is provided as open source software without warranty.

Users remain responsible for validating the framework within their own environments and security requirements.