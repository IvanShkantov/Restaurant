CREATE TABLE ArchivedPurchases (
    ArchiveID INT IDENTITY PRIMARY KEY,
    PurchaseID INT NOT NULL,
    
    CreatedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NOT NULL,
    EmployeeID INT NOT NULL,               -- Для фильтрации
    EmployeeFullName NVARCHAR(200) NOT NULL, -- Для отображения
    SupplierID INT NOT NULL,               -- Для фильтрации
    SupplierName NVARCHAR(100) NOT NULL,   -- Для отображения
    TotalPrice DECIMAL(10,2) NOT NULL,
    
    ArchivedAt DATETIME2 DEFAULT GETDATE()
);

-- Таблица архива позиций закупок
CREATE TABLE ArchivedPurchaseItems (
    ArchiveItemID INT IDENTITY PRIMARY KEY,
    ArchiveID INT NOT NULL,
    
    ProductName NVARCHAR(100) NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    
    CONSTRAINT FK_ArchivedItem_Purchase 
        FOREIGN KEY (ArchiveID) 
        REFERENCES ArchivedPurchases(ArchiveID)
        ON DELETE CASCADE
);

CREATE TABLE ArchivedOrders (
    ArchiveID INT IDENTITY PRIMARY KEY,
    OrderID INT NOT NULL,
    
    CreatedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NOT NULL,
    EmployeeID INT NOT NULL,               -- Для фильтрации
    EmployeeFullName NVARCHAR(200) NOT NULL, -- Для отображения
    TotalPrice DECIMAL(10,2) NOT NULL,
    
    ArchivedAt DATETIME2 DEFAULT GETDATE()
);

-- Таблица архива позиций заказов
CREATE TABLE ArchivedOrderItems (
    ArchiveItemID INT IDENTITY PRIMARY KEY,
    ArchiveID INT NOT NULL,
    
    DishName NVARCHAR(100) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    
    CONSTRAINT FK_ArchivedItem_Order 
        FOREIGN KEY (ArchiveID) 
        REFERENCES ArchivedOrders(ArchiveID)
        ON DELETE CASCADE
);

