# Organizainador

Una aplicación web ASP.NET Core para la organización y gestión de actividades académicas.

## Características

- **Gestión de Clases**: Crea y administra tus clases con horarios recurrentes
- **Gestión de Actividades**: Registra actividades y tareas pendientes
- **Calendario Interactivo**: Visualiza todos tus eventos en un calendario integrado con FullCalendar
- **Horarios Flexibles**: Soporta eventos recurrentes y de fecha única
- **Widget de Clima**: Información del clima integrada en la interfaz
- **Autenticación de Usuarios**: Sistema de login seguro con cookies

## Tecnologías

- ASP.NET Core 9.0
- Entity Framework Core con PostgreSQL
- Razor Pages y MVC
- Bootstrap 5
- FullCalendar.js
- Dapper para consultas específicas

## Requisitos

- .NET 9.0 SDK
- PostgreSQL

## Configuración

1. Configura la cadena de conexión en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

2. Ejecuta las migraciones de la base de datos

3. Inicia la aplicación:
```bash
dotnet run
```

## Estructura del Proyecto

- `Controllers/` - Controladores MVC para Actividades, Clases, Horarios y Usuarios
- `Models/` - Modelos de datos (ActividadModel, ClaseModel, HorarioModel, UsuarioModel)
- `Pages/` - Razor Pages (Login, PaginaPrincipal, Calendario)
- `Views/` - Vistas MVC
- `Services/` - Servicios de negocio (UserService para autenticación)
- `Data/` - Contexto de base de datos (AppDbContext)
- `wwwroot/` - Archivos estáticos (CSS, JavaScript)