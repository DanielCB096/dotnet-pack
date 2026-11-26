# Plan: renombrar el repositorio a `pipelinekit`

> **Estado:** ejecutado el 2026-11-26. Repositorio actual:
> `DanielCB096/pipelinekit`. La política de Trusted Publishing no requirió
> cambios porque NuGet valida el ID inmutable del repositorio.
>
> **Alcance:** solo cambia el nombre del repositorio. El producto, los paquetes y
> el namespace siguen siendo `PipelineKit`.

## 1. Decisión

| Elemento | Antes | Después |
|---|---|---|
| Producto | PipelineKit | PipelineKit (sin cambios) |
| Paquetes NuGet | `PipelineKit.*` | `PipelineKit.*` (sin cambios) |
| Namespace | `PipelineKit.*` | `PipelineKit.*` (sin cambios) |
| Repositorio | `DanielCB096/dotnet-pack` | `DanielCB096/pipelinekit` |
| GitHub Pages | `danielcb096.github.io/dotnet-pack/` | `danielcb096.github.io/pipelinekit/` |

El nombre `dotnet-pack` describe una herramienta de empaquetado, no un kit de
pipelines de datos. `PipelineKit` ya es el nombre del producto, de los paquetes
y de la documentación; alinear el repositorio elimina esa contradicción.

## 2. Qué cambia y qué no

| Cambia | No cambia |
|---|---|
| URL del repositorio y de las releases (GitHub redirige) | IDs de paquete y versiones |
| URL de GitHub Pages (sin redirección automática) | Cuenta y propietario de NuGet |
| Badges y enlaces del README | Política de Trusted Publishing (valida el ID inmutable) |
| `RepositoryUrl` y `PackageProjectUrl` de los paquetes nuevos | Workflow `nuget-publish.yml` |
| URL de clones nuevos y remotos locales | Feed de GitHub Packages (`nuget.pkg.github.com/DanielCB096`) |
| Nombre del repositorio en GitHub | Namespace, API pública, releases y tags (redirigen) |

## 3. Impacto y mitigación

| Impacto | Riesgo | Mitigación |
|---|---|---|
| GitHub redirige el repo antiguo al nuevo | Bajo | No crear nunca un repo nuevo con el nombre `dotnet-pack`; rompería la redirección |
| Pages cambia de URL y la antigua deja de servir | **Alto** | Decidir estrategia de URL antes de renombrar (sección 4) |
| Trusted Publishing | **Bajo** | Valida el ID inmutable del repo (`#1470930886`); verificar OIDC con `nuget-publish` |
| Enlaces en README/docs y badges | Medio | PR preparado de antemano; merge después del rename |
| Metadatos de paquetes ya publicados | Bajo | Apuntan al repo antiguo, que redirige; se refrescan en la siguiente versión |
| Remotos locales y forks | Bajo | `git remote set-url`; los forks pueden seguir el redirect |
| Webhooks/apps externas | Bajo | Revisar integraciones antes del cambio |

## 4. Estrategia para la URL de documentación

GitHub Pages no conserva la URL anterior al renombrar un project site. Hay tres
opciones; elegir una antes de ejecutar:

### Opción A — Aceptar la URL nueva (recomendada si no hay dominio)

- Nueva URL: `https://danielcb096.github.io/pipelinekit/`.
- Actualizar todos los enlaces y asumir que la URL antigua devolverá 404.
- No crear un repo `dotnet-pack` para redirigir: eso desactiva la redirección
  de GitHub para clones, issues y releases.

### Opción B — Dominio propio (recomendada si se quiere una URL estable)

- Configurar un dominio, por ejemplo `pipelinekit.dev`, en GitHub Pages.
- La URL de docs deja de depender del nombre del repositorio.
- Requiere DNS y un certificado; hacerlo antes del rename para no romper enlaces.

### Opción C — Repo de redirección en el nombre antiguo (no recomendada)

- Crear `dotnet-pack` con una página HTML que redirija a la nueva URL.
- **Rompe la redirección de GitHub** para el resto de URLs del repo antiguo.
- Solo tiene sentido si los enlaces a Pages son más importantes que los clones e
  issues antiguos, algo poco probable.

## 5. Precondiciones

- [ ] Aprobación del equipo y ventana de 60 minutos sin releases.
- [ ] Acceso de administración al repo de GitHub.
- [ ] Acceso a NuGet.org para verificar la política (no se edita si el ID no cambia).
- [ ] Acceso a la configuración de Pages y al entorno `github-pages`.
- [ ] Estrategia de URL de docs decidida (A, B o C).
- [ ] PR con todos los enlaces actualizados, preparado pero **sin fusionar**.
- [ ] Aviso a consumidores y al equipo.
- [ ] `gh` autenticado o acceso a la UI de GitHub.

## 6. Runbook

### Fase 0 — Preparar el PR de enlaces

1. Localizar todas las referencias:

   ```bash
   git grep -n "dotnet-pack" -- ':!docs/_site/**'
   ```

2. Actualizar, como mínimo:
   - `Directory.Build.props`: `PackageProjectUrl` y `RepositoryUrl`.
   - `README.md`: badges de CI/tests/coverage y enlace a docs.
   - `docs/README.md`, `docs/index.md` y `docs/handover.md`.
   - `docs/PUBLISHING.md` (URL de releases y tabla de infraestructura).
   - `src/*/README.md` y `templates/README.md`.
   - `docs/adr/0005-trusted-publishing.md`: **no reescribir el ADR**; añadir una
     nota de addendum con la fecha del rename y el nombre nuevo.
3. Ejecutar `dotnet tool run docfx docs/docfx.json` y comprobar 0 warnings.
4. Abrir el PR y dejarlo sin merge.

### Fase 1 — Renombrar el repositorio

1. GitHub → **Settings → General → Repository name** → `pipelinekit` → **Rename**.
2. Actualizar el remoto local:

   ```bash
   git remote set-url origin https://github.com/DanielCB096/pipelinekit.git
   ```

3. Comprobar la redirección del nombre antiguo:

   ```bash
   git ls-remote https://github.com/DanielCB096/dotnet-pack.git
   curl -sI https://github.com/DanielCB096/dotnet-pack | head -n 1
   ```

   Debe responder con redirección al repo nuevo.

### Fase 2 — Verificar Trusted Publishing

1. NuGet.org → cuenta `Daniel_CB` → **Trusted Publishing**.
2. Abrir la política `PipelineKit GitHub Actions`.
3. Comprobar que el **Repository ID** sigue siendo `#1470930886`. NuGet valida
   por ID inmutable, no por nombre; el rename no invalida la política.
4. El nombre mostrado puede seguir siendo `dotnet-pack` hasta que NuGet lo
   refresque; es cosmético y no afecta a la publicación.
5. No editar ni recrear la política salvo que el ID cambie.
6. Probar OIDC sin publicar una versión nueva:

   ```bash
   gh workflow run nuget-publish.yml --ref main
   ```

   La versión `0.8.0` ya existe; `--skip-duplicate` hará que los pushes sean
   duplicados, pero el login debe funcionar. Si el login falla, revisar que la
   política siga activa y con el ID correcto.

### Fase 3 — Republicar la documentación

1. Lanzar `docs` sobre `main`.
2. Verificar la URL nueva:

   ```bash
   curl -s -o /dev/null -w "%{http_code}\n" https://danielcb096.github.io/pipelinekit/
   ```

3. Comprobar una página interna, por ejemplo `handover.html` y `roadmap-1.0.html`.

### Fase 4 — Fusionar el PR de enlaces

1. Merge del PR preparado en la fase 0.
2. Esperar CI y `docs`.
3. Verificar que badges, README y páginas apuntan a `pipelinekit`.

### Fase 5 — Metadatos y comunicaciones

1. GitHub → **About**: descripción, homepage (nueva URL de Pages), topics.
2. Revisar webhooks, apps y protecciones de rama que referencien el nombre.
3. Avisar a consumidores:
   - El repo se ha movido.
   - Los paquetes y el namespace no cambian.
   - La URL de docs cambia (o se mantiene si hay dominio propio).
4. Publicar una versión de parche (`0.8.1`) o la `1.0.0` para que los metadatos
   `RepositoryUrl`/`PackageProjectUrl` de los paquetes apunten al repo nuevo.

## 7. Verificación final

- [ ] `https://github.com/DanielCB096/pipelinekit` responde 200.
- [ ] `https://github.com/DanielCB096/dotnet-pack` redirige al nuevo.
- [ ] `git ls-remote` funciona con la URL antigua y con la nueva.
- [ ] GitHub Pages nuevo responde 200 y el antiguo ya no se usa.
- [ ] `nuget-publish` autentica por OIDC y termina en verde con duplicados.
- [ ] Badges del README cargan.
- [ ] Release `v0.8.0` sigue accesible (redirigida).
- [ ] `dotnet tool run docfx docs/docfx.json` sin warnings.
- [ ] `dotnet build`, `dotnet test` y `dotnet format` en verde.
- [ ] No existe un repo nuevo llamado `dotnet-pack`.

## 8. Rollback

Si algo falla antes de comunicar el cambio:

1. GitHub → Settings → renombrar de vuelta a `dotnet-pack`.
2. Actualizar el remoto local a la URL original.
3. La política de NuGet no cambia; si se recreó, restaurar el ID/nombre anterior.
4. Relanzar `docs` y verificar la URL antigua.
5. Cerrar o revertir el PR de enlaces.

Si ya se comunicó o se publicó una versión con los metadatos nuevos, el rollback
es más caro: mantener el nombre nuevo, corregir los enlaces y publicar un parche.
GitHub mantiene la redirección del nombre anterior mientras no se reutilice.

## 9. Checklist resumida

- [ ] Estrategia de docs decidida.
- [ ] PR de enlaces preparado y validado con DocFX.
- [ ] Ventana anunciada.
- [ ] Repo renombrado y remoto actualizado.
- [ ] Política de NuGet verificada (ID sin cambios) y OIDC probado.
- [ ] Docs republicadas en la URL nueva.
- [ ] PR fusionado y CI en verde.
- [ ] Metadatos, topics y homepage actualizados.
- [ ] Consumidores avisados.
- [ ] Versión de parche publicada para refrescar metadatos.

## 10. Referencias

- [Handover](handover.md)
- [Roadmap 1.0](roadmap-1.0.md)
- [ADR 0005: Trusted Publishing](adr/0005-trusted-publishing.md)
- [Publicación](PUBLISHING.md)
