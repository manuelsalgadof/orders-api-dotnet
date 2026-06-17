# OrderOps — Plataforma B2B de Gestión de Pedidos

API REST cloud-ready desarrollada en .NET 9 para digitalizar la gestión de pedidos operacionales, reemplazando flujos manuales (Excel, correos, WhatsApp) por un sistema seguro, trazable e integrable.

## Narrativa del proyecto

OrderOps es una mini plataforma operacional B2B orientada a digitalizar pedidos, trazabilidad e integración para empresas chilenas — especialmente pymes, logística, proveedores y operaciones internas.

## Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | .NET 9 / ASP.NET Core |
| ORM | EF Core 9 (Database First) + Dapper |
| Base de datos | SQL Server (Azure SQL Server) |
| Autenticación | JWT Bearer |
| Hashing | PBKDF2 versionado |
| Contenedores | Docker (multi-stage) |
| CI/CD | GitHub Actions → Docker Hub → Azure Container Apps |
| Frontend | Angular 19 (proyecto separado: orders-front/) |

## Arquitectura

N-Tier limpia: Controller → Service → Repository → DbContext

```
Controllers/     → Routing, auth, status codes HTTP
Services/        → Lógica de negocio
Repositories/    → Acceso a datos (EF Core + Dapper)
Interfaces/      → Contratos entre capas
DTOs/            → Entrada/salida desacoplada
Entities/        → Modelos Database First
BackgroundJobs/  → Procesamiento batch asíncrono
Middlewares/     → Pipeline HTTP (CorrelationId, Logging)
```

## Endpoints

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | /api/Auth/Generartoken | No | Login → JWT |
| POST | /api/Orders | Sí | Crear pedido |
| GET | /api/Orders | Sí | Listar pedidos (paginado) |
| GET | /api/Orders/{id} | Sí | Detalle de pedido + items + historial |
| GET | /api/Orders/export | Sí | Exportar CSV |
| POST | /api/Jobs/reprocess-orders | Sí | Lanzar reprocesamiento batch |
| GET | /api/Jobs/{id} | Sí | Estado del job |
| POST | /api/Users | Admin | Crear usuario |
| GET | /api/Users | Admin | Listar usuarios |
| GET | /api/Users/{id} | Admin | Detalle usuario |
| PUT | /api/Users/{id} | Admin | Actualizar usuario |
| DELETE | /api/Users/{id} | Admin | Eliminar usuario |
| GET | /health | No | Health check |
| GET | / | No | Swagger UI |

Roles disponibles: `Admin`, `Operator`, `Viewer`

## Seguridad

- JWT con validación completa (issuer, audience, lifetime, signing key)
- Hashing PBKDF2 versionado (algoritmo + factor de trabajo + salt + hash)
- Rate limiting: 60 req/min por IP
- CORS por configuración — sin wildcard
- Jwt:Key fail-fast en Production si no está configurado
- Preflight CI/CD valida todos los secrets antes de desplegar
- Admin seed desde configuración — sin credenciales hardcodeadas

## Ejecución local

### Con dotnet

```bash
cd OrdersApi
dotnet restore
dotnet run --project OrdersApi/OrdersApi.csproj
```

Swagger en: `https://localhost:7212` o `http://localhost:5199`

### Con Docker

```bash
docker build -t orders-api -f OrdersApi/Dockerfile .
docker run -p 8080:8080 orders-api
```

### Con Docker Compose (backend + frontend)

```bash
# Desde D:\Proyectos Personales\orders-front\
docker compose up --build
```

## Variables de entorno requeridas

| Variable | Descripción |
|---|---|
| ConnectionStrings__DefaultConnection | Connection string SQL Server |
| Jwt__Key | Clave secreta JWT (mín. 32 chars) |
| Jwt__Issuer | Issuer del token (ej: OrdersApi) |
| Jwt__Audience | Audience del token (ej: OrdersApiUsers) |
| AdminSeed__Name | Nombre del admin inicial |
| AdminSeed__Email | Email del admin inicial |
| AdminSeed__Password | Contraseña del admin inicial |

Local: usar `dotnet user-secrets` o `appsettings.Development.json` (NO versionar con secretos reales).

## Tests

```bash
dotnet test OrdersApi.Tests/OrdersApi.Tests.csproj
```

## CI/CD

Push a `main` → build + test → Docker Hub → Azure Container Apps

Pipeline en `.github/workflows/dotnet-ci.yml`

## Base de datos

Scripts en `bd/` — fuente de verdad del schema:
1. `tablas.sql` — DDL principal
2. `Users.sql` — tabla Users
3. `roles-migration.sql` — ampliar roles (Admin, Operator, Viewer)
4. `order-status-history.sql` — tabla historial + SP ProcessOrders actualizado
5. `CREATE PROCEDURE ProcessOrders.sql` — SP original (reemplazado por script 4)
6. `insercion de prueba.sql` — seed básico

**IMPORTANTE**: Ejecutar scripts en Azure SQL requiere aprobación explícita.
