# Traspaso de PipelineKit

Documento de continuidad para el equipo que asume el mantenimiento.

- **Versión actual:** `0.8.0`
- **Tag:** `v0.8.0`
- **Commit de referencia:** `7d75020`
- **Última actualización:** 2026-11-25
- **Estado:** release publicada en NuGet.org, GitHub Release y GitHub Pages; CI en verde.

## 1. Resumen

PipelineKit es una suite de 9 paquetes NuGet para construir pipelines de datos
incrementales en .NET: checkpoints, ventanas de extracción, calidad,
observabilidad, contrato de agente y utilidades de test.

Estado actual:

| Elemento | Valor |
|---|---|
| TFM | `net11.0` (`PipelineKit.AgentProtocol` también `netstandard2.0`) |
| SDK | `11.0.100` (`global.json`) |
| Tests | 65 en 8 proyectos |
| Cobertura | 68.1% líneas / 58.7% ramas (informativa) |
| Mutación | 100% en Incremental (15/15) y Quality (56/56), gate `break: 80` |
| Paquetes | 9 en NuGet.org como `0.8.0` |
| Docs | https://danielcb096.github.io/dotnet-pack/ |
| Release | https://github.com/DanielCB096/dotnet-pack/releases/tag/v0.8.0 |

Lo que **no** está hecho todavía y condiciona el 1.0:

- No hay baseline de API pública ni analizadores de compatibilidad.
- No hay referencia de API generada en DocFX.
- La cobertura no bloquea el CI.
- La mutación solo cubre 5 ficheros.
- El TFM es solo `net11.0` (STS); falta decidir si se multi-targetea `net10.0` (LTS).
- No hay firma de paquetes ni SBOM.
- El `CODEOWNERS` tiene una sola persona.

El plan para cerrar esto está en [roadmap-1.0.md](roadmap-1.0.md).

## 2. Mapa del repositorio

| Ruta | Contenido |
|---|---|
| `src/` | 8 paquetes de librería (`Incremental`, `Sqlite`, `EntityFrameworkCore`, `Quality`, `Telemetry`, `Telemetry.OpenTelemetry`, `AgentProtocol`, `Testing`) |
| `tests/` | 8 proyectos xUnit, uno por paquete o integración |
| `samples/LegacyBridge/` | Ejemplo end-to-end con SQLite, outbox, RabbitMQ y agente x86 |
| `templates/` | Paquete `PipelineKit.Templates` y plantilla `pipelinekit-job` |
| `benchmarks/` | BenchmarkDotNet y `RESULTS.md` |
| `observability/` | Collector, Tempo, Prometheus, Loki, Grafana y dashboard |
| `docs/` | Guías, ADRs, capturas, DocFX y `PUBLISHING.md` |
| `scripts/` | `publish-nuget.ps1` (fallback manual) y `make-storyboard.ps1` |
| `.github/workflows/` | CI, docs, mutation, observability, publish y nuget-publish |
| raíz | `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, `NuGet.Config`, `CHANGELOG.md` |

Ficheros que se tocan en casi cualquier cambio:

- `Directory.Build.props`: TFM, versión, metadatos de paquete.
- `Directory.Packages.props`: versiones centralizadas de NuGet.
- `Directory.Build.targets`: README, icono, SourceLink y símbolos.
- `README.md` y READMEs por paquete: comandos de instalación con la versión actual.

## 3. Puesta en marcha local

Requisitos:

- .NET SDK `11.0.100` (el `global.json` lo fija).
- Docker, solo para RabbitMQ y el stack de observabilidad.
- PowerShell 7+ para los scripts de `scripts/`.

```bash
dotnet tool restore
dotnet restore PipelineKit.slnx --configfile NuGet.Config
dotnet build PipelineKit.slnx -c Release
dotnet test PipelineKit.slnx -c Release
dotnet format PipelineKit.slnx --verify-no-changes
```

Empaquetado y plantilla:

```bash
dotnet pack PipelineKit.slnx -c Release -o artifacts
dotnet new install artifacts/PipelineKit.Templates.0.8.0.nupkg
dotnet new pipelinekit-job -n Demo -o /tmp/demo
dotnet restore /tmp/demo/Demo.csproj --source "$PWD/artifacts" --source https://api.nuget.org/v3/index.json
```

Mutación:

```bash
dotnet stryker --config-file stryker-config.incremental.json
dotnet stryker --config-file stryker-config.quality.json
```

Docs y observabilidad:

```bash
dotnet tool run docfx docs/docfx.json
docker compose -f observability/docker-compose.yml up -d
```

Notas:

- `docs/_site/` es salida generada y está ignorada por git.
- La mutación escribe en `StrykerOutput/` (ignorado).
- Los benchmarks usan BenchmarkDotNet `0.16.0-preview.2`; es la primera versión que reconoce el runtime de .NET 11. Solo afecta al proyecto de benchmarks.

## 4. CI/CD

| Workflow | Disparo | Qué valida |
|---|---|---|
| `ci` | push a `main`, PR, manual | build, format, paquetes vulnerables, tests + cobertura, pack, plantilla; jobs `windows-x86` y `rabbitmq-e2e` |
| `docs` | push a `docs/**` o al propio workflow, manual | DocFX y despliegue en GitHub Pages |
| `mutation` | lunes 06:00 UTC y manual | Stryker en Incremental y Quality; falla por debajo de 80 |
| `observability` | manual | levanta el stack, ejecuta LegacyBridge y verifica Prometheus, Tempo y Loki |
| `publish` | tag `v*` y manual | GitHub Packages + GitHub Release con los `.nupkg` |
| `nuget-publish` | manual | NuGet.org con Trusted Publishing (OIDC) |

Detalles operativos:

- `ci` no publica nada; sube `coverage` y `packages` como artefactos.
- `docs` despliega con el entorno `github-pages`, restringido a `main`.
- `mutation` y `observability` no bloquean PRs; son jobs programados o manuales.
- `publish` se dispara con el tag. Un `workflow_dispatch` manual también crea release, así que no se usa salvo prueba.
- `nuget-publish` es manual a propósito: evita publicar en cada tag.

## 5. Runbook de release

Proceso usado para `0.8.0`. Sirve para cualquier versión `0.x` o `1.0.0`.

### 5.1 Preparar la versión

1. Actualizar `<Version>` en `Directory.Build.props`.
2. Localizar todas las referencias a la versión anterior:

   ```bash
   git grep -n "0\.8\.0" -- ':!CHANGELOG.md'
   ```

3. Actualizar como mínimo:
   - `README.md`, READMEs de `src/*` y `templates/README.md`.
   - `templates/pipelinekit-job/PipelineKitJob.csproj` (referencias a paquetes).
   - `samples/LegacyBridge/LegacyBridge.Control/Program.cs` (versión de recurso OTel).
   - `docs/PUBLISHING.md`, `.github/ISSUE_TEMPLATE/bug_report.yml`.
   - `.github/workflows/ci.yml` (ruta del `.nupkg` de la plantilla).
4. Añadir la sección en `CHANGELOG.md` con `Added`, `Changed` y `Breaking` si aplica.

### 5.2 Verificar antes de publicar

```bash
dotnet restore PipelineKit.slnx --configfile NuGet.Config
dotnet build PipelineKit.slnx -c Release --no-restore
dotnet test PipelineKit.slnx -c Release --no-build
dotnet format PipelineKit.slnx --verify-no-changes
dotnet stryker --config-file stryker-config.incremental.json
dotnet stryker --config-file stryker-config.quality.json
dotnet pack PipelineKit.slnx -c Release --no-build -o artifacts
```

Comprobar el pack:

- 9 `.nupkg` y 8 `.snupkg` (Templates no genera símbolos).
- El nuspec de cada librería declara `net11.0`; AgentProtocol añade `netstandard2.0`.
- La plantilla referencia la nueva versión.

### 5.3 Publicar

1. Commit y push a `main`; esperar CI en verde.
2. Crear y subir el tag:

   ```bash
   git tag -a vX.Y.Z -m "PipelineKit X.Y.Z"
   git push origin vX.Y.Z
   ```

   Esto ejecuta `publish.yml`: publica en GitHub Packages y crea la GitHub Release.

3. Ejecutar `nuget-publish` manualmente sobre `main`:

   ```bash
   gh workflow run nuget-publish.yml --ref main
   ```

   O desde **Actions → nuget-publish → Run workflow**.

4. Verificar:

   ```bash
   curl -s https://api.nuget.org/v3-flatcontainer/pipelinekit.incremental/index.json
   curl -s -o /dev/null -w "%{http_code}\n" https://www.nuget.org/packages/PipelineKit.Incremental/X.Y.Z
   ```

   También: release con 9 assets, sitio de docs y CI de `main`.

La indexación de NuGet puede tardar varios minutos aunque el workflow termine en verde.

### 5.4 Rollback y problemas

| Situación | Acción |
|---|---|
| El workflow falla antes de publicar | Corregir y relanzar; no reutilizar el tag si ya apunta a un commit malo |
| NuGet aceptó unos paquetes y otros no | Relanzar `nuget-publish`; `--skip-duplicate` evita errores por los ya publicados |
| Un paquete tiene un fallo grave | **Unlist** en NuGet.org (no borrar); publicar un patch y documentarlo |
| La release de GitHub está mal | Editar la release; el tag no se mueve una vez publicado |
| Docs no despliega | Comprobar el entorno `github-pages` y relanzar `docs` desde `main` |

NuGet no permite reemplazar una versión: cualquier corrección es una versión nueva.

## 6. Infraestructura y credenciales

| Elemento | Detalle |
|---|---|
| GitHub | `DanielCB096/dotnet-pack`, rama `main` |
| NuGet.org | cuenta `Daniel_CB`; los paquetes son `PipelineKit.*` |
| Trusted Publishing | política para owner `Daniel_CB`, repo `DanielCB096/dotnet-pack`, workflow `nuget-publish.yml`, scope Push, glob `PipelineKit.*` |
| Variable de repo | `NUGET_USER` opcional; el workflow usa `Daniel_CB` por defecto |
| Secretos de Actions | **ninguno** para NuGet; se usa OIDC (`id-token: write`) |
| GitHub Pages | build type `workflow`; entorno `github-pages` restringido a `main` |
| CODEOWNERS | `* @DanielCB096`; ampliar al equipo antes del traspaso |

El script `scripts/publish-nuget.ps1` es el fallback local y sí requiere
`NUGET_API_KEY` en el entorno. Lee la versión de `Directory.Build.props` si no
se pasa `-Version`.

Acciones que requieren permisos de administración:

- Cambiar la política de Trusted Publishing en NuGet.org.
- Cambiar reglas del entorno `github-pages`.
- Transferir el repositorio o los paquetes.
- Rotar propietarios de NuGet/GitHub.

## 7. Invariantes de diseño

No romper estas reglas sin un ADR nuevo:

1. **Checkpoint después del sink** (ADR 0001). Un fallo del sink no avanza el checkpoint.
2. **Un solo efecto de caja por batch** y outbox en la misma transacción (ADR 0004).
3. **Sin infraestructura en el core** (ADR 0003): SQLite, EF, RabbitMQ y OTel viven en paquetes separados.
4. **Agente aislado por contrato JSON** (ADR 0002): request, progress y result en ficheros; códigos de salida estables.
5. **.NET 11 como baseline** (ADR 0007): `net11.0`; AgentProtocol mantiene `netstandard2.0`.
6. **Paquetes pequeños y componibles**: evitar dependencias nuevas en el core sin justificarlo.

## 8. Calidad: qué está cubierto y qué no

Cubierto en CI:

- Build sin warnings (`TreatWarningsAsErrors`).
- 65 tests unitarios y de integración.
- `dotnet format --verify-no-changes`.
- Paquetes vulnerables (`dotnet list package --vulnerable`).
- Plantilla instalable y ejecutable.
- Publicación y ejecución `win-x86`.
- RabbitMQ de punta a punta.
- Cobertura como artefacto (sin umbral).
- Mutación semanal con umbral 80.
- Observabilidad manual contra el stack real.

Huecos conocidos:

| Hueco | Impacto | Siguiente paso |
|---|---|---|
| Sin baseline de API pública | Alto para 1.0 | Añadir `PublicApiAnalyzers` y `ApiCompat` |
| Sin referencia de API en DocFX | Medio | Generar metadatos XML en `docfx.json` |
| Mutación limitada a 5 ficheros | Medio | Ampliar a runner, EF y telemetría |
| Cobertura sin umbral | Medio | Fijar un mínimo tras estabilizar tests |
| Solo `net11.0` (STS) | Alto para consumidores LTS | Decidir multi-target `net10.0` antes de 1.0 |
| BenchmarkDotNet en preview | Bajo | Actualizar cuando haya estable con .NET 11 |
| Sin firma de paquetes ni SBOM | Medio | Decidir en el roadmap de 1.0 |
| Bus factor 1 | Alto | Ampliar CODEOWNERS y permisos |

## 9. Checklist de la primera semana

- [ ] Clonar, restaurar, compilar y ejecutar los 65 tests.
- [ ] Ejecutar `dotnet pack` e inspeccionar un nuspec (`net11.0` y dependencias).
- [ ] Instalar la plantilla `0.8.0` y ejecutar un job generado.
- [ ] Generar DocFX y revisar el sitio en local.
- [ ] Ejecutar Stryker en ambas configuraciones.
- [ ] Levantar el stack de observabilidad y ejecutar `observability`.
- [ ] Relanzar cada workflow manualmente en una rama de prueba.
- [ ] Verificar acceso a NuGet.org, Trusted Publishing y GitHub Pages.
- [ ] Leer ADRs 0001–0007, `docs/architecture.md` y `docs/code-guide.md`.
- [ ] Revisar el roadmap y cerrar las decisiones D1–D3.
- [ ] Actualizar CODEOWNERS y contactos.

## 10. Contactos y propiedad

Rellenar antes de cerrar el traspaso:

| Responsabilidad | Persona/equipo |
|---|---|
| Mantenimiento del código | _pendiente_ |
| Propietario de NuGet.org | _pendiente_ |
| Administrador de GitHub/Pages | _pendiente_ |
| Contacto de seguridad | _pendiente_ |
| On-call / escalado | _pendiente_ |

Mientras no se actualice, el propietario actual es `@DanielCB096`.

El plan detallado para alinear el nombre del repositorio con el producto está en
[rename-repo.md](rename-repo.md).
