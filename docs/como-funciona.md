# Cómo funciona PipelineKit

PipelineKit es una caja de herramientas .NET para mover datos desde un sistema
antiguo hacia uno nuevo **sin perder filas, sin duplicarlas y pudiendo ver qué
pasó**.

No es un framework cerrado: son paquetes pequeños que se pueden usar juntos o
por separado.

## El problema

Copiar datos de un ERP legado a una base moderna tiene varios riesgos:

- no saber por dónde iba la última ejecución,
- perder filas si el proceso se cae a mitad,
- duplicar filas al reintentar,
- tumbar todo el lote por una fila corrupta,
- no detectar borrados,
- depender de un driver de 32 bits,
- no avisar a otros servicios,
- no poder diagnosticar qué pasó.

PipelineKit resuelve esas piezas.

## La idea central: el marcapáginas

- El **checkpoint** es el marcapáginas.
- El **runner** es el lector.
- El **source** es el libro.
- El **sink** es el cuaderno donde se copia.

Regla de oro:

> El marcapáginas solo se mueve cuando la copia está guardada.

Si el sink falla, el checkpoint no avanza. La siguiente ejecución vuelve a leer
la misma ventana. Por eso el sink debe ser idempotente.

Eso se llama **at-least-once**: al menos una vez.

## Flujo principal

```text
Fuente (ERP, SQL, API)
        │
        ▼
IIncrementalSource ──► IncrementalRunner ──► IBatchSink
                              │                    │
                              ▼                    ▼
                        Checkpoint            Staging / DW
                              │                    │
                              └── solo avanza ─────┘
                                  si el sink terminó
```

1. El runner lee el último checkpoint.
2. Calcula la ventana: fecha con solapamiento, clave o periodo.
3. Pide filas al source.
4. El sink valida, escribe y devuelve contadores.
5. Solo entonces se guarda el nuevo checkpoint.
6. Si el sink falla, el checkpoint no se guarda.

## Solapamiento y hash

Si ayer se procesó hasta las 10:00, hoy se empieza a las 09:55 para capturar
actualizaciones tardías. Eso puede releer filas que no cambiaron.

Para evitar reescrituras, cada fila tiene un **hash**. Si el hash coincide con
el guardado, la fila se cuenta como `unchanged` y no se reescribe.

## Paquetes

| Paquete | Responsabilidad |
|---|---|
| `PipelineKit.Incremental` | Checkpoints, ventanas, hash y runner |
| `PipelineKit.Incremental.Sqlite` | Checkpoints en SQLite |
| `PipelineKit.Incremental.EntityFrameworkCore` | Checkpoints con EF Core |
| `PipelineKit.Quality` | Reglas, cuarentena, deriva y reconciliación |
| `PipelineKit.Telemetry` | Trazas y métricas del pipeline |
| `PipelineKit.Telemetry.OpenTelemetry` | Envío a Collector/Tempo/Prometheus/Loki |
| `PipelineKit.AgentProtocol` | Contrato y host de agentes x86 |
| `PipelineKit.Testing` | Fuente falsa, sink colector y datos sucios |
| `PipelineKit.Templates` | `dotnet new pipelinekit-job` |

## La demo LegacyBridge

La demo simula un ERP antiguo en SQLite con 8 pedidos:

- 5 válidos,
- 1 con código demasiado largo,
- 1 con cantidad 0,
- 1 cancelado.

### Primera ejecución

```text
extracted=8, written=5, rejected=3
```

El sink valida, manda las malas a cuarentena, escribe las buenas en staging y
en el modelo dimensional, guarda un evento en outbox y solo entonces se mueve
el checkpoint.

### Segunda ejecución

```text
extracted=1, written=0, unchanged=1
```

Releyó una fila por el solapamiento, pero el hash dijo que no cambió.

### Tercera ejecución

Se actualiza un pedido, se inserta otro y se borra uno físicamente.

```text
extracted=3, written=2, unchanged=1
Reconciliación de borrados: 1
```

El incremental ve la actualización y la inserción. El borrado físico se detecta
comparando claves entre origen y warehouse.

## Agente aislado

```text
Control x64 ──request.json──► Agente x86
     ▲                            │
     └──── result.json ◄──────────┘
```

El control escribe la petición, el agente consulta y escribe NDJSON, y el host
lee el resultado. Si el agente se cuelga, el host lo mata. Así el driver de 32
bits vive en un proceso aparte.

## Outbox y RabbitMQ

El sink guarda en la misma transacción:

- los datos,
- el registro del lote,
- el evento en `outbox`.

Después, un relay publica los eventos pendientes:

- a `messages.ndjson` por defecto,
- a RabbitMQ si existe `RABBITMQ_HOST`.

Luego los marca como procesados. Si el relay cae entre publicar y marcar, el
evento se publica otra vez; el consumidor lo detecta por `id` y no lo reprocesa.

## Observabilidad

```text
Aplicación .NET
      │ OTLP
      ▼
Collector
  ├──► Tempo        (trazas)
  ├──► Prometheus   (métricas)
  └──► Loki         (logs)
          │
          ▼
       Grafana
```

El dashboard `PipelineKit pipelines` muestra batches, filas extraídas,
rechazadas, fallos, duración p95, tabla por pipeline y logs.

## Benchmarks

| Benchmark | Filas | Tiempo | Memoria |
|---|---:|---:|---:|
| Hash de 10 campos | — | 2.067 µs | 2.38 KB |
| Runner completo | 1,000 | 3.642 ms | 2.00 MB |
| Runner completo | 10,000 | 41.16 ms | 20.36 MB |

## CI/CD

En cada push:

1. **build-test-pack**: compila, ejecuta 65 tests, empaqueta y prueba la
   plantilla.
2. **windows-x86**: publica el ejemplo x86 autocontenido y lo ejecuta.
3. **rabbitmq-e2e**: publica y consume eventos con RabbitMQ.

En cada tag `v*`, el workflow `publish` publica en GitHub Packages y crea una
release. `nuget-publish` es manual y usa Trusted Publishing (OIDC).

## Cómo se usa

Con la plantilla:

```bash
dotnet new install PipelineKit.Templates::0.5.0
dotnet new pipelinekit-job -n MiPipeline
```

Con paquetes:

```bash
dotnet add package PipelineKit.Incremental --version 0.5.0
dotnet add package PipelineKit.Incremental.Sqlite --version 0.5.0
dotnet add package PipelineKit.Quality --version 0.5.0
```

Implementas `IIncrementalSource` y `IBatchSink`, y ejecutas:

```csharp
var runner = new IncrementalRunner(store);
var result = await runner.RunAsync(request, source, sink);
```

## Qué NO es

- No es un orquestador visual.
- No reemplaza Airflow o Temporal.
- No es un conector mágico para cualquier base de datos.
- No obliga a usar todas las piezas.

Es una caja de piezas para no reinventar el incremental, la calidad, la
telemetría y la idempotencia en cada proyecto.

## Resumen

```text
1. Leo desde un checkpoint.
2. Extraigo una ventana con solapamiento.
3. Valido y escribo de forma idempotente.
4. Solo entonces muevo el checkpoint.
5. Los errores van a cuarentena.
6. Los borrados se reconcilian.
7. Los eventos salen por outbox.
8. Todo se ve en Grafana.
```
