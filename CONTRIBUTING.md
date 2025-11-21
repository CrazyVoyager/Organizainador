# Guía de Contribución

¡Gracias por tu interés en contribuir a Organizainador! Este documento proporciona las pautas para contribuir al proyecto.

## 🚀 Cómo Empezar

1. **Fork el Repositorio:** Haz clic en el botón "Fork" en la página del repositorio
2. **Clona tu Fork:**
   ```bash
   git clone https://github.com/tu-usuario/Organizainador.git
   cd Organizainador
   ```
3. **Configura el Upstream:**
   ```bash
   git remote add upstream https://github.com/CrazyVoyager/Organizainador.git
   ```
4. **Crea una Rama:**
   ```bash
   git checkout -b feature/nombre-de-tu-feature
   ```

## 📝 Proceso de Contribución

### 1. Antes de Empezar
- Revisa los [Issues](https://github.com/CrazyVoyager/Organizainador/issues) existentes
- Si tu cambio es grande, abre un Issue para discutirlo primero
- Asegúrate de que no haya un PR abierto para el mismo cambio

### 2. Desarrollando tu Contribución

#### Estilo de Código
- Sigue las convenciones de C# y .NET
- Usa nombres descriptivos para variables y métodos
- Añade comentarios cuando sea necesario para código complejo
- Mantén los métodos pequeños y con una sola responsabilidad

#### Commits
- Escribe mensajes de commit claros y descriptivos
- Usa el formato: `tipo: descripción breve`
  - `feat:` nueva característica
  - `fix:` corrección de bug
  - `docs:` cambios en documentación
  - `style:` cambios de formato/estilo
  - `refactor:` refactorización de código
  - `test:` añadir o modificar tests
  - `chore:` tareas de mantenimiento

Ejemplo:
```
feat: añadir exportación de calendario a PDF
fix: corregir error en cálculo de horarios
docs: actualizar README con instrucciones de instalación
```

### 3. Testing
- Asegúrate de que el proyecto compila sin errores
  ```bash
  dotnet build
  ```
- Prueba tu cambio manualmente
- Si añades nueva funcionalidad, considera añadir tests

### 4. Crear el Pull Request

1. **Actualiza tu Rama:**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Push tus Cambios:**
   ```bash
   git push origin feature/nombre-de-tu-feature
   ```

3. **Abre el Pull Request:**
   - Ve a tu fork en GitHub
   - Haz clic en "New Pull Request"
   - Completa la plantilla del PR con:
     - Descripción clara de los cambios
     - Referencias a Issues relacionados
     - Capturas de pantalla si hay cambios visuales

## 🐛 Reportar Bugs

Al reportar un bug, incluye:
- Descripción clara del problema
- Pasos para reproducir el bug
- Comportamiento esperado vs. comportamiento actual
- Versión de .NET, PostgreSQL y sistema operativo
- Capturas de pantalla si es relevante

## 💡 Sugerir Mejoras

Para sugerir nuevas características:
- Verifica que no exista ya un Issue similar
- Describe claramente el problema que resuelve
- Proporciona ejemplos de uso
- Considera el impacto en usuarios existentes

## 📋 Áreas donde Contribuir

### Funcionalidades Prioritarias
- [ ] Sistema de notificaciones/recordatorios
- [ ] Exportación de calendario (PDF, iCal)
- [ ] Tema oscuro
- [ ] Aplicación móvil
- [ ] Integración con Google Calendar
- [ ] Sistema de etiquetas mejorado
- [ ] Estadísticas de tiempo de estudio

### Mejoras de Seguridad
- [ ] Implementar hashing de contraseñas (bcrypt)
- [ ] Autenticación de dos factores
- [ ] Rate limiting en login
- [ ] Validación CSRF mejorada

### Mejoras Técnicas
- [ ] Añadir tests unitarios
- [ ] Añadir tests de integración
- [ ] Implementar migraciones de EF Core
- [ ] Mejorar manejo de errores
- [ ] Añadir logging estructurado
- [ ] Implementar cache

### Documentación
- [ ] Video tutoriales
- [ ] Documentación de API
- [ ] Guía de despliegue
- [ ] Traducciones

## 🔍 Revisión de Código

Los mantenedores revisarán tu PR considerando:
- Calidad del código
- Adherencia a las convenciones del proyecto
- Completitud de la documentación
- Impacto en el rendimiento
- Compatibilidad con versiones anteriores

## ⚖️ Código de Conducta

- Sé respetuoso con otros contribuidores
- Acepta críticas constructivas
- Enfócate en lo que es mejor para el proyecto
- Muestra empatía hacia otros miembros de la comunidad

## 🎯 Primeras Contribuciones

Si es tu primera contribución, busca Issues etiquetados con:
- `good first issue`
- `help wanted`
- `documentation`

## 📞 ¿Necesitas Ayuda?

- Abre un Issue con la etiqueta `question`
- Revisa la documentación en el README
- Contacta a los mantenedores

## 📄 Licencia

Al contribuir, aceptas que tus contribuciones se licencien bajo la misma licencia del proyecto (MIT).

---

¡Gracias por contribuir a Organizainador! 🎉
