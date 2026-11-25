# Deep dive: `IncrementalRunner`

Archivo: `src/PipelineKit.Incremental/IncrementalRunner.cs`

Es la pieza que coordina una ejecución. No sabe leer ni escribir datos: solo
orquesta el checkpoint, el source y el sink.

## Dependencias

```csharp
public IncrementalRunner(
    ICheckpointStore store,
    TimeProvider? timeProvider = null,
    ILogger<IncrementalRunner>? logger = null)
```

- `ICheckpointStore`: lee y confirma el marcapáginas.
- `TimeProvider`: reloj inyectable; en tests se usa `FixedTimeProvider`.
- `ILogger`: trazas de la ejecución.

## Secuencia

```text
RunAsync
  │
  ├─ 1. GetAsync(key)                  ← checkpoint anterior
  ├─ 2. ExtractionWindowPlanner.Plan   ← ventana con solapamiento
  ├─ 3. source.ReadAsync(window)       ← filas en streaming
  ├─ 4. sink.WriteAsync(context, rows) ← escritura idempotente
  ├─ 5. tracking.Completed?            ← ¿consumió todo?
  ├─ 6. CommitAsync(next)              ← solo ahora avanza
  └─ 7. IncrementalResult
```

## 1. Identidad del checkpoint

```csharp
var key = new CheckpointKey(
    request.Pipeline,
    request.Source,
    request.Table,
    request.Strategy.ToString());
```

Un mismo pipeline puede tener varios checkpoints: por país, por tabla y por
estrategia. La combinación de los cuatro forma la clave.

## 2. Ventana

```csharp
var checkpoint = await _store.GetAsync(key, ct);
var window = ExtractionWindowPlanner.Plan(request, checkpoint, startedAt);
```

Para `TimestampAndKey`, la ventana empieza en
`lastModified - overlap`. El solapamiento permite releer actualizaciones
tardías. Para `Key`, empieza después de `lastKey`.

## 3. Streaming con seguimiento

```csharp
var tracking = new TrackingAsyncEnumerable(source.ReadAsync(window, ct));
```

`TrackingAsyncEnumerable` envuelve el `IAsyncEnumerable` del source y, a medida
que el sink consume filas, registra:

- número de filas,
- máxima clave,
- máxima fecha,
- último periodo,
- último hash,
- si el stream se consumió completo.

Esto evita materializar todo el lote en memoria.

## 4. El sink escribe

```csharp
writeResult = await sink.WriteAsync(
    context,
    tracking.TrackAsync(ct),
    ct);
```

Si el sink lanza una excepción, el `catch` registra el fallo y **no** se llama a
`CommitAsync`. El checkpoint anterior sigue intacto.

## 5. Guardia de consumo completo

```csharp
if (!tracking.Completed)
{
    throw new InvalidOperationException(
        "The batch sink did not consume the full batch; the checkpoint was not committed.");
}
```

Si un sink se detiene a la mitad, el runner se niega a avanzar. Es mejor repetir
el lote que perder filas silenciosamente.

## 6. El checkpoint nunca retrocede

```csharp
var next = new Checkpoint(
    Max(checkpoint?.LastKey, writeResult.LastKey ?? tracking.LastKey),
    Max(checkpoint?.LastModified, writeResult.LastModified ?? tracking.LastModified),
    MaxPeriod(checkpoint?.Period, writeResult.Period ?? tracking.LastPeriod),
    writeResult.RowHash ?? tracking.LastHash);
```

Se toma el **máximo** entre el checkpoint anterior y lo visto en el lote.

Ejemplo: el checkpoint está en la clave 8. Llega una actualización tardía de la
clave 2. El lote contiene la clave 2, pero el nuevo checkpoint sigue en 8.

Sin este `Max`, una actualización tardía haría retroceder el marcapáginas y se
releerían filas indefinidamente.

## 7. Commit y resultado

```csharp
await _store.CommitAsync(key, next, ct);

return new IncrementalResult(
    batchId,
    tracking.Rows,
    writeResult.RowsWritten,
    writeResult.RowsRejected,
    duration,
    CheckpointCommitted: true,
    RowsUnchanged: writeResult.RowsUnchanged);
```

`IncrementalResult` separa:

- `RowsExtracted`: filas leídas,
- `RowsWritten`: filas escritas de verdad,
- `RowsRejected`: filas a cuarentena,
- `RowsUnchanged`: filas releídas pero sin cambios.

## Qué pasa si falla

```text
1. GetAsync devuelve checkpoint = C.
2. Se leen filas.
3. El sink escribe y falla.
4. El runner lanza la excepción.
5. CommitAsync NO se ejecuta.
6. La próxima ejecución vuelve a leer desde C.
```

El sink debe tolerar que le llegue otra vez la misma fila. En LegacyBridge se
resuelve con:

- clave natural `order_id`,
- hash de fila,
- `INSERT ... ON CONFLICT DO UPDATE`,
- comparación de hash para saltar filas iguales.

## Cómo se prueba

Tests en `tests/PipelineKit.Incremental.Tests`:

- `First_run_extracts_everything_and_commits`
- `Second_run_starts_after_the_confirmed_checkpoint`
- `Sink_failure_does_not_advance_the_checkpoint`
- `Row_hash_is_stable_and_key_order_independent`
- `Timestamp_window_applies_overlap`
- `Late_update_with_lower_key_is_read_and_does_not_regress_the_checkpoint`

El último es el que protege el `Max`: una actualización tardía con clave menor
se lee, pero el checkpoint no retrocede.

## Cómo depurarlo

1. Pon un breakpoint en `RunAsync` y revisa `checkpoint` y `window`.
2. Si el sink no escribe, revisa `writeResult`.
3. Si el checkpoint no avanza, mira si `CommitAsync` se ejecutó.
4. Si se releen filas, revisa el `Overlap` y el hash del sink.
5. Ejecuta los tests de `PipelineKit.Incremental.Tests`; son rápidos y cubren
   los fallos importantes.
