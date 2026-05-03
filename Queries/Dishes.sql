USE Restaurant;

-- DROP TABLE DishCategories;

-- Категории блюд
CREATE TABLE DishCategories (
    DishCategoryID INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO DishCategories (CategoryName)
VALUES
(N'Супы'),
(N'Салаты'),
(N'Основные блюда'),
(N'Десерты');

Select * from DishCategories;

----------------------------------

-- DROP TABLE Dishes;

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

INSERT INTO Dishes (Name, Price, CategoryID)
VALUES
(N'Борщ', 15.50, 1),
(N'Оливье', 20.20, 2),
(N'Драники', 25.30, 3),
(N'Шоколадный фондан', 30.00, 4);

Select * from Dishes;

----------------------------------

-- DROP TABLE DishProducts;

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


INSERT INTO DishProducts (DishID, ProductID, Quantity)
VALUES
-- Борщ
(1, 1, 0.3),   -- Картофель
(1, 3, 0.3),   -- Свекла
(1, 4, 0.4),   -- Капуста
(1, 5, 0.2),   -- Морковь
(1, 2, 0.1),   -- Лук
(1, 8, 0.5),   -- Говядина
(1, 15, 0.05), -- Томатная паста
(1, 18, 0.01), -- Соль
(1, 20, 0.01), -- Укроп
(1, 12, 0.05), -- Масло растительное

-- Оливье
(2, 1, 0.3),   -- Картофель
(2, 9, 0.3),   -- Колбаса
(2, 6, 0.2),   -- Огурцы
(2, 16, 0.2),  -- Горошек
(2, 21, 3),    -- Яйца
(2, 11, 0.2),  -- Майонез
(2, 20, 0.05), -- Укроп

-- Драники
(3, 1, 0.5),   -- Картофель
(3, 21, 2),    -- Яйца
(3, 14, 0.1),  -- Мука
(3, 2, 0.1),   -- Лук
(3, 18, 0.01), -- Соль
(3, 13, 0.05), -- Масло сливочное

-- Шоколадный фондан
(4, 19, 0.3),  -- Шоколад
(4, 13, 0.2),  -- Масло сливочное
(4, 21, 3),    -- Яйца
(4, 17, 0.15), -- Сахар
(4, 14, 0.1);  -- Мука

Select * from DishProducts;

----------------------------------
GO
CREATE OR ALTER FUNCTION dbo.GetDishProductIdentifier (
    @DishID INT,
    @ProductID INT)
RETURNS NVARCHAR(200)
AS
BEGIN
    DECLARE @Identifier NVARCHAR(200);
    
    SELECT @Identifier = d.Name + ' (' + p.Name + ')'

    FROM Products p
    INNER JOIN Dishes d ON d.DishID = @DishID
    WHERE p.ProductID = @ProductID;
    
    RETURN @Identifier;
END;
GO
SELECT 
    DishID,
    ProductID,
    dbo.GetDishProductIdentifier(DishID, ProductID) AS DishProductIdentifier
FROM DishProducts;