# Publicar y consumir los paquetes

Esta guía explica lo que ocurre cuando subes una etiqueta `v*` y cómo instalar
los paquetes desde GitHub Packages.

## 1. Publicar una versión

```powershell
git tag v0.5.0
git push origin v0.5.0
```

Eso lanza el workflow `publish`, que:

1. Restaura, compila y prueba.
2. Genera los `.nupkg`.
3. Los publica en GitHub Packages.
4. Crea una release con los paquetes adjuntos.

No tienes que ejecutar `dotnet nuget push` a mano.

## 2. Ver los paquetes

- Releases: https://github.com/DanielCB096/dotnet-pack/releases
- Paquetes: https://github.com/DanielCB096?tab=packages

## 3. Consumirlos desde otro proyecto

Crea o edita `NuGet.Config` en el proyecto consumidor:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/DanielCB096/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="TU-USUARIO" />
      <add key="ClearTextPassword" value="TU_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

El token debe tener permiso `read:packages`.

Luego:

```bash
dotnet add package PipelineKit.Incremental --version 0.5.0
```

## 4. Publicar a mano (opcional)

```powershell
dotnet pack PipelineKit.slnx -c Release -o artifacts

dotnet nuget push "artifacts/*.nupkg" `
  --api-key "TU_TOKEN" `
  --source "https://nuget.pkg.github.com/DanielCB096/index.json"
```

## 5. Cambiar la versión

La versión está en `Directory.Build.props`:

```xml
<Version>0.5.0</Version>
```

Para una nueva entrega, cámbiala, actualiza `CHANGELOG.md`, haz commit y sube
una etiqueta nueva:

```powershell
git tag v0.6.0
git push origin v0.6.0
```

## 6. Instalar la plantilla

```powershell
dotnet new install PipelineKit.Templates::0.5.0 `
  --add-source "https://nuget.pkg.github.com/DanielCB096/index.json"
```

O desde el paquete local:

```powershell
dotnet new install artifacts/PipelineKit.Templates.0.5.0.nupkg
```

## 7. Publicar en NuGet.org con Trusted Publishing

La vía recomendada es **Trusted Publishing**: GitHub pide un token OIDC y NuGet
devuelve una API key temporal. No hay secretos permanentes.

1. En NuGet.org, entra a **Tu usuario → Trusted Publishing → New policy**.
2. Rellena:

| Campo | Valor |
|---|---|
| Policy Name | `PipelineKit GitHub Actions` |
| Package Owner | `Daniel_CB` |
| CI/CD Provider | GitHub Actions |
| Repository Owner | `DanielCB096` |
| Repository | `dotnet-pack` |
| Workflow File | `nuget-publish.yml` |
| Environment | vacío |

3. Scopes: marca **Push new packages and package versions**.
4. Glob Patterns:

```text
PipelineKit.*
```

5. Ejecuta **Actions → nuget-publish → Run workflow**.

El workflow usa `NuGet/login@v1` y publica con la clave temporal. No necesitas
crear el secreto `NUGET_API_KEY` para GitHub Actions.

### Alternativa local con API key

Si publicas desde tu máquina, la API key sigue funcionando:

```powershell
$env:NUGET_API_KEY = "TU_API_KEY"
.\scripts\publish-nuget.ps1
```

Los IDs `PipelineKit.*` están libres en NuGet.org a la fecha de esta guía.
