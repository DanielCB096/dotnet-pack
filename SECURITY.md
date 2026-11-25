# Security

## Supported versions

| Version | Supported |
|---|---|
| 0.6.x | Yes |
| 0.5.x | No |

## Reporting a vulnerability

Do not open a public issue for a security problem.

Contact the maintainer through GitHub:

https://github.com/DanielCB096

Include:

- affected package and version,
- a minimal reproduction,
- impact and any suggested fix.

## Demo credentials

`docker-compose.yml` uses `pipeline` / `pipeline` for RabbitMQ and the
observability stack runs with anonymous Grafana access. These are **local demo
defaults**, not production settings.

Never commit real credentials, API keys or production connection strings.
