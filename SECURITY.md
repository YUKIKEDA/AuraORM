# Security Policy

If you find a vulnerability, **do not write it in a public Issue**.

Report it through [GitHub Security Advisories](https://github.com/YUKIKEDA/AuraORM/security/advisories/new). No personal email address is published.

Response targets (best effort):

- Acknowledgement: within a few days
- Plan for a fix: depending on severity

AuraORM writes SQL. A way to make it put a caller's value into SQL text instead of a parameter, or to make an identifier escape its quotes, is a vulnerability (SQL injection). Report it here. Do not include real connection strings, credentials, or production data in reproduction steps.
