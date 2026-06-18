-- =============================================================================
-- audit-admins.sql
-- Motor:     SQL Server (Azure SQL)
-- Proposito: Identificar usuarios Admin activos para limpieza pre-demo
--            Creados antes del fix ValidateRole (retornaba Admin para null;
--            ahora retorna Viewer por defecto)
-- Impacto:   SOLO LECTURA — no modifica datos
-- Aprobacion requerida: lectura — no requiere aprobacion destructiva
-- NO ejecutar contra BD real sin aprobacion de Manuel
-- =============================================================================

-- Conteo total de admins activos
SELECT COUNT(*) AS TotalAdminsActivos
FROM dbo.Users
WHERE Role = 'Admin'
  AND Status = 'Active';

-- Listado completo de admins (activos e inactivos) con email enmascarado
-- Email masking: primeros 3 caracteres + *** + dominio
SELECT
    Id,
    Name,
    LEFT(Email, 3) + '***@' + SUBSTRING(Email, CHARINDEX('@', Email) + 1, 100) AS EmailMasked,
    Role,
    Status,
    CreatedAt,
    UpdatedAt
FROM dbo.Users
WHERE Role = 'Admin'
ORDER BY CreatedAt ASC;
