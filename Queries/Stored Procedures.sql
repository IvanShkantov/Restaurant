
GO
-- Логинация польлзователя
CREATE OR ALTER PROCEDURE sp_LoginUser
    @Login NVARCHAR(50),
    @PasswordHash NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        e.EmployeeID,
        e.LName,
        e.FName,
        e.MName,
        p.PositionID,
        p.PositionName
    FROM Employees e
    JOIN Positions p ON e.PositionID = p.PositionID
    WHERE 
        e.Login = @Login
        AND e.PasswordHash = @PasswordHash
        AND e.IsActivated = 1;
END;

GO
-- Регистрация польлзователя
CREATE OR ALTER PROCEDURE sp_RegisterUser
    @LName NVARCHAR(50),
    @FName NVARCHAR(50),
    @MName NVARCHAR(50) = NULL,
    @Login NVARCHAR(50),
    @InviteCodeHash NVARCHAR(200),
    @PasswordHash NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @EmployeeID INT;
    
    SELECT @EmployeeID = EmployeeID
    FROM Employees
    WHERE LName = @LName
      AND FName = @FName
      AND (MName = @MName OR (MName IS NULL AND @MName IS NULL))
      AND PasswordHash = @InviteCodeHash
      AND IsActivated = 0
      AND Login IS NULL;
    
    IF @EmployeeID IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM Employees WHERE Login = @Login AND EmployeeID != @EmployeeID)
        BEGIN
            SELECT 
                'LoginExists' AS Result, 
                'Логин уже занят' AS Message,
                CAST(NULL AS INT) AS EmployeeID,
                CAST(NULL AS NVARCHAR(50)) AS LName,
                CAST(NULL AS NVARCHAR(50)) AS FName,
                CAST(NULL AS NVARCHAR(50)) AS MName,
                CAST(NULL AS INT) AS PositionID,
                CAST(NULL AS NVARCHAR(50)) AS PositionName;
            RETURN;
        END
        
        UPDATE Employees
        SET Login = @Login, PasswordHash = @PasswordHash, IsActivated = 1
        WHERE EmployeeID = @EmployeeID;
        
        SELECT 
            'Success' AS Result,
            'Регистрация успешна' AS Message,
            e.EmployeeID,
            e.LName,
            e.FName,
            e.MName,
            e.PositionID,
            p.PositionName
        FROM Employees e
        JOIN Positions p ON e.PositionID = p.PositionID
        WHERE e.EmployeeID = @EmployeeID;
    END
    ELSE
    BEGIN
        IF EXISTS (
            SELECT 1 FROM Employees
            WHERE LName = @LName AND FName = @FName
              AND (MName = @MName OR (MName IS NULL AND @MName IS NULL))
              AND IsActivated = 1
        )
        BEGIN
            SELECT 
                'AlreadyActivated' AS Result, 
                'Сотрудник уже зарегистрирован' AS Message,
                CAST(NULL AS INT) AS EmployeeID,
                CAST(NULL AS NVARCHAR(50)) AS LName,
                CAST(NULL AS NVARCHAR(50)) AS FName,
                CAST(NULL AS NVARCHAR(50)) AS MName,
                CAST(NULL AS INT) AS PositionID,
                CAST(NULL AS NVARCHAR(50)) AS PositionName;
        END
        ELSE
        BEGIN
            SELECT 
                'InvalidCode' AS Result, 
                'Неверные данные или пригласительный код' AS Message,
                CAST(NULL AS INT) AS EmployeeID,
                CAST(NULL AS NVARCHAR(50)) AS LName,
                CAST(NULL AS NVARCHAR(50)) AS FName,
                CAST(NULL AS NVARCHAR(50)) AS MName,
                CAST(NULL AS INT) AS PositionID,
                CAST(NULL AS NVARCHAR(50)) AS PositionName;
        END
    END
END;

GO

-- Добавление на склад при завершении закупки
CREATE PROCEDURE sp_UpdateWarehouseOnDelivery
    @PurchaseID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    MERGE Warehouse AS target
    USING (
        SELECT 
            pi.ProductID,
            SUM(pi.Quantity) AS DeliveredQuantity
        FROM PurchaseItems pi
        WHERE pi.PurchaseID = @PurchaseID
        GROUP BY pi.ProductID
    ) AS source ON (target.ProductID = source.ProductID)
    WHEN MATCHED THEN
        UPDATE SET 
            target.Quantity = target.Quantity + source.DeliveredQuantity
    WHEN NOT MATCHED THEN
        INSERT (ProductID, Quantity)
        VALUES (source.ProductID, source.DeliveredQuantity);
END;

GO
-- Списывание со склада при завершении заказа
CREATE OR ALTER PROCEDURE sp_UpdateWarehouseOnOrderCompletion
    @OrderID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE w
    SET w.Quantity = w.Quantity - source.RequiredQuantity
    FROM Warehouse w
    INNER JOIN (
        SELECT 
            dp.ProductID,
            SUM(dp.Quantity * oi.Quantity) AS RequiredQuantity
        FROM OrderItems oi
        INNER JOIN DishProducts dp ON oi.DishID = dp.DishID
        WHERE oi.OrderID = @OrderID
        GROUP BY dp.ProductID
    ) source ON w.ProductID = source.ProductID;
    
END;

GO