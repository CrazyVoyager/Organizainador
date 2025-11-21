# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

## [Unreleased]

### Added (2024-11-21)
- ✨ Documentación completa del proyecto en README.md
- ✨ Script de esquema de base de datos (`database-schema.sql`)
- ✨ Archivo de ejemplo para variables de entorno (`.env.example`)
- ✨ Guía de contribución (`CONTRIBUTING.md`)
- ✨ Este archivo de changelog

### Fixed (2024-11-21)
- 🐛 Corregidas todas las advertencias de referencias nulas (21 warnings)
  - Modelos: `UsuarioModel`, `ActividadModel`, `ClaseModel`, `HorarioModel`
  - Páginas: `CalendarioModel`, `PaginaPrincipalModel`, `AppEvent`
- 🐛 Corregida advertencia MVC1001 de `ValidateAntiForgeryToken` en Login.cshtml.cs
- 🐛 Corregidas advertencias CS8602 de posibles referencias nulas en Calendario.cshtml.cs
- 🐛 Corregida referencia nula en Views/Horarios/Details.cshtml
- 🔧 Agregado `PrivateAssets="all"` a paquete de diseño para evitar inclusión en publicación

### Changed (2024-11-21)
- 🔧 Actualizado .gitignore para excluir archivos .env
- 🔧 Removidos directorios bin/ y obj/ del control de versiones
- 📝 README mejorado con instrucciones detalladas de instalación y configuración

### Security (2024-11-21)
- ✅ Análisis de seguridad CodeQL ejecutado: 0 vulnerabilidades encontradas
- 📝 Documentadas recomendaciones de seguridad para producción en README

## [Estado del Proyecto]

### ✅ Funcionalidades Implementadas
- Sistema de autenticación con cookies
- Gestión de usuarios con roles
- Gestión de clases académicas
- Gestión de horarios recurrentes
- Gestión de actividades
- Calendario interactivo con FullCalendar
- Integración con PostgreSQL usando EF Core y Dapper

### 📋 Build Status
- **Compilación:** ✅ 0 Errores, 0 Advertencias
- **Seguridad:** ✅ 0 Vulnerabilidades (CodeQL)
- **Framework:** .NET 9.0
- **Base de Datos:** PostgreSQL 12+

### 🔮 Mejoras Futuras Recomendadas
- [ ] Implementar hashing de contraseñas (bcrypt/argon2)
- [ ] Agregar tests unitarios
- [ ] Agregar tests de integración
- [ ] Implementar sistema de notificaciones
- [ ] Exportación de calendario (PDF, iCal)
- [ ] Tema oscuro
- [ ] Aplicación móvil
- [ ] Integración con Google Calendar
- [ ] Autenticación de dos factores
- [ ] Rate limiting en login
- [ ] Migraciones de Entity Framework (opcional)

## Formato

Este archivo sigue los principios de [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

### Tipos de Cambios
- `Added` para nuevas funcionalidades
- `Changed` para cambios en funcionalidades existentes
- `Deprecated` para funcionalidades que serán removidas en versiones futuras
- `Removed` para funcionalidades removidas
- `Fixed` para corrección de bugs
- `Security` para cambios relacionados con seguridad
