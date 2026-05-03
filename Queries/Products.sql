
USE Restaurant;

----------------------------------

-- DROP TABLE ProdCategories;

-- Категории
CREATE TABLE ProdCategories (
    CategoryID INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO ProdCategories (CategoryName) VALUES
(N'Овощи'),
(N'Мясо'),
(N'Молочные продукты'),
(N'Бакалея'),
(N'Зелень'),
(N'Яйца');

Select * from ProdCategories;

----------------------------------

-- DROP TABLE Products;

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


INSERT INTO Products (Name, Unit, CategoryID) VALUES
(N'Картофель', N'кг', 1),
(N'Лук репчатый', N'кг', 1),
(N'Свекла', N'кг', 1),
(N'Капуста', N'кг', 1),
(N'Морковь', N'кг', 1),
(N'Огурцы маринованные', N'кг', 1),

(N'Куриное филе', N'кг', 2),
(N'Говядина', N'кг', 2),
(N'Колбаса варёная', N'кг', 2),

(N'Молоко', N'л', 3),
(N'Майонез', N'кг', 3),
(N'Масло растительное', N'л', 3),
(N'Масло сливочное', N'кг', 3),

(N'Мука', N'кг', 4),
(N'Томатная паста', N'кг', 4),
(N'Горошек', N'кг', 4),
(N'Сахар', N'кг', 4),
(N'Соль', N'кг', 4),
(N'Шоколад', N'кг', 4),

(N'Укроп', N'кг', 5),

(N'Яйца куриные', N'шт', 6);
Update Products set ImagePath = NULL Where ProductID > 0
Select * from Products;

----------------------------------

-- DROP TABLE Warehouse;

-- Склады
CREATE TABLE Warehouse (
    ProductID INT PRIMARY KEY,
    Quantity DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Quantity >= 0),

    CONSTRAINT FK_Warehouse_Product
        FOREIGN KEY (ProductID) 
        REFERENCES Products(ProductID)
        ON DELETE CASCADE
);

select * from Products

INSERT INTO Warehouse (ProductID, Quantity, MinQuantity, MaxQuantity)
VALUES
(1, 80, 20, 150),   -- Картофель
(2, 40, 10, 100),   -- Лук
(3, 30, 10, 80),    -- Свекла
(4, 35, 10, 90),    -- Капуста
(5, 25, 8, 70),     -- Морковь
(6, 20, 5, 60),     -- Огурцы маринованные

(7, 25, 10, 50),    -- Куриное филе
(8, 20, 10, 40),    -- Говядина
(9, 15, 5, 40),     -- Колбаса

(10, 50, 15, 80),   -- Молоко
(11, 20, 5, 40),    -- Майонез
(12, 30, 10, 60),   -- Масло растительное
(13, 15, 5, 30),    -- Масло сливочное

(14, 60, 20, 120),  -- Мука
(15, 25, 5, 50),    -- Томатная паста
(16, 30, 10, 70),   -- Горошек
(17, 40, 10, 100),  -- Сахар
(18, 50, 10, 120),  -- Соль
(19, 20, 5, 60),    -- Шоколад

(20, 5, 2, 15),     -- Укроп
(21, 100, 30, 200); -- Яйца

Select * from Warehouse;
