# Organizainador

## 📋 Descripción
Organizainador es una aplicación web ASP.NET Core MVC que permite a los usuarios gestionar sus actividades académicas, clases y horarios en un calendario interactivo. El sistema incluye autenticación de usuarios y una interfaz intuitiva para organizar el tiempo de estudio.

## ✨ Características
- 🔐 Sistema de autenticación con cookies
- 📅 Calendario interactivo para visualizar actividades y clases
- 📚 Gestión de clases con horarios recurrentes
- ✅ Gestión de actividades personales
- 👥 Gestión de usuarios con roles
- 🎨 Interfaz moderna con Bootstrap

## 🛠️ Tecnologías Utilizadas
- **Backend:** ASP.NET Core 9.0 (MVC + Razor Pages)
- **Base de datos:** PostgreSQL
- **ORM:** Entity Framework Core + Dapper
- **Frontend:** Bootstrap 5, JavaScript, FullCalendar
- **Autenticación:** Cookie-based Authentication

## 📋 Requisitos Previos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- Editor de código (Visual Studio 2022, VS Code, Rider, etc.)

## ⚙️ Configuración e Instalación

### 1. Clonar el Repositorio
```bash
git clone https://github.com/CrazyVoyager/Organizainador.git
cd Organizainador
```

### 2. Configurar la Base de Datos

#### Crear la Base de Datos
```bash
# Conectarse a PostgreSQL
psql -U postgres

# Crear la base de datos
CREATE DATABASE BD_org;
```

#### Ejecutar el Script de Esquema
Ejecuta el archivo `database-schema.sql` que contiene todas las tablas y funciones necesarias:

```bash
psql -U postgres -d BD_org -f database-schema.sql
```

### 3. Configurar la Cadena de Conexión

Edita el archivo `appsettings.json` o `appsettings.Development.json` con tus credenciales de PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BD_org;Username=tu_usuario;Password=tu_contraseña"
  }
}
```

**⚠️ Importante:** Nunca subas archivos con contraseñas reales al repositorio. Usa variables de entorno para producción.

### 4. Restaurar Dependencias
```bash
dotnet restore
```

### 5. Compilar el Proyecto
```bash
dotnet build
```

### 6. Ejecutar la Aplicación
```bash
dotnet run
```

La aplicación estará disponible en:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## 📁 Estructura del Proyecto

```
Organizainador/
├── Controllers/          # Controladores MVC
│   ├── ActividadesController.cs
│   ├── ClasesController.cs
│   ├── HorariosController.cs
│   └── UsuariosController.cs
├── Models/              # Modelos de datos
│   ├── ActividadModel.cs
│   ├── ClaseModel.cs
│   ├── HorarioModel.cs
│   └── UsuarioModel.cs
├── Views/               # Vistas MVC
├── Pages/               # Razor Pages
│   ├── Login.cshtml
│   ├── Calendario.cshtml
│   └── PaginaPrincipal.cshtml
├── Services/            # Servicios de lógica de negocio
│   └── UserService.cs
├── Data/                # Contexto de base de datos
│   └── AppDbContext.cs
├── wwwroot/            # Archivos estáticos (CSS, JS, imágenes)
└── Program.cs          # Configuración de la aplicación

```

## 🗃️ Esquema de Base de Datos

### Tablas Principales

#### `tab_usr` - Usuarios
Almacena información de los usuarios del sistema.
- `tus_id_usr`: ID del usuario (PK)
- `tus_nom`: Nombre del usuario
- `tus_mail`: Email (único)
- `tus_c_est`: Casa de estudios
- `tus_est`: Especialidad/Carrera
- `tus_rol`: Rol del usuario
- `tus_cont`: Contraseña (hasheada en producción recomendado)

#### `tab_clas` - Clases
Información de las clases académicas.
- `tcl_id_clas`: ID de la clase (PK)
- `tus_id_usr`: ID del usuario (FK)
- `tcl_nom_clas`: Nombre de la clase
- `tcl_desc`: Descripción
- `tcl_cant_h_d`: Cantidad de horas por día

#### `tab_hor` - Horarios
Horarios recurrentes de las clases.
- `tho_id_hor`: ID del horario (PK)
- `tcl_id_clas`: ID de la clase (FK)
- `tho_d_sem`: Día de la semana
- `tho_h_ini`: Hora de inicio
- `tho_h_fin`: Hora de fin

#### `tab_act` - Actividades
Actividades puntuales de los usuarios.
- `tac_id_act`: ID de la actividad (PK)
- `tus_id_usr`: ID del usuario (FK)
- `tac_nom_act`: Nombre de la actividad
- `tac_desc`: Descripción
- `tac_t_act`: Tipo/Etiqueta
- `created_at`: Fecha de creación

## 🔑 Funcionalidades Principales

### Autenticación
- Login con email y contraseña
- Autenticación basada en cookies
- Función stored procedure `auth_validate_user` para validación segura

### Gestión de Clases
- Crear, editar y eliminar clases
- Asignar horarios recurrentes (días de la semana)
- Visualización en el calendario

### Gestión de Actividades
- Crear actividades con fecha y hora específicas
- Categorización con etiquetas
- Visualización en el calendario

### Calendario Interactivo
- Vista mensual de todas las clases y actividades
- Colores diferenciados (azul para clases, verde para actividades)
- Información detallada al hacer clic en los eventos

## 🚀 Uso de la Aplicación

1. **Registro/Login:** Accede a la página de login e ingresa tus credenciales
2. **Página Principal:** Visualiza tu panel principal con opciones de navegación
3. **Calendario:** Ve todas tus clases y actividades en un calendario interactivo
4. **Gestión de Clases:** Crea y gestiona tus clases académicas con horarios recurrentes
5. **Gestión de Actividades:** Añade actividades puntuales o tareas por hacer

## 🧪 Desarrollo y Testing

### Ejecutar en Modo Desarrollo
```bash
dotnet run --environment Development
```

### Compilar para Producción
```bash
dotnet publish -c Release -o ./publish
```

## 🔒 Seguridad

⚠️ **Consideraciones de Seguridad para Producción:**

1. **Contraseñas:** Actualmente las contraseñas se almacenan en texto plano. Para producción, implementar hashing con BCrypt o similar.
2. **Cadenas de Conexión:** Usar Azure Key Vault, AWS Secrets Manager o variables de entorno.
3. **HTTPS:** Asegurarse de que HTTPS esté habilitado en producción.
4. **Validación CSRF:** Configurar validación anti-forgery para todos los formularios.
5. **SQL Injection:** El proyecto usa Dapper y EF Core con parámetros, lo cual previene SQL injection.

## 🐛 Solución de Problemas

### Error de Conexión a PostgreSQL
- Verifica que PostgreSQL está ejecutándose: `sudo service postgresql status`
- Verifica las credenciales en `appsettings.json`
- Verifica que la base de datos `BD_org` existe

### Error "Function auth_validate_user does not exist"
- Asegúrate de haber ejecutado el script `database-schema.sql`
- Verifica que la función existe: `\df auth_validate_user` en psql

### Errores de Build
```bash
# Limpiar y reconstruir
dotnet clean
dotnet restore
dotnet build
```

## 📝 Notas Adicionales

### Node.js y package.json
El archivo `package.json` y `index.js` son archivos legacy de pruebas con PostgreSQL. No son necesarios para ejecutar la aplicación ASP.NET Core actual.

### Migraciones de Entity Framework
El proyecto no usa migraciones de EF Core. El esquema de base de datos se gestiona directamente mediante scripts SQL.

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/NuevaCaracteristica`)
3. Commit tus cambios (`git commit -m 'Agregar nueva característica'`)
4. Push a la rama (`git push origin feature/NuevaCaracteristica`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto es de código abierto y está disponible bajo la licencia MIT.

## 👥 Autor

- **CrazyVoyager** - [GitHub](https://github.com/CrazyVoyager)

## 📮 Contacto

Si tienes preguntas o sugerencias, por favor abre un issue en GitHub.