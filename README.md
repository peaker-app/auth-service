# auth-service

Registro, inicio de sesión, confirmación de correo y emisión de JWT. MySQL 8.4
(`peaker_auth`). Ver `.claude/docs/DESIGN.md` §4.

## Puesta en marcha local

La cadena de conexión **no se versiona** (`ARCHITECTURE.md` §11, regla innegociable 8):
`appsettings.json` la deja vacía. Dentro de Docker la aporta
`services-deployment/config/auth-service.env` mediante `ConnectionStrings__AuthDatabase`.

Fuera de Docker se carga esa misma variable desde el `.env`, sin copiar la contraseña a ningún
sitio. Sirve tanto para `dotnet run` como para `dotnet ef`:

```powershell
cd ..\services-deployment
. .\scripts\Use-DevDatabase.ps1 auth       # bash: source ./scripts/use-dev-database.sh auth
```

`dotnet user-secrets` es una alternativa válida para `dotnet run`, pero **`dotnet ef` la ignora**:
`AuthDbContextFactory` tiene prioridad sobre el host de la API y solo lee la variable de entorno.

El esquema no se aplica en el arranque (`ARCHITECTURE.md` §12). Fuera de Docker:

```bash
dotnet ef database update \
  --project AuthService/AuthService.Infrastructure \
  --startup-project AuthService/AuthService.API
```
