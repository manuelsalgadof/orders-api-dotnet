-- =============================================================================
-- roles-migration.sql
-- Fase 3: Ampliar roles de usuarios
-- Motor:   SQL Server (Azure SQL) -- T-SQL
-- Objeto:  dbo.Users -- constraint CK_Users_Role
-- Proposito:
--   Reemplazar CK_Users_Role (solo 'Admin') por CK_Users_Role_V2
--   que acepta 'Admin', 'Operator', 'Viewer'.
-- Idempotente: si se ejecuta dos veces no falla.
-- Sin transaccion explicita -- cada sentencia DDL es atomica en SQL Server.
-- Sin datos reales.
-- Aprobacion requerida para ejecutar en Azure SQL.
-- =============================================================================

-- PRECONDICION:
--   Todos los registros existentes en dbo.Users tienen Role = 'Admin'
--   (unica opcion que permitia CK_Users_Role original).
--   La nueva constraint CK_Users_Role_V2 acepta 'Admin', por lo que
--   no hay riesgo de violacion de constraint sobre datos existentes.

-- POSTCONDICION:
--   CK_Users_Role eliminada.
--   CK_Users_Role_V2 activa con roles: Admin, Operator, Viewer.

-- ROLLBACK MANUAL (si es necesario revertir):
--   ALTER TABLE dbo.Users DROP CONSTRAINT CK_Users_Role_V2;
--   ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Role
--       CHECK (Role IN ('Admin'));

SET NOCOUNT ON;

-- Paso 1: Drop CK_Users_Role si existe
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name            = 'CK_Users_Role'
      AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users DROP CONSTRAINT CK_Users_Role;
    PRINT 'CK_Users_Role eliminada.';
END
ELSE
BEGIN
    PRINT 'CK_Users_Role no existe -- nada que eliminar.';
END;

-- Paso 2: Crear CK_Users_Role_V2 si no existe
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name            = 'CK_Users_Role_V2'
      AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
        ADD CONSTRAINT CK_Users_Role_V2
        CHECK (Role IN ('Admin', 'Operator', 'Viewer'));
    PRINT 'CK_Users_Role_V2 creada. Roles permitidos: Admin, Operator, Viewer.';
END
ELSE
BEGIN
    PRINT 'CK_Users_Role_V2 ya existe -- ninguna accion requerida.';
END;

PRINT 'roles-migration.sql completado.';
