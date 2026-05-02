
USE Restaurant;

-- DROP TABLE Suppliers;

-- Поставщики
CREATE TABLE Suppliers (
    SupplierID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) UNIQUE,
    Email NVARCHAR(100) UNIQUE
);

INSERT INTO Suppliers (Name, Phone, Email)
VALUES
(N'АмиФрут', N'+375296689813', N'amifruit@mail.ru'),
(N'Велес-Мит', N'+375176546262', N'sales@veles-meat.by'),
(N'Поставский молочный завод', N'+375215545877', N'post_milk@mail.ru'),
(N'МИНБАКАЛЕЯТОРГ', N'+375291174446', N'minbak@minbak.by');

Select * from Suppliers

----------------------------------

--DROP TABLE Purchases;

-- Закупки
CREATE TABLE Purchases (
    PurchaseID INT IDENTITY PRIMARY KEY,

    SupplierID INT,
    EmployeeID INT,
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ClosedAt DATETIME2 NULL,

    PurchaseStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    
    Price DECIMAL(10,2) DEFAULT 0,

    CONSTRAINT FK_Purchase_Supplier
        FOREIGN KEY (SupplierID) 
        REFERENCES Suppliers(SupplierID)
        ON DELETE SET NULL,

    CONSTRAINT FK_Purchase_Employee
        FOREIGN KEY (EmployeeID) 
        REFERENCES Employees(EmployeeID)
        ON DELETE SET NULL
);


INSERT INTO Purchases (SupplierID, EmployeeID)
VALUES
(1, 6),
(2, 6),
(3, 6),
(4, 6);

Select * from Purchases;
Select * from PurchaseItems;

update Purchases set PurchaseStatus = 'Pending'

----------------------------------

--DROP TABLE PurchaseItems;

-- Позиции закуок
CREATE TABLE PurchaseItems (
    PurchaseID INT NOT NULL,
    ProductID INT NOT NULL,
    PriceID INT, 

    UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
    Quantity DECIMAL(10,2) NOT NULL CHECK (Quantity > 0),
    
    TotalPrice DECIMAL(10,2),

    CONSTRAINT PK_PurchaseItems
        PRIMARY KEY (PurchaseID, ProductID),

    CONSTRAINT FK_Item_Purchase
        FOREIGN KEY (PurchaseID) 
        REFERENCES Purchases(PurchaseID) 
        ON DELETE CASCADE,

    CONSTRAINT FK_Item_Product
        FOREIGN KEY (ProductID) 
        REFERENCES Products(ProductID)
        ON DELETE CASCADE,

    CONSTRAINT FK_Item_Price
        FOREIGN KEY (PriceID) 
        REFERENCES ProductPrices(PriceID)
        ON DELETE SET NULL,
);


delete from PurchaseItems
delete from Purchases
INSERT INTO PurchaseItems (PurchaseID, ProductID, Quantity, PriceID)
VALUES
-- Овощи
(1, 1, 50, 1),
(1, 2, 20, 2),

-- Мясо
(7, 7, 30, 3),
(7, 8, 25, 4),

-- Молочка
(3, 10, 40, 5),
(3, 13, 20, 6),

-- Бакалея
(9, 14, 50, 7),
(10, 17, 30, 8);

Select * from Purchases
Select * from PurchaseItems;
Select * from Products

----------------------------------

CREATE TABLE ProductPrices (
    PriceID INT IDENTITY PRIMARY KEY,
    SupplierID INT NOT NULL,
    ProductID INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_Price_Supplier 
        FOREIGN KEY (SupplierID) 
        REFERENCES Suppliers(SupplierID)
        ON DELETE CASCADE,

    CONSTRAINT FK_Price_Product 
        FOREIGN KEY (ProductID) 
        REFERENCES Products(ProductID)
        ON DELETE SET NULL
)

INSERT INTO ProductPrices (SupplierID, ProductID, Price)
VALUES
-- Овощи
(1, 1, 0.5),
(1, 2, 0.4),

-- Мясо
(2, 7, 5.0),
(2, 8, 6.0),

-- Молочка
(3, 10, 1.2),
(3, 13, 2.5),

-- Бакалея
(4, 14, 0.8),
(4, 17, 1.0);

Select * from ProductPrices;


CREATE FUNCTION dbo.GetPurchaseIdentifier (@PurchaseID INT)
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @Identifier NVARCHAR(200);
    
    SELECT @Identifier = '«' + s.Name + '» от ' + FORMAT(p.CreatedAt, 'dd.MM.yyyy HH:mm')
    FROM Purchases p
    INNER JOIN Suppliers s ON p.SupplierID = s.SupplierID
    WHERE p.PurchaseID = @PurchaseID;
    
    RETURN @Identifier;
END;

SELECT 
    PurchaseID,
    dbo.GetPurchaseIdentifier(PurchaseID) AS PurchaseIdentifier,
    Price
FROM Purchases;

GO



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

select * from ProductPrices;

delete from OrderItems where OrderID > 0
-- Проверяем работу
INSERT INTO OrderItems (OrderID, DishID, Quantity) 
VALUES (1, 1, 2);
INSERT INTO OrderItems (OrderID, DishID, Quantity) 
VALUES (1, 3, 4);

-- Смотрим результат
SELECT * FROM PurchaseItems 
SELECT * FROM Purchases 

-- 15.50

UPDATE ProductPrices SET Price = 6.50 
WHERE ProductID = 8

-- Смотрим результат
SELECT * FROM PurchaseItems 
SELECT * FROM Purchases 

delete from Purchases where PurchaseID = 8 OR PurchaseID = 2


-- Обновляем количество
UPDATE PurchaseItems SET Quantity = 100 
WHERE PurchaseID = 2 AND ProductID = 8;


-- Проверяем обновленную сумму
SELECT * FROM PurchaseItems 
SELECT * FROM Purchases 

-- Удаляем позицию
DELETE FROM PurchaseItems WHERE PurchaseID = 2 AND ProductID = 8;

-- Сумма должна стать 0
SELECT * FROM Purchases 