
USE Restaurant;

----------------------------------

-- DROP TABLE Positions;

-- Должности
CREATE TABLE Positions (
    PositionID INT IDENTITY PRIMARY KEY,
    PositionName NVARCHAR(50) NOT NULL UNIQUE
);
delete from Positions
INSERT INTO Positions (PositionName)
VALUES
(N'Администратор'),
(N'Повар'),
(N'Су-шеф'),
(N'Кладовщик'),
(N'Закупщик'),
(N'Официант'),
(N'Бухгалтер ');

Update Positions set PositionID = 6 where PositionName = 'Официант'
delete from Positions where PositionName = 'Кассир'
Select * from Positions;

----------------------------------

-- DROP TABLE [Permissions];

-- Права
CREATE TABLE [Permissions] (
    PermissionID INT IDENTITY PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO [Permissions] (PermissionName)
VALUES
(N'Просмотр продуктов'),
(N'Редактирование продуктов'),
(N'Просмотр журнала'), 
(N'Управление блюдами'),
(N'Оформление закупок'),
(N'Управление сотрудниками'),
(N'Управление складом'), 
(N'Оформление заказов'),
(N'Управление итогами'); 

update [Permissions] set PermissionName = 'Просмотр журнала' where PermissionName = 'Просмотр блюд'
update [Permissions] set PermissionName = 'Управление сотрудниками' where PermissionName = 'Просмотр склада'
update [Permissions] set PermissionName = 'Управление итогами' where PermissionName = 'Управление продажами'

Select * from [Permissions];

----------------------------------

-- DROP TABLE PositionPermissions;

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




INSERT INTO PositionPermissions VALUES
-- Администратор
(1,1),(1,2),(1,3),(1,4),(1,5),(1,6),(1,7),(1,8),(1,9),

-- Повар
(2,1),(2,3),(2,4),

-- Су-шеф
(3,1),(3,3),(3,4),(3,6),

-- Кладовщик
(4,1),(4,6),(4,7),

-- Закупщик
(5,1),(5,5),(5,6),

-- Официант
(7,1),(7,8);

Select * from PositionPermissions;

----------------------------------

--DROP TABLE Employees;

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

INSERT INTO Employees (LName, FName, MName, Login, PositionID) VALUES
(N'Захаров', N'Харитон', N'Радеонович', 'admin', 1),
(N'Фелатова', N'Лариса', N'Андреевна', 'ofish2', 7),
(N'Сеченов', N'Дмитрий', N'Сергеевич', 'syshef', 3),
(N'Муравьёва', N'Зинаида', N'Петровна', 'povar', 2),
(N'Петров', N'Виктор', N'Васильевич', 'kladovsh', 4),
(N'Терешкова', N'Валентина', N'Владимировна', 'ofish', 7),
(N'Нечаев', N'Сергей', N'Алексеевич', 'zakup', 5);

--Update Employees set PositionID = 7 where LName = 'Фелатова'
Update Employees set IsActivated = 0 where PasswordHash is NULL

Select * from Employees;

delete from Employees where EmployeeID = 9;


CREATE FUNCTION dbo.GetEmployeeFullName (@EmployeeID INT)
RETURNS NVARCHAR(300)
AS
BEGIN
    DECLARE @FullName NVARCHAR(300);
    
    SELECT @FullName = LName + ' ' + FName + ' ' + ISNULL(MName, '')
    FROM Employees
    WHERE EmployeeID = @EmployeeID;
    
    RETURN @FullName;
END;

GO

CREATE PROCEDURE sp_LoginUser
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
GO