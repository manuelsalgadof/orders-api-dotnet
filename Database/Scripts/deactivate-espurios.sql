-- =============================================================================
-- deactivate-espurios.sql
-- Motor:     SQL Server (Azure SQL)
-- Proposito: Desactivar usuarios Admin espurios creados antes del fix
--            ValidateRole (que retornaba Admin para null → ahora retorna Viewer)
-- Impacto:   UPDATE sobre dbo.Users — cambia Status a 'Inactive'
--            NO usa DELETE — preserva historial
-- Estado:    PLANTILLA — requiere aprobacion explicita antes de ejecutar
-- Aprobacion requerida: "APROBADO LIMPIEZA ADMIN AZURE SQL"
-- NO ejecutar contra BD real sin aprobacion explicita de Manuel
-- =============================================================================
-- Instrucciones de uso:
--   1. Ejecutar audit-admins.sql en Azure SQL (solo lectura).
--   2. Identificar IDs espurios (excluir admin seed legitimo).
--   3. Reemplazar el placeholder IN (...) con los IDs reales.
--   4. Obtener aprobacion explicita de Manuel.
--   5. Ejecutar hasta PASO 3 inclusive — revisar FilasAfectadas y listado.
--   6. Descomentar COMMIT TRANSACTION solo si el resultado es correcto.
--   7. En caso de duda: ejecutar ROLLBACK TRANSACTION.
-- =============================================================================

-- PASO 1: Verificar estado actual antes del cambio
SELECT
    Id,
    Name,
    LEFT(Email, 3) + '***@' + SUBSTRING(Email, CHARINDEX('@', Email) + 1, 100) AS EmailMasked,
    Role,
    Status,
    CreatedAt
FROM dbo.Users
WHERE Role = 'Admin'
ORDER BY CreatedAt ASC;

-- PASO 2: Desactivar admins espurios identificados en audit-admins.sql
--         NUNCA incluir el ID del admin seed legitimo
--         NUNCA usar DELETE — solo cambio de Status a 'Inactive'
BEGIN TRANSACTION;

UPDATE dbo.Users
SET
    Status    = 'Inactive',
    UpdatedAt = GETUTCDATE()
WHERE
    Id IN (/* REEMPLAZAR: IDs de admins espurios identificados en audit-admins.sql */)
    AND Role   = 'Admin'
    AND Status = 'Active';

-- PASO 3: Verificar resultado antes de confirmar
SELECT @@ROWCOUNT AS FilasAfectadas;

SELECT
    Id,
    Name,
    LEFT(Email, 3) + '***@' + SUBSTRING(Email, CHARINDEX('@', Email) + 1, 100) AS EmailMasked,
    Role,
    Status,
    UpdatedAt
FROM dbo.Users
WHERE Role = 'Admin'
ORDER BY CreatedAt ASC;

-- PASO 4: Confirmar solo si FilasAfectadas y listado son correctos
--         Descomentar COMMIT tras revision — no ejecutar a ciegas
-- COMMIT TRANSACTION;

-- Para revertir antes de COMMIT:
-- ROLLBACK TRANSACTION;
