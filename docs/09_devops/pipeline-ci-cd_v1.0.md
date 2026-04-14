 # Pipeline CI/CD

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** pipeline-ci-cd_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Definir la configuración del pipeline de Integración Continua y Entrega Continua (CI/CD) del proyecto GeoFoto, utilizando GitHub Actions como plataforma de automatización.

---

## 2. Plataforma

| Aspecto | Valor |
|---------|-------|
| Plataforma CI/CD | GitHub Actions |
| Runner | ubuntu-latest |
| Archivo de configuración | `.github/workflows/ci.yml` |
| Triggers | push a `main`, push a `develop`, pull_request a `main` |

---

## 3. Diagrama del pipeline

```text
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ Checkout │───►│  Build   │───►│  Test    │───►│ Build    │───►│ Upload   │
│  código  │    │ solución │    │ + Cover  │    │ APK      │    │ Artifacts│
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
     │               │               │               │               │
     │          dotnet build    dotnet test     dotnet publish   upload-artifact
     │          --config        --collect:     -f net10.0-      APK + test
     │          Release         XPlat Code     android          report
     │                          Coverage
```

---

## 4. Workflow completo

```yaml
name: GeoFoto CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '10.0.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore GeoFoto.sln

      - name: Build solution
        run: dotnet build GeoFoto.sln --configuration Release --no-restore

      - name: Run tests with coverage
        run: |
          dotnet test GeoFoto.sln \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=test-results.trx" \
            --collect:"XPlat Code Coverage" \
            --results-directory ./TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: ./TestResults/**/*.trx

      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: coverage-report
          path: ./TestResults/**/coverage.cobertura.xml

  build-apk:
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main'

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install MAUI workload
        run: dotnet workload install maui-android

      - name: Restore dependencies
        run: dotnet restore GeoFoto.Mobile/GeoFoto.Mobile.csproj

      - name: Build APK Android Debug
        run: |
          dotnet publish GeoFoto.Mobile/GeoFoto.Mobile.csproj \
            -f net10.0-android \
            -c Debug \
            -o ./artifacts/apk

      - name: Upload APK artifact
        uses: actions/upload-artifact@v4
        with:
          name: geofoto-apk-debug
          path: ./artifacts/apk/**/*.apk
```

---

## 5. Jobs del pipeline

### 5.1 Job: build-and-test

| Step | Acción | Descripción |
|------|--------|-------------|
| 1 | Checkout | Clona el repositorio |
| 2 | Setup .NET | Instala .NET 10 SDK |
| 3 | Restore | Restaura paquetes NuGet |
| 4 | Build | Compila la solución en modo Release |
| 5 | Test | Ejecuta tests xUnit con cobertura XPlat |
| 6 | Upload tests | Sube reporte .trx como artifact |
| 7 | Upload coverage | Sube reporte cobertura como artifact |

**Triggers:** Todos los pushes y pull requests.

### 5.2 Job: build-apk

| Step | Acción | Descripción |
|------|--------|-------------|
| 1 | Checkout | Clona el repositorio |
| 2 | Setup .NET | Instala .NET 10 SDK |
| 3 | Install workload | Instala maui-android workload |
| 4 | Restore | Restaura paquetes del proyecto Mobile |
| 5 | Build APK | Publica APK Android Debug |
| 6 | Upload APK | Sube APK como artifact descargable |

**Triggers:** Solo push a `main` (depende de build-and-test exitoso).

---

## 6. Artifacts generados

| Artifact | Contenido | Retención |
|----------|----------|-----------|
| test-results | Archivo .trx con resultados de tests | 30 días |
| coverage-report | Archivo coverage.cobertura.xml | 30 días |
| geofoto-apk-debug | APK Android Debug | 90 días |

---

## 7. Branch protection rules

Para la rama `main`:

| Regla | Valor |
|-------|-------|
| Require pull request reviews | 1 aprobación mínima |
| Require status checks | build-and-test debe pasar |
| Require branches up to date | Sí |
| Require linear history | No (se permite merge commit) |
| Include administrators | Sí |

---

## 8. Secretos y variables

| Variable | Tipo | Descripción |
|----------|------|-------------|
| DOTNET_VERSION | env (workflow) | Versión de .NET SDK |

> **Nota:** En v1.0 no se requieren secretos adicionales. No hay deploy a servidores externos ni signing de APK.

---

## 9. Calidad gates

| Gate | Criterio | Acción si falla |
|------|----------|----------------|
| Build | Compilación sin errores | Bloquea pipeline |
| Tests | 100% tests pasando | Bloquea merge a main |
| Cobertura | ≥ 70% global | Warning (no bloquea en v1.0) |

---

## 10. Evolución futura (v2.0+)

| Mejora | Descripción |
|--------|-------------|
| APK firmado (Release) | Signing con keystore para distribución |
| Deploy API a Azure | Publicación automática en Azure App Service |
| Deploy Web a Azure Static | Publicación de Blazor Web |
| SonarQube | Análisis estático de código |
| Dependabot | Actualización automática de dependencias |

---

## 11. Trazabilidad

| Documento | Referencia |
|-----------|-----------|
| Estrategia de Testing | estrategia-testing-motor_v1.0.md |
| Definition of Done | definition-of-done_v1.0.md |
| Acuerdo de equipo | acuerdo-equipo_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
