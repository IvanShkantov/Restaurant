USE Restaurant;

----------------------------------

CREATE TABLE Orders (
    OrderID INT IDENTITY PRIMARY KEY,
    EmployeeID INT,

    Status NVARCHAR(20) NOT NULL DEFAULT 'Created',

    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ClosedAt DATETIME2 NULL,
    
    Price DECIMAL(10,2) DEFAULT 0,

    CONSTRAINT FK_Order_Employee
        FOREIGN KEY (EmployeeID) 
        REFERENCES Employees(EmployeeID)
        ON DELETE SET NULL
);



drop TABLE OrderItems

CREATE TABLE OrderItems (
    OrderID INT NOT NULL,
    DishID INT NOT NULL,
    
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,

    TotalPrice DECIMAL(10,2),
    
    CONSTRAINT PK_OrderItems
        PRIMARY KEY (OrderID, DishID),

    CONSTRAINT FK_Item_Order
        FOREIGN KEY (OrderID) 
        REFERENCES Orders(OrderID) 
        ON DELETE CASCADE,

    CONSTRAINT FK_Dish_Order
        FOREIGN KEY (DishID) 
        REFERENCES Dishes(DishID)
        ON DELETE CASCADE
);


GO

select * from Orders;


GO

CREATE FUNCTION dbo.GetOrderIdentifier (@OrderID INT)
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @Identifier NVARCHAR(200);
    
    SELECT @Identifier = CONCAT(
        dbo.GetEmployeeFullName(e.EmployeeID), 
        ' от ', 
        FORMAT(o.CreatedAt, 'dd.MM.yyyy HH:mm')
    )
    FROM Orders o
    INNER JOIN Employees e ON e.EmployeeID = o.EmployeeID
    WHERE o.OrderID = @OrderID;
    
    RETURN @Identifier;
END;

Select * from Orders

GO

CREATE OR ALTER VIEW vw_OrderDetails AS
SELECT 
    o.OrderID,
    o.CreatedAt,
    o.ClosedAt,
    o.Status,
    o.Price AS TotalOrderPrice,
    
    emp.EmployeeID,
    dbo.GetEmployeeFullName(emp.EmployeeID) AS EmployeeFullName,
    
    oi.DishID,
    d.Name AS DishName,
    d.Price AS DishBasePrice,
    oi.Quantity,
    oi.UnitPrice,
    oi.TotalPrice AS ItemTotalPrice
    
FROM Orders o
LEFT JOIN Employees emp ON o.EmployeeID = emp.EmployeeID
INNER JOIN OrderItems oi ON o.OrderID = oi.OrderID
LEFT JOIN Dishes d ON oi.DishID = d.DishID;
GO
-- Функция возвращает, сколько продуктов нужно для ВСЕХ активных заказов (без учёта нового)
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
    -- Считаем потребность для активных заказов
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

Select * from dbo.GetRequiredProductsForActiveOrders()