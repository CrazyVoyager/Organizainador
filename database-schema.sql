-- ============================================================================
-- DATABASE SCHEMA FOR ORGANIZAINADOR
-- PostgreSQL Database Schema
-- ============================================================================

-- Drop existing tables if they exist (use with caution in production)
-- DROP TABLE IF EXISTS tab_hor CASCADE;
-- DROP TABLE IF EXISTS tab_act CASCADE;
-- DROP TABLE IF EXISTS tab_clas CASCADE;
-- DROP TABLE IF EXISTS tab_usr CASCADE;
-- DROP FUNCTION IF EXISTS auth_validate_user(TEXT, TEXT);

-- ============================================================================
-- TABLA: tab_usr (Usuarios)
-- Descripción: Almacena la información de los usuarios del sistema
-- ============================================================================
CREATE TABLE IF NOT EXISTS tab_usr (
    tus_id_usr SERIAL PRIMARY KEY,
    tus_nom VARCHAR(100) NOT NULL,
    tus_mail VARCHAR(150) NOT NULL UNIQUE,
    tus_c_est VARCHAR(150),
    tus_est VARCHAR(150),
    tus_rol VARCHAR(50) NOT NULL DEFAULT 'Usuario',
    tus_cont VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índice para búsquedas por email
CREATE INDEX IF NOT EXISTS idx_usr_mail ON tab_usr(tus_mail);

-- ============================================================================
-- TABLA: tab_clas (Clases)
-- Descripción: Almacena las clases académicas de los usuarios
-- ============================================================================
CREATE TABLE IF NOT EXISTS tab_clas (
    tcl_id_clas SERIAL PRIMARY KEY,
    tus_id_usr INTEGER NOT NULL,
    tcl_nom_clas VARCHAR(100) NOT NULL,
    tcl_desc TEXT,
    tcl_cant_h_d INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_clas_usuario FOREIGN KEY (tus_id_usr) 
        REFERENCES tab_usr(tus_id_usr) ON DELETE CASCADE
);

-- Índice para búsquedas por usuario
CREATE INDEX IF NOT EXISTS idx_clas_usuario ON tab_clas(tus_id_usr);

-- ============================================================================
-- TABLA: tab_hor (Horarios)
-- Descripción: Almacena los horarios recurrentes de las clases
-- ============================================================================
CREATE TABLE IF NOT EXISTS tab_hor (
    tho_id_hor SERIAL PRIMARY KEY,
    tcl_id_clas INTEGER NOT NULL,
    tho_d_sem VARCHAR(20) NOT NULL,
    tho_h_ini TIME NOT NULL,
    tho_h_fin TIME NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_hor_clase FOREIGN KEY (tcl_id_clas) 
        REFERENCES tab_clas(tcl_id_clas) ON DELETE CASCADE,
    CONSTRAINT chk_dia_semana CHECK (
        tho_d_sem IN ('Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo')
    ),
    CONSTRAINT chk_hora_valida CHECK (tho_h_fin > tho_h_ini)
);

-- Índice para búsquedas por clase
CREATE INDEX IF NOT EXISTS idx_hor_clase ON tab_hor(tcl_id_clas);

-- ============================================================================
-- TABLA: tab_act (Actividades)
-- Descripción: Almacena las actividades puntuales de los usuarios
-- ============================================================================
CREATE TABLE IF NOT EXISTS tab_act (
    tac_id_act SERIAL PRIMARY KEY,
    tus_id_usr INTEGER NOT NULL,
    tac_nom_act VARCHAR(150) NOT NULL,
    tac_desc TEXT,
    tac_t_act VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_act_usuario FOREIGN KEY (tus_id_usr) 
        REFERENCES tab_usr(tus_id_usr) ON DELETE CASCADE
);

-- Índice para búsquedas por usuario
CREATE INDEX IF NOT EXISTS idx_act_usuario ON tab_act(tus_id_usr);

-- ============================================================================
-- FUNCIÓN: auth_validate_user
-- Descripción: Valida las credenciales del usuario y retorna información básica
-- Parámetros:
--   _email: Email del usuario
--   _password: Contraseña del usuario
-- Retorna: Registro con UserId, Email y Role si las credenciales son válidas
-- ============================================================================
CREATE OR REPLACE FUNCTION auth_validate_user(
    _email TEXT,
    _password TEXT
)
RETURNS TABLE (
    "UserId" TEXT,
    "Email" TEXT,
    "Role" TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.tus_id_usr::TEXT AS "UserId",
        u.tus_mail::TEXT AS "Email",
        u.tus_rol::TEXT AS "Role"
    FROM tab_usr u
    WHERE u.tus_mail = _email 
      AND u.tus_cont = _password;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- DATOS DE EJEMPLO (OPCIONAL)
-- Descomenta las siguientes líneas si deseas insertar datos de prueba
-- ============================================================================

-- Usuario de ejemplo
-- NOTA: En producción, las contraseñas deben estar hasheadas
-- INSERT INTO tab_usr (tus_nom, tus_mail, tus_c_est, tus_est, tus_rol, tus_cont)
-- VALUES 
--     ('Admin Usuario', 'admin@organizainador.com', 'Universidad Ejemplo', 'Ingeniería de Software', 'Administrador', '123'),
--     ('Usuario Test', 'test@test.com', 'Universidad Ejemplo', 'Ingeniería Informática', 'Usuario', '123');

-- Clase de ejemplo
-- INSERT INTO tab_clas (tus_id_usr, tcl_nom_clas, tcl_desc, tcl_cant_h_d)
-- VALUES 
--     (1, 'Matemáticas I', 'Curso de matemáticas básicas', 2),
--     (1, 'Programación', 'Curso de programación en C#', 3);

-- Horario de ejemplo
-- INSERT INTO tab_hor (tcl_id_clas, tho_d_sem, tho_h_ini, tho_h_fin)
-- VALUES 
--     (1, 'Lunes', '08:00:00', '10:00:00'),
--     (1, 'Miércoles', '08:00:00', '10:00:00'),
--     (2, 'Martes', '10:00:00', '13:00:00'),
--     (2, 'Jueves', '10:00:00', '13:00:00');

-- Actividad de ejemplo
-- INSERT INTO tab_act (tus_id_usr, tac_nom_act, tac_desc, tac_t_act)
-- VALUES 
--     (1, 'Entrega Proyecto Final', 'Entregar el proyecto final de programación', 'Tarea'),
--     (1, 'Reunión con el profesor', 'Reunión para revisar notas', 'Reunión');

-- ============================================================================
-- VERIFICACIÓN
-- ============================================================================

-- Verificar que las tablas se crearon correctamente
-- SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname = 'public';

-- Verificar que la función se creó correctamente
-- SELECT proname FROM pg_proc WHERE proname = 'auth_validate_user';

-- ============================================================================
-- NOTAS IMPORTANTES
-- ============================================================================

-- 1. SEGURIDAD: Este esquema almacena contraseñas en texto plano. 
--    Para producción, implementar hashing con bcrypt o argon2.
--    Ejemplo de función con hash:
--    WHERE u.tus_mail = _email AND u.tus_cont = crypt(_password, u.tus_cont)
--    Requiere la extensión pgcrypto: CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 2. RESPALDOS: Configurar respaldos automáticos de la base de datos.
--    pg_dump -U postgres -d BD_org -f backup_$(date +%Y%m%d).sql

-- 3. MANTENIMIENTO: Ejecutar VACUUM y ANALYZE periódicamente para optimización.
--    VACUUM ANALYZE tab_usr;
--    VACUUM ANALYZE tab_clas;
--    VACUUM ANALYZE tab_hor;
--    VACUUM ANALYZE tab_act;

-- 4. PERMISOS: Asignar permisos apropiados a los usuarios de la base de datos.
--    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO organizainador_user;
--    GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO organizainador_user;

-- ============================================================================
-- FIN DEL SCRIPT
-- ============================================================================
