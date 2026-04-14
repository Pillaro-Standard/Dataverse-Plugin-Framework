# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 1.x     | :white_check_mark: |
| < 1.0   | :x:                |

---

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please report it responsibly.

### How to Report

1. **Do NOT open a public GitHub issue** for security vulnerabilities
2. **Email us directly** at: info@pillaro.cz
3. **Include the following information**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### What to Expect

| Timeframe | Action |
|-----------|--------|
| 48 hours | Initial acknowledgment of your report |
| 7 days | Preliminary assessment and severity evaluation |
| 30 days | Target resolution for confirmed vulnerabilities |

### Disclosure Policy

- We will work with you to understand and resolve the issue
- We will keep you informed of our progress
- We will credit you in the security advisory (unless you prefer anonymity)
- We request that you do not publicly disclose the issue until we have addressed it

---

## Security Best Practices

When using this framework in production:

### Plugin Assembly Security

- Sign assemblies with a strong name key
- Use sandbox isolation mode (`IsolationModeEnum.Sandbox`)
- Avoid storing sensitive data in unsecured configuration strings

### Dataverse Security

- Follow principle of least privilege for plugin registration
- Use service accounts with minimal required permissions
- Audit plugin execution logs regularly

### Development Security

- Keep dependencies up to date
- Review code changes for security implications
- Use static analysis tools to detect vulnerabilities

---

## Known Security Considerations

### Sandbox Limitations

Plugins run in a sandboxed environment with restrictions:

- No file system access
- No registry access
- Limited network access (only allowed endpoints)
- No reflection on system assemblies

### Data Handling

- Avoid logging sensitive entity data
- Use secure configuration for credentials
- Implement proper error handling to prevent information leakage

---

## Security Updates

Security updates will be released as patch versions and announced via:

- GitHub Security Advisories
- Release notes

We recommend enabling GitHub notifications for this repository to stay informed about security updates.
