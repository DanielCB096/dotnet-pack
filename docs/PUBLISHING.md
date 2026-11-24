# Publicar y consumir los paquetes

Esta guía explica lo que ocurre cuando subes una etiqueta `v*` y cómo instalar
los paquetes desde GitHub Packages.

## 1. Publicar una versión

```powershell
git tag v0.2.0
git push origin v0.2.0
```

Eso lanza el workflow `publish`, que:

1. Restaura, compila y prueba.
2. Genera los cinco `.nupkg`.
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
dotnet add package PipelineKit.Incremental --version 0.2.0
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
<Version>0.2.0</Version>
```

Para una nueva entrega, cámbiala, actualiza `CHANGELOG.md`, haz commit y sube
una etiqueta nueva:

```powershell
git tag v0.3.0
git push origin v0.3.0
```
