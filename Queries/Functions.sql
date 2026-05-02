GO

-- Функция возвращает, сколько продуктов нужно для активных заказов
CREATE OR ALTER FUNCTION dbo.GetRequiredProductsForActiveOrders ()
RETURNS @Required TABLE
(
    ProductID INT,
    ProductName NVARCHAR(100),
    Unit NVARCHAR(20),
    RequiredQuantity DECIMAL(10,2),
    AvailableQuantity DECIMAL(10,2),
    Shortage DECIMAL(10,2)
)
AS
BEGIN
    WITH ActiveOrdersRequired AS (
        SELECT 
            dp.ProductID,
            SUM(dp.Quantity * oi.Quantity) AS TotalRequired
        FROM OrderItems oi
        INNER JOIN Orders o ON oi.OrderID = o.OrderID
        INNER JOIN DishProducts dp ON oi.DishID = dp.DishID
        WHERE o.Status IN ('Created', 'Processing', 'Ready')
        GROUP BY dp.ProductID
    )
    INSERT INTO @Required
    SELECT 
        aor.ProductID,
        pr.Name AS ProductName,
        pr.Unit,
        ISNULL(aor.TotalRequired, 0) AS RequiredQuantity,
        ISNULL(w.Quantity, 0) AS AvailableQuantity,
        CASE 
            WHEN ISNULL(aor.TotalRequired, 0) > ISNULL(w.Quantity, 0)
            THEN ISNULL(aor.TotalRequired, 0) - ISNULL(w.Quantity, 0)
            ELSE 0 
        END AS Shortage
    FROM ActiveOrdersRequired aor
    INNER JOIN Products pr ON aor.ProductID = pr.ProductID
    LEFT JOIN Warehouse w ON aor.ProductID = w.ProductID;
    
    RETURN;
END;

GO

-- Функция для получения выручки за период
CREATE OR ALTER FUNCTION dbo.GetRevenue (
    @StartDate DATE, 
    @EndDate DATE, 
    @EmployeeID INT = NULL
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Total DECIMAL(18,2);
    
    SELECT @Total = ISNULL(SUM(TotalPrice), 0)
    FROM ArchivedOrders
    WHERE ClosedAt >= @StartDate
      AND ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@EmployeeID IS NULL OR EmployeeID = @EmployeeID);
    
    RETURN @Total;
END;

GO

-- Функция для получения расходов за период
CREATE OR ALTER FUNCTION dbo.GetExpenses (
    @StartDate DATE, 
    @EndDate DATE, 
    @SupplierID INT = NULL
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Total DECIMAL(18,2);
    
    SELECT @Total = ISNULL(SUM(TotalPrice), 0)
    FROM ArchivedPurchases
    WHERE ClosedAt >= @StartDate
      AND ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@SupplierID IS NULL OR SupplierID = @SupplierID);
    
    RETURN @Total;
END;

GO

-- Функция для получения выручки по месяцам
CREATE OR ALTER FUNCTION dbo.GetRevenueByMonth (
    @StartDate DATE, 
    @EndDate DATE, 
    @EmployeeID INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        DATEFROMPARTS(YEAR(ClosedAt), MONTH(ClosedAt), 1) AS PeriodDate,
        SUM(TotalPrice) AS TotalAmount,
        COUNT(*) AS OrderCount
    FROM ArchivedOrders
    WHERE ClosedAt >= @StartDate
      AND ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@EmployeeID IS NULL OR EmployeeID = @EmployeeID)
    GROUP BY DATEFROMPARTS(YEAR(ClosedAt), MONTH(ClosedAt), 1)
);

GO

-- Функция для получения расходов по месяцам
CREATE OR ALTER FUNCTION dbo.GetExpensesByMonth (
    @StartDate DATE, 
    @EndDate DATE, 
    @SupplierID INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        DATEFROMPARTS(YEAR(ClosedAt), MONTH(ClosedAt), 1) AS PeriodDate,
        SUM(TotalPrice) AS TotalAmount,
        COUNT(*) AS PurchaseCount
    FROM ArchivedPurchases
    WHERE ClosedAt >= @StartDate
      AND ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@SupplierID IS NULL OR SupplierID = @SupplierID)
    GROUP BY DATEFROMPARTS(YEAR(ClosedAt), MONTH(ClosedAt), 1)
);

GO

-- Функция для получения прибыли с группировкой
CREATE OR ALTER FUNCTION dbo.GetProfitByPeriod (
    @StartDate DATE, 
    @EndDate DATE, 
    @EmployeeID INT = NULL,
    @SupplierID INT = NULL
)
RETURNS @Result TABLE
(
    PeriodDate DATE,
    Revenue DECIMAL(18,2),
    Expenses DECIMAL(18,2),
    Profit DECIMAL(18,2),
    OrderCount INT,
    PurchaseCount INT
)
AS
BEGIN
        INSERT INTO @Result
        SELECT 
            ISNULL(r.PeriodDate, e.PeriodDate) AS PeriodDate,
            ISNULL(r.TotalAmount, 0) AS Revenue,
            ISNULL(e.TotalAmount, 0) AS Expenses,
            ISNULL(r.TotalAmount, 0) - ISNULL(e.TotalAmount, 0) AS Profit,
            ISNULL(r.OrderCount, 0) AS OrderCount,
            ISNULL(e.PurchaseCount, 0) AS PurchaseCount
        FROM dbo.GetRevenueByMonth(@StartDate, @EndDate, @EmployeeID) r
        FULL OUTER JOIN dbo.GetExpensesByMonth(@StartDate, @EndDate, @SupplierID) e
            ON r.PeriodDate = e.PeriodDate;
    
    
    RETURN;
END;

GO

-- Функция для получения объединенного списка заказов и закупок
CREATE OR ALTER FUNCTION dbo.GetOperationsList (
    @StartDate DATE, 
    @EndDate DATE,
    @ReportType NVARCHAR(20), 
    @EmployeeID INT = NULL,
    @SupplierID INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        'Заказ' AS OperationType,
        ao.OrderID AS OperationID,
        ao.ClosedAt AS OperationDate,
        ao.EmployeeFullName AS EmployeeName,
        NULL AS SupplierName,
        ao.TotalPrice AS Amount,
        'Completed' AS Status
    FROM ArchivedOrders ao
    WHERE (@ReportType IN ('Revenue', 'Profit'))
      AND ao.ClosedAt >= @StartDate
      AND ao.ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@EmployeeID IS NULL OR ao.EmployeeID = @EmployeeID)
    
    UNION ALL
    
    SELECT 
        'Закупка' AS OperationType,
        ap.PurchaseID AS OperationID,
        ap.ClosedAt AS OperationDate,
        ap.EmployeeFullName AS EmployeeName,
        ap.SupplierName AS SupplierName,
        ap.TotalPrice AS Amount,
        'Delivered' AS Status
    FROM ArchivedPurchases ap
    WHERE (@ReportType IN ('Expenses', 'Profit'))
      AND ap.ClosedAt >= @StartDate
      AND ap.ClosedAt < DATEADD(DAY, 1, @EndDate)
      AND (@SupplierID IS NULL OR ap.SupplierID = @SupplierID)
);

GO