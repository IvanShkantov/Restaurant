
USE Restaurant;

-- Категории продуктов
CREATE TABLE ProdCategories (
    CategoryID INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);

-- Продукты
CREATE TABLE Products (
    ProductID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    ImagePath NVARCHAR(225) NULL,
    CategoryID INT,
    
    CONSTRAINT FK_Products_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES ProdCategories(CategoryID)
        ON DELETE SET NULL
        ON UPDATE CASCADE
);

-- Склады
CREATE TABLE Warehouse (
    ProductID INT PRIMARY KEY,
    Quantity DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Quantity >= 0),

    CONSTRAINT FK_Warehouse_Product
        FOREIGN KEY (ProductID) 
        REFERENCES Products(ProductID)
        ON DELETE CASCADE
);

-- Категории блюд
CREATE TABLE DishCategories (
    DishCategoryID INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);

-- Блюда
CREATE TABLE Dishes (
    DishID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(10,2) NOT NULL CHECK (Price > 0),
    ImagePath NVARCHAR(225) NULL,
    CategoryID INT,

    CONSTRAINT FK_Dish_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES DishCategories(DishCategoryID)
        ON DELETE SET NULL
        ON UPDATE CASCADE
);

-- Ингридиенты
CREATE TABLE DishProducts (
    DishID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL CHECK (Quantity > 0),

    CONSTRAINT PK_DishProducts
        PRIMARY KEY (DishID, ProductID),

    CONSTRAINT FK_DishProducts_Dish
        FOREIGN KEY (DishID)
        REFERENCES Dishes(DishID)
        ON DELETE CASCADE,

    CONSTRAINT FK_DishProducts_Product
        FOREIGN KEY (ProductID)
        REFERENCES Products(ProductID)
        ON DELETE CASCADE
);

-- Должности
CREATE TABLE Positions (
    PositionID INT IDENTITY PRIMARY KEY,
    PositionName NVARCHAR(50) NOT NULL UNIQUE
);

-- Права
CREATE TABLE [Permissions] (
    PermissionID INT IDENTITY PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL UNIQUE
);

-- Права для должностей 
CREATE TABLE PositionPermissions (
    PositionID INT NOT NULL,
    PermissionID INT NOT NULL,

    CONSTRAINT PK_PositionPermissions
        PRIMARY KEY (PositionID, PermissionID),

    CONSTRAINT FK_PP_Position
        FOREIGN KEY (PositionID)
        REFERENCES Positions(PositionID)
        ON DELETE CASCADE,

    CONSTRAINT FK_PP_Permission
        FOREIGN KEY (PermissionID)
        REFERENCES [Permissions](PermissionID)
        ON DELETE CASCADE
);

-- Сотрудники
CREATE TABLE Employees (
    EmployeeID INT IDENTITY PRIMARY KEY,
    LName NVARCHAR(50) NOT NULL,
    FName NVARCHAR(50) NOT NULL,
    MName NVARCHAR(50),
    PositionID INT NOT NULL,
    Login NVARCHAR(50),
    PasswordHash NVARCHAR(200),
    IsActivated BIT DEFAULT 0,

    CONSTRAINT FK_Employees_Positions
        FOREIGN KEY (PositionID)
        REFERENCES Positions(PositionID)
);

-- Поставщики
CREATE TABLE Suppliers (
    SupplierID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) UNIQUE,
    Email NVARCHAR(100) UNIQUE
);

-- Закупки
CREATE TABLE Purchases (
    PurchaseID INT IDENTITY PRIMARY KEY,

    SupplierID INT,
    EmployeeID INT,
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ClosedAt DATETIME2 NULL,

    PurchaseStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending' 
    CHECK (PurchaseStatus IN ('Pending', 'Delivered')),
    
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

-- Цены поставщика
CREATE TABLE ProductPrices (
    PriceID INT IDENTITY PRIMARY KEY,
    SupplierID INT NOT NULL,
    ProductID INT,
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

-- Заказы
CREATE TABLE Orders (
    OrderID INT IDENTITY PRIMARY KEY,
    EmployeeID INT,

    Status NVARCHAR(20) NOT NULL DEFAULT 'Created',
    CHECK (Status IN ('Created', 'Processing', 'Ready', 'Completed')),

    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ClosedAt DATETIME2 NULL,
    
    Price DECIMAL(10,2) DEFAULT 0,

    CONSTRAINT FK_Order_Employee
        FOREIGN KEY (EmployeeID) 
        REFERENCES Employees(EmployeeID)
        ON DELETE SET NULL
);

-- Позиции заказов
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

-- Таблица архива закупок
CREATE TABLE ArchivedPurchases (
    ArchiveID INT IDENTITY PRIMARY KEY,
    PurchaseID INT NOT NULL,
    
    CreatedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NOT NULL,
    EmployeeID INT NOT NULL,               
    EmployeeFullName NVARCHAR(200) NOT NULL, 
    SupplierID INT NOT NULL,               
    SupplierName NVARCHAR(100) NOT NULL,   
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

-- Таблица архива заказов
CREATE TABLE ArchivedOrders (
    ArchiveID INT IDENTITY PRIMARY KEY,
    OrderID INT NOT NULL,
    
    CreatedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2 NOT NULL,
    EmployeeID INT NOT NULL,               
    EmployeeFullName NVARCHAR(200) NOT NULL, 
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

CREATE TABLE ActivityLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    EventDate DATETIME NOT NULL DEFAULT GETDATE(),
    EmployeeID INT,
    EmployeeNameSnapshot NVARCHAR(200) NULL,
    EventType NVARCHAR(50) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityID INT NOT NULL,
    Description NVARCHAR(255) NULL,
    Details NVARCHAR(255) NULL,
    
    CONSTRAINT FK_ActivityLog_Employee 
        FOREIGN KEY (EmployeeID) 
        REFERENCES Employees(EmployeeID)
        ON DELETE SET NULL
);