
GO

-- Пересчёт стоимости позиции заказа и общей стоимости всего заказа
CREATE OR ALTER TRIGGER trg_OrderItems_MaintainOrderTotal
ON OrderItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        UPDATE oi
        SET 
            oi.UnitPrice = d.Price,
            oi.TotalPrice = oi.Quantity * d.Price
        FROM OrderItems oi
        INNER JOIN inserted i ON i.OrderID = oi.OrderID AND i.DishID = oi.DishID
        INNER JOIN Dishes d ON d.DishID = oi.DishID;
    END
    
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
       AND UPDATE(Quantity)
    BEGIN
        UPDATE oi
        SET oi.TotalPrice = oi.Quantity * oi.UnitPrice
        FROM OrderItems oi
        INNER JOIN inserted i ON i.OrderID = oi.OrderID AND i.DishID = oi.DishID;
    END
    
    UPDATE o
    SET o.Price = ISNULL((
        SELECT SUM(TotalPrice)
        FROM OrderItems oi
        WHERE oi.OrderID = o.OrderID
    ), 0)
    FROM Orders o
    WHERE o.OrderID IN (
        SELECT OrderID FROM inserted
        UNION
        SELECT OrderID FROM deleted
    );
END;

GO

-- Пересчёт стоимости позиции закупки и общей стоимости всей закупки
CREATE OR ALTER TRIGGER trg_PurchaseItems_MaintainOrderTotal
ON PurchaseItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        UPDATE item
        SET 
            item.UnitPrice = p.Price,
            item.TotalPrice = item.Quantity * p.Price
        FROM PurchaseItems item
        INNER JOIN inserted i ON i.PurchaseID = item.PurchaseID AND i.PriceID = item.PriceID
        INNER JOIN ProductPrices p ON p.PriceID = item.PriceID;
    END
    
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
       AND UPDATE(Quantity)
    BEGIN
        UPDATE item
        SET item.TotalPrice = item.Quantity * item.UnitPrice
        FROM PurchaseItems item
        INNER JOIN inserted i ON i.PurchaseID = item.PurchaseID AND i.PriceID = item.PriceID;
    END
    
    UPDATE p
    SET p.Price = ISNULL((
        SELECT SUM(TotalPrice)
        FROM PurchaseItems item
        WHERE item.PurchaseID = p.PurchaseID
    ), 0)
    FROM Purchases p
    WHERE p.PurchaseID IN (
        SELECT PurchaseID FROM inserted
        UNION
        SELECT PurchaseID FROM deleted
    );
END;

GO

-- Архивация закупки при доставке
CREATE OR ALTER TRIGGER trg_Purchase_ArchiveOnDelivery
ON Purchases
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(PurchaseStatus)
    BEGIN
        DECLARE @ArchiveID INT;
        DECLARE @CurrentPurchaseID INT;

        SELECT TOP 1 @CurrentPurchaseID = i.PurchaseID
        FROM inserted i
        INNER JOIN deleted d ON i.PurchaseID = d.PurchaseID
        WHERE i.PurchaseStatus = 'Delivered'
          AND ISNULL(d.PurchaseStatus, '') != 'Delivered'
          AND NOT EXISTS (
              SELECT 1 FROM ArchivedPurchases ap 
              WHERE ap.PurchaseID = i.PurchaseID
          );

        IF @CurrentPurchaseID IS NOT NULL
        BEGIN
            EXEC sp_UpdateWarehouseOnDelivery @PurchaseID = @CurrentPurchaseID;
        END

        INSERT INTO ArchivedPurchases (
            PurchaseID,
            CreatedAt,
            ClosedAt,
            EmployeeID,
            EmployeeFullName,
            SupplierID,
            SupplierName,
            TotalPrice
        )
        SELECT 
            i.PurchaseID,
            i.CreatedAt,
            GETDATE(),
            i.EmployeeID,                                      
            e.LName + ' ' + e.FName + ' ' + ISNULL(e.MName, ''),
            i.SupplierID,                                      
            s.Name,                                            
            i.Price
        FROM inserted i
        INNER JOIN deleted d ON i.PurchaseID = d.PurchaseID
        INNER JOIN Employees e ON i.EmployeeID = e.EmployeeID
        INNER JOIN Suppliers s ON i.SupplierID = s.SupplierID
        WHERE i.PurchaseStatus = 'Delivered'
          AND ISNULL(d.PurchaseStatus, '') != 'Delivered'
          AND NOT EXISTS (
              SELECT 1 FROM ArchivedPurchases ap 
              WHERE ap.PurchaseID = i.PurchaseID
          );
        
        SET @ArchiveID = SCOPE_IDENTITY();

        IF @ArchiveID IS NOT NULL
        BEGIN
            INSERT INTO ArchivedPurchaseItems (
                ArchiveID, ProductName, Quantity, UnitPrice, TotalPrice
            )
            SELECT 
                @ArchiveID,
                pr.Name,
                pi.Quantity,
                pi.UnitPrice,
                pi.TotalPrice
            FROM PurchaseItems pi
            INNER JOIN Products pr ON pi.ProductID = pr.ProductID
            INNER JOIN inserted i ON pi.PurchaseID = i.PurchaseID
            WHERE i.PurchaseStatus = 'Delivered';
        END

    END
END;

GO

-- Архивация заказа при завершении
CREATE OR ALTER TRIGGER trg_Order_ArchiveOnCompletion
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(Status)
    BEGIN
        DECLARE @ArchiveID INT;
        DECLARE @CurrentOrderID INT;

        SELECT TOP 1 @CurrentOrderID = i.OrderID
        FROM inserted i
        INNER JOIN deleted d ON i.OrderID = d.OrderID
        WHERE i.Status = 'Completed'
          AND ISNULL(d.Status, '') != 'Completed'
          AND NOT EXISTS (
              SELECT 1 FROM ArchivedOrders ao 
              WHERE ao.OrderID = i.OrderID
          );

        IF @CurrentOrderID IS NOT NULL
        BEGIN
            EXEC sp_UpdateWarehouseOnOrderCompletion @OrderID = @CurrentOrderID;         
        END
        
        INSERT INTO ArchivedOrders (
            OrderID,
            CreatedAt,
            ClosedAt,
            EmployeeID,
            EmployeeFullName,
            TotalPrice
        )
        SELECT 
            i.OrderID,
            i.CreatedAt,
            GETDATE(),
            i.EmployeeID,                                      
            e.LName + ' ' + e.FName + ' ' + ISNULL(e.MName, ''), 
            i.Price
        FROM inserted i
        INNER JOIN deleted d ON i.OrderID = d.OrderID
        INNER JOIN Employees e ON i.EmployeeID = e.EmployeeID
        WHERE i.Status = 'Completed'
          AND ISNULL(d.Status, '') != 'Completed'
          AND NOT EXISTS (
              SELECT 1 FROM ArchivedOrders ao 
              WHERE ao.OrderID = i.OrderID
          );
        
        SET @ArchiveID = SCOPE_IDENTITY();
        
        IF @ArchiveID IS NOT NULL
        BEGIN
            INSERT INTO ArchivedOrderItems (
                ArchiveID, DishName, Quantity, UnitPrice, TotalPrice
            )
            SELECT 
                @ArchiveID,
                d.Name,
                oi.Quantity,
                oi.UnitPrice,
                oi.TotalPrice
            FROM OrderItems oi
            INNER JOIN Dishes d ON oi.DishID = d.DishID
            INNER JOIN inserted i ON oi.OrderID = i.OrderID
            WHERE i.Status = 'Completed';
        END
    END
END;

GO

-- Сохраниение полного имени сторудника
CREATE OR ALTER TRIGGER trg_ActivityLog_SaveEmployeeSnapshot
ON ActivityLog
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE al
    SET al.EmployeeNameSnapshot = dbo.GetEmployeeFullName(e.EmployeeID)
    FROM ActivityLog al
    INNER JOIN inserted i ON al.LogID = i.LogID
    INNER JOIN Employees e ON al.EmployeeID = e.EmployeeID;
END;

GO

CREATE OR ALTER TRIGGER trg_Product_CreateWarehouse
ON Products
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Warehouse (ProductID, Quantity)
    SELECT 
        i.ProductID,
        0
    FROM inserted i
    WHERE NOT EXISTS (
        SELECT 1 FROM Warehouse w 
        WHERE w.ProductID = i.ProductID
    );
END;
GO