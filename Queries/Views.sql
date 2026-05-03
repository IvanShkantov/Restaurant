GO

CREATE OR ALTER VIEW vw_PurchaseDetails AS
SELECT 
    p.PurchaseID,
    p.CreatedAt,
    p.ClosedAt,
    p.PurchaseStatus,
    p.Price AS TotalPurchasePrice,
    
    emp.EmployeeID,
    dbo.GetEmployeeFullName(emp.EmployeeID) AS EmployeeFullName,
    
    sup.SupplierID,
    sup.Name AS SupplierName,
    
    pi.ProductID,
    prod.Name AS ProductName,
    pi.Quantity,
    pi.UnitPrice,
    pi.TotalPrice AS ItemTotalPrice
    
FROM Purchases p
LEFT JOIN Employees emp ON p.EmployeeID = emp.EmployeeID
LEFT JOIN Suppliers sup ON p.SupplierID = sup.SupplierID
INNER JOIN PurchaseItems pi ON p.PurchaseID = pi.PurchaseID
INNER JOIN Products prod ON pi.ProductID = prod.ProductID;

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

CREATE OR ALTER VIEW vw_AllPermissions AS
SELECT 
    pos.PositionID,
    pos.PositionName,
    per.PermissionID,
    per.PermissionName,
    CAST(CASE 
        WHEN pp.PositionID IS NOT NULL 
        THEN 1 
        ELSE 0 
    END AS BIT) AS HasPermission
FROM Positions pos
CROSS JOIN Permissions per
LEFT JOIN PositionPermissions pp 
    ON pp.PositionID = pos.PositionID 
    AND pp.PermissionID = per.PermissionID