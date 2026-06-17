-- =============================================================================
-- order-status-history.sql
-- Fase 4: Trazabilidad de estados de pedidos
-- Motor:   SQL Server (Azure SQL) -- T-SQL
-- Objetos:
--   1. dbo.OrderStatusHistory  -- tabla nueva
--   2. IX_OrderStatusHistory_OrderId_ChangedAt  -- indice no cluster
--   3. dbo.ProcessOrders  -- SP actualizado (CREATE OR ALTER)
-- Proposito:
--   Registrar el historial de cambios de estado de cada pedido.
--   El SP ProcessOrders se actualiza para insertar en OrderStatusHistory
--   por cada Order que cambia de Pending a Processed.
-- Idempotente: IF NOT EXISTS en tabla e indice. CREATE OR ALTER en SP.
-- Sin transaccion explicita -- operaciones atomicas individuales.
-- Sin datos reales.
-- Aprobacion requerida para ejecutar en Azure SQL.
-- Orden obligatorio de ejecucion:
--   1. Crear tabla OrderStatusHistory
--   2. Crear indice
--   3. Crear/actualizar SP ProcessOrders
-- =============================================================================

-- CONTRATO DE LA TABLA OrderStatusHistory:
--   Columna        Tipo              Nullable  Descripcion
--   Id             INT IDENTITY      NO        PK autoincremental
--   OrderId        INT               NO        FK -> Orders(Id)
--   FromStatus     NVARCHAR(30)      SI        Estado anterior (NULL en estado inicial)
--   ToStatus       NVARCHAR(30)      NO        Estado nuevo
--   ChangedAt      DATETIME2         NO        UTC -- DEFAULT GETUTCDATE()
--   ChangedBy      NVARCHAR(256)     SI        Email o identificador del usuario/proceso
--   Source         NVARCHAR(50)      NO        Origen: 'System', 'API', 'Job'

-- CONTRATO DEL SP ProcessOrders (actualizado):
--   Entrada:       ninguna
--   Salida:        SELECT con una fila y una columna:
--                    AffectedRows INT -- cantidad de Orders cambiados de Pending a Processed
--   Efecto:        UPDATE Orders SET Status='Processed' WHERE Status='Pending'
--                  INSERT en OrderStatusHistory por cada Order procesado
--                    FromStatus = 'Pending', ToStatus = 'Processed',
--                    Source = 'Job', ChangedBy = NULL, ChangedAt = GETUTCDATE()
--   Compatibilidad hacia atras:
--                  El result set es identico al SP original (AffectedRows INT).
--                  JobRepository.ProcessOrdersAsync no requiere modificacion.

SET NOCOUNT ON;

-- =============================================================================
-- Paso 1: Crear tabla OrderStatusHistory si no existe
-- =============================================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.OrderStatusHistory')
      AND type      = 'U'
)
BEGIN
    CREATE TABLE dbo.OrderStatusHistory (
        Id         INT           IDENTITY(1,1) NOT NULL,
        OrderId    INT                         NOT NULL,
        FromStatus NVARCHAR(30)                NULL,
        ToStatus   NVARCHAR(30)                NOT NULL,
        ChangedAt  DATETIME2                   NOT NULL CONSTRAINT DF_OrderStatusHistory_ChangedAt DEFAULT GETUTCDATE(),
        ChangedBy  NVARCHAR(256)               NULL,
        Source     NVARCHAR(50)                NOT NULL CONSTRAINT DF_OrderStatusHistory_Source    DEFAULT 'System',

        CONSTRAINT PK_OrderStatusHistory
            PRIMARY KEY CLUSTERED (Id),

        CONSTRAINT FK_OrderStatusHistory_Orders
            FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id),

        CONSTRAINT CK_OrderStatusHistory_ToStatus
            CHECK (LEN(LTRIM(RTRIM(ToStatus))) > 0),

        CONSTRAINT CK_OrderStatusHistory_Source
            CHECK (Source IN ('System', 'API', 'Job'))
    );
    PRINT 'Tabla dbo.OrderStatusHistory creada.';
END
ELSE
BEGIN
    PRINT 'Tabla dbo.OrderStatusHistory ya existe -- ninguna accion requerida.';
END;

-- =============================================================================
-- Paso 2: Crear indice no cluster en (OrderId, ChangedAt) si no existe
-- Motivo: queries de historial de un pedido filtran por OrderId y ordenan
--         por ChangedAt. Sin indice, habria full scan sobre la tabla.
-- Impacto en escrituras: leve overhead por INSERT en cada cambio de estado.
-- Impacto en storage: menor -- columnas INT + DATETIME2.
-- =============================================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.OrderStatusHistory')
      AND name      = 'IX_OrderStatusHistory_OrderId_ChangedAt'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderStatusHistory_OrderId_ChangedAt
        ON dbo.OrderStatusHistory (OrderId ASC, ChangedAt ASC);
    PRINT 'Indice IX_OrderStatusHistory_OrderId_ChangedAt creado.';
END
ELSE
BEGIN
    PRINT 'Indice IX_OrderStatusHistory_OrderId_ChangedAt ya existe -- ninguna accion requerida.';
END;

-- =============================================================================
-- Paso 3: Actualizar SP ProcessOrders
-- CREATE OR ALTER es idempotente: crea si no existe, reemplaza si existe.
-- Compatibilidad: mismo result set que el SP original (AffectedRows INT).
-- Efecto adicional: INSERT en OrderStatusHistory por cada Order procesado.
-- Patron: OUTPUT ... INTO tabla variable para capturar IDs sin segundo SELECT.
-- Sin NOLOCK -- el UPDATE requiere locks exclusivos de todas formas.
-- =============================================================================
GO
CREATE OR ALTER PROCEDURE dbo.ProcessOrders
AS
BEGIN
    SET NOCOUNT ON;

    -- Tabla variable para capturar los IDs de Orders actualizados.
    -- Las table variables sobreviven al ROLLBACK, pero aquí se usan solo
    -- para pasar datos del UPDATE al INSERT dentro de la misma transacción.
    DECLARE @ProcessedOrders TABLE (
        OrderId INT NOT NULL
    );

    DECLARE @AffectedRows INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Actualizar Orders de Pending a Processed.
        -- OUTPUT captura los IDs de cada fila modificada sin segundo SELECT.
        UPDATE dbo.Orders
        SET    Status = 'Processed'
        OUTPUT INSERTED.Id INTO @ProcessedOrders (OrderId)
        WHERE  Status = 'Pending';

        SET @AffectedRows = @@ROWCOUNT;

        -- Insertar un registro de historial por cada Order procesado.
        -- Source='Job' identifica que el cambio fue producido por el job batch.
        -- ChangedBy=NULL porque el SP no recibe contexto de usuario autenticado.
        -- Si no hay filas en @ProcessedOrders, el INSERT no escribe nada.
        INSERT INTO dbo.OrderStatusHistory (
            OrderId,
            FromStatus,
            ToStatus,
            ChangedAt,
            ChangedBy,
            Source
        )
        SELECT
            po.OrderId,
            'Pending',
            'Processed',
            GETUTCDATE(),
            NULL,
            'Job'
        FROM @ProcessedOrders AS po;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    -- Retornar conteo de filas afectadas.
    -- Mismo contrato que el SP original: una fila, columna AffectedRows INT.
    SELECT @AffectedRows AS AffectedRows;
END;

PRINT 'SP dbo.ProcessOrders actualizado.';
PRINT 'order-status-history.sql completado.';
