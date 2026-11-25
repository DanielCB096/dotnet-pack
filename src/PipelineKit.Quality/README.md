# PipelineKit.Quality

Data quality rules, quarantine, schema drift detection and reconciliation.

## Install

```bash
dotnet add package PipelineKit.Quality --version 0.7.0
```

## Usage

```csharp
var evaluator = new QualityEvaluator(
    [new NotNullRule("id"), new MaxLengthRule("code", 3)],
    quarantineStore);

var result = await evaluator.EvaluateAsync(row.Values, context);
```

Included rules:

- `NotNullRule`
- `MaxLengthRule`
- `RangeRule`
- `RegexRule`

Also includes `SchemaDriftDetector` and `Reconciliation` helpers. Quarantine
keeps the original value, rule, message and batch id.

Docs: https://github.com/DanielCB096/dotnet-pack/blob/main/docs/architecture.md
