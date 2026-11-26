# Roadmap a 1.0

Objetivo: convertir PipelineKit `0.8.0` en una `1.0.0` con API pública estable,
contratos versionados, política de compatibilidad y documentación de referencia.

Este documento es una propuesta de trabajo. Las decisiones marcadas como
**D1–D7** deben cerrarse antes de la primera release candidate.

## 1. Definición de “1.0 terminado”

- [ ] API pública inventariada y con baseline automático.
- [ ] Contratos no-C# versionados: checkpoint, outbox, JSON del agente y telemetría.
- [ ] Política SemVer y ventana de soporte escritas.
- [ ] Referencia de API publicada en DocFX.
- [ ] Guía de migración `0.8 → 1.0` y `net10 → net11`.
- [ ] `net10.0` (LTS) decidido: multi-target o soporte declarado solo para `net11.0`.
- [ ] Dos release candidates publicadas y probadas por al menos un consumidor externo.
- [ ] Sin bugs P0/P1 abiertos durante la ventana de RC.
- [ ] CODEOWNERS y permisos con más de una persona.
- [ ] Release `1.0.0` firmada o decisión explícita de no firmar.

## 2. Superficie actual

| Paquete | TFM | Estado propuesto para 1.0 |
|---|---|---|
| `PipelineKit.Incremental` | `net11.0` | **Estable** |
| `PipelineKit.Incremental.Sqlite` | `net11.0` | **Estable** |
| `PipelineKit.Incremental.EntityFrameworkCore` | `net11.0` | **Estable** |
| `PipelineKit.Quality` | `net11.0` | **Estable** |
| `PipelineKit.Telemetry` | `net11.0` | **Estable** |
| `PipelineKit.Telemetry.OpenTelemetry` | `net11.0` | **Estable** |
| `PipelineKit.AgentProtocol` | `netstandard2.0`, `net11.0` | **Estable con contrato JSON congelado** |
| `PipelineKit.Testing` | `net11.0` | **Estable con matiz** (ver D1) |
| `PipelineKit.Templates` | plantilla | Sigue la versión de los paquetes |

## 3. Líneas de trabajo

### 3.1 Inventario y congelación de API

1. Añadir `Microsoft.CodeAnalysis.PublicApiAnalyzers` a los proyectos empaquetables.
2. Generar y commitear `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`.
3. Activar `EnablePackageValidation` y un baseline de `ApiCompat` para detectar
   cambios binarios entre versiones.
4. Revisar cada tipo público y clasificarlo:
   - **Stable**: se congela en 1.0.
   - **Experimental**: se marca con un atributo propio y puede cambiar en minors.
   - **Internal**: se mueve a `internal` antes de 1.0 si no es necesario.
5. Quitar `CS1591` de `NoWarn` en paquetes públicos y completar XML docs.
6. Revisar nombres, nulabilidad y valores por defecto antes de congelar.

Criterio de salida: el CI falla si se añade, elimina o cambia un miembro público
sin actualizar el baseline.

### 3.2 Contratos que no son API de C#

| Contrato | Qué congelar | Prueba necesaria |
|---|---|---|
| Checkpoint SQLite/EF | Tabla, columnas, claves y semántica de commit | Test de esquema y migración desde una BD 0.8 |
| Outbox | Campos JSON, clave de idempotencia, orden | Golden file + test de relé duplicado |
| Agente | Nombres de fichero, campos JSON, casing, nulos omitidos, códigos de salida | Golden files de request/progress/result |
| Telemetría | Nombres de métricas, atributos, unidades y spans | Test que fije nombres y unidades |
| Dashboard | Consultas PromQL/LogQL y paneles | Smoke test del JSON de Grafana |
| Configuración | Nombres de variables de entorno y opciones | Test de binding |

Recomendación para el agente: añadir un campo `protocolVersion` a
`AgentRequest`/`AgentResult` antes de 1.0, o documentar que la evolución es
**solo aditiva** y que los campos desconocidos se ignoran. Ver D2.

### 3.3 Compatibilidad y soporte

Escribir `docs/compatibility.md` con:

- Qué es breaking: API pública, nombres JSON, esquema de checkpoint, códigos de
  salida, TFM, suelo de dependencias y comportamiento de commit.
- Deprecación: `[Obsolete]` durante al menos un minor; retirada en el siguiente major.
- Ventana de soporte: parches de seguridad y bugs críticos.
- Versiones mínimas de dependencias y política de Dependabot.
- Plataformas soportadas: Linux x64 en CI, Windows x86 en el sample; declarar el resto.
- Política de TFM y ciclo de vida de .NET.

### 3.4 Documentación y muestras

1. Generar referencia de API en DocFX a partir de los XML docs:

   ```json
   "metadata": [
     {
       "src": [{ "src": "../src", "files": ["**/*.csproj"] }],
       "dest": "api"
     }
   ]
   ```

2. Añadir quickstarts por paquete y un “de cero a pipeline” de 10 minutos.
3. Guía de migración `0.7 → 0.8` (net10 → net11) y `0.8 → 1.0`.
4. Troubleshooting: checkpoints que no avanzan, sink que falla, outbox duplicado,
   agente que no arranca, métricas que no llegan.
5. Checklist de producción: WAL, backups, concurrencia, timeouts, reintentos.

### 3.5 Ingeniería de release

- Definir el proceso de RC: `1.0.0-alpha.1`, `beta.1`, `rc.1`, `rc.2`.
- Decidir firma de paquetes NuGet y SBOM (CycloneDX); ver D5.
- Verificar builds reproducibles (`Deterministic` ya está activo).
- Automatizar el checklist de release en un script.
- Documentar unlist/deprecación y el proceso de security advisory.

### 3.6 Calidad

- Fijar un umbral de cobertura (propuesta: 60% líneas, sin bajar del actual).
- Ampliar mutación a `IncrementalRunner`, `EfCoreCheckpointStore` y telemetría.
- Añadir tests de contrato golden para JSON y esquema.
- Añadir un job de `ApiCompat` y otro de enlaces rotos en docs.
- Mantener `windows-x86`, RabbitMQ y observabilidad como jobs de integración.

## 4. Decisiones pendientes

| ID | Decisión | Opciones | Recomendación |
|---|---|---|---|
| D1 | ¿`PipelineKit.Testing` es estable en 1.0? | Estable / Experimental | **Estable con matiz**: APIs principales congeladas; helpers nuevos permitidos en minors |
| D2 | Versionado del contrato de agente | Campo `protocolVersion` / solo aditivo | **Añadir `protocolVersion` antes de 1.0** y aceptar campos desconocidos |
| D3 | TFM | Solo `net11.0` / `net10.0;net11.0` | **Evaluar multi-target**: EF Core 11 solo trae `net11.0`, así que requiere EF 10 condicional para `net10.0` y doble matriz de tests |
| D4 | Telemetría | Congelar nombres actuales / adoptar semconv | Congelar nombres actuales y documentar el mapeo a semconv |
| D5 | Firma y SBOM | Firmar / no firmar | SBOM sí; firma si el equipo tiene acceso al certificado |
| D6 | Ventana de soporte | 18 meses / 24 meses / alineada con .NET | Alineada con el ciclo de .NET, mínimo 12 meses por versión |
| D7 | Analizadores de API | Adoptar / no | **Adoptar** antes de la primera RC |

### Nota sobre D3

`Microsoft.EntityFrameworkCore` y `Microsoft.EntityFrameworkCore.Sqlite` 11.0.0
solo publican `net11.0`. `Microsoft.Data.Sqlite` 11.0.0 publica `net10.0`. Por
tanto, multi-target exige:

- `net10.0` con EF Core 10.0.x y `net11.0` con EF Core 11.0.0 (versiones
  condicionales en `Directory.Packages.props`).
- Tests en ambas matrices.
- Documentar qué funcionalidades solo existen en `net11.0`.

Si el coste no se justifica, la alternativa es declarar `net11.0` como único
TFM soportado y revisar la decisión en .NET 12 (LTS).

## 5. Fases propuestas

| Fase | Duración | Entregables |
|---|---:|---|
| 0. Traspaso | 1 semana | Checklist de la primera semana, accesos, decisiones D1–D3 |
| 1. API | 2 semanas | Analizadores, baseline, inventario, XML docs |
| 2. Contratos | 2 semanas | Golden tests, `protocolVersion`, esquema de checkpoint, compatibilidad |
| 3. Docs | 2 semanas | Referencia de API, quickstarts, migración, troubleshooting |
| 4. RC | 2–3 semanas | `rc.1`, `rc.2`, feedback externo, bugs corregidos |
| 5. 1.0 | 1 semana | Release, anuncio, verificación de paquetes |

Las duraciones son orientativas y dependen de D3 (multi-target añade al menos
una semana).

## 6. Checklist de release 1.0

- [ ] Decisiones D1–D7 cerradas y documentadas en ADRs.
- [ ] Baseline de API actualizado y `ApiCompat` en verde.
- [ ] Contratos JSON y esquema de checkpoint con golden tests.
- [ ] Referencia de API publicada.
- [ ] Guías de migración revisadas por alguien externo al equipo.
- [ ] `README`, CHANGELOG y READMEs de paquete con `1.0.0`.
- [ ] Plantilla `1.0.0` probada desde NuGet, no solo desde artefactos locales.
- [ ] RC probada por un consumidor real.
- [ ] Release notes con breaking changes y guía de migración.
- [ ] SBOM (y firma, si aplica) adjuntos o publicados.
- [ ] CODEOWNERS y contactos actualizados.
- [ ] Plan de rollback/unlist documentado.

## 7. No objetivos para 1.0

- Nuevos conectores de bases de datos o sinks específicos.
- Orquestador o scheduler propio.
- UI de administración.
- Soporte de .NET Framework más allá del contrato `netstandard2.0` del agente.
- Reescritura del motor de ejecución.

Estos puntos pueden entrar después de 1.0 sin romper la API si se mantiene la
compatibilidad de los contratos actuales.
