-- ============================================================
-- Склад деталей + Планировщик производства
-- Выполните этот скрипт вручную в ProductionDB
-- ============================================================

-- 1. Склад готовых деталей (остатки)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DetailStocks')
BEGIN
    CREATE TABLE DetailStocks (
        DetailStockID   INT IDENTITY(1,1) PRIMARY KEY,
        DetailID        INT NOT NULL UNIQUE REFERENCES Details(DetailID),
        CurrentQuantity INT NOT NULL DEFAULT 0,
        ReceivedQuantity INT NOT NULL DEFAULT 0,
        ShippedQuantity INT NOT NULL DEFAULT 0,
        LastUpdated     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 2. Журнал движения деталей на складе
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DetailTransactions')
BEGIN
    CREATE TABLE DetailTransactions (
        DetailTransactionID INT IDENTITY(1,1) PRIMARY KEY,
        DetailID            INT NOT NULL REFERENCES Details(DetailID),
        Quantity            INT NOT NULL,
        TransactionType     NVARCHAR(50) NOT NULL,  -- Receipt, Shipment, Production, Adjustment
        TransactionDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description         NVARCHAR(500) NULL,
        DocumentNumber      INT NULL
    );
    CREATE INDEX IX_DetailTransactions_DetailID ON DetailTransactions(DetailID);
    CREATE INDEX IX_DetailTransactions_Date ON DetailTransactions(TransactionDate);
END
GO

-- 3. Месячный план выпуска (отгрузки)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MonthlyProductionPlans')
BEGIN
    CREATE TABLE MonthlyProductionPlans (
        PlanID    INT IDENTITY(1,1) PRIMARY KEY,
        [Year]    INT NOT NULL,
        [Month]   INT NOT NULL,
        Notes     NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_MonthlyProductionPlans_YearMonth UNIQUE ([Year], [Month])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MonthlyProductionPlanItems')
BEGIN
    CREATE TABLE MonthlyProductionPlanItems (
        PlanItemID    INT IDENTITY(1,1) PRIMARY KEY,
        PlanID        INT NOT NULL REFERENCES MonthlyProductionPlans(PlanID) ON DELETE CASCADE,
        DetailID      INT NOT NULL REFERENCES Details(DetailID),
        Quantity      INT NOT NULL,
        ShipmentDate  DATE NOT NULL,
        Notes         NVARCHAR(300) NULL
    );
    CREATE INDEX IX_MonthlyProductionPlanItems_PlanID ON MonthlyProductionPlanItems(PlanID);
    CREATE INDEX IX_MonthlyProductionPlanItems_ShipmentDate ON MonthlyProductionPlanItems(ShipmentDate);
END
GO

-- 4. Сгенерированный план по сменам (результат работы планировщика)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GeneratedProductionPlans')
BEGIN
    CREATE TABLE GeneratedProductionPlans (
        GeneratedPlanID INT IDENTITY(1,1) PRIMARY KEY,
        [Year]          INT NOT NULL,
        [Month]         INT NOT NULL,
        GeneratedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Notes           NVARCHAR(500) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GeneratedProductionPlanItems')
BEGIN
    CREATE TABLE GeneratedProductionPlanItems (
        ItemID           INT IDENTITY(1,1) PRIMARY KEY,
        GeneratedPlanID  INT NOT NULL REFERENCES GeneratedProductionPlans(GeneratedPlanID) ON DELETE CASCADE,
        WorkDate         DATE NOT NULL,
        ShiftCode        NVARCHAR(20) NOT NULL,   -- '1я', '2я'
        EquipmentID      INT NOT NULL REFERENCES Equipment(EquipmentID),
        DetailID         INT NOT NULL REFERENCES Details(DetailID),
        PlannedQuantity  INT NOT NULL,
        IsOverdue        BIT NOT NULL DEFAULT 0,
        Notes            NVARCHAR(300) NULL
    );
    CREATE INDEX IX_GeneratedProductionPlanItems_PlanID ON GeneratedProductionPlanItems(GeneratedPlanID);
    CREATE INDEX IX_GeneratedProductionPlanItems_WorkDate ON GeneratedProductionPlanItems(WorkDate, ShiftCode);
END
GO

-- 5. Норма выработки за смену (для расчёта мощности планировщика)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('DetailOperations') AND name = 'NormPerShift'
)
BEGIN
    ALTER TABLE DetailOperations ADD NormPerShift INT NULL;
END
GO

-- Пример: UPDATE DetailOperations SET NormPerShift = 50 WHERE DetailOperationID = 1;

-- 6. Статус сгенерированного плана (Draft / Confirmed)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('GeneratedProductionPlans') AND name = 'Status'
)
BEGIN
    ALTER TABLE GeneratedProductionPlans ADD Status NVARCHAR(30) NOT NULL DEFAULT 'Draft';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('GeneratedProductionPlans') AND name = 'ConfirmedAt'
)
BEGIN
    ALTER TABLE GeneratedProductionPlans ADD ConfirmedAt DATETIME2 NULL;
END
GO
