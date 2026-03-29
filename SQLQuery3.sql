-- Создание базы данных
USE [master]
GO

-- Удаляем старую базу если существует
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'LoadMate')
BEGIN
    ALTER DATABASE [LoadMate] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    DROP DATABASE [LoadMate]
END
GO

CREATE DATABASE [LoadMate]
 CONTAINMENT = NONE
 ON PRIMARY 
( NAME = N'LoadMate', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\LoadMate.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'LoadMate_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\LoadMate_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO

ALTER DATABASE [LoadMate] SET COMPATIBILITY_LEVEL = 160
GO

USE [LoadMate]
GO

-- =====================================================
-- 1. Справочные таблицы (для соответствия 3НФ)
-- =====================================================

-- Статусы пользователя
CREATE TABLE [dbo].[UserStatus] (
    [UserStatus_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_UserStatus PRIMARY KEY CLUSTERED ([UserStatus_id] ASC)
)
GO

-- Статусы водителя
CREATE TABLE [dbo].[DriverStatus] (
    [DriverStatus_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_DriverStatus PRIMARY KEY CLUSTERED ([DriverStatus_id] ASC)
)
GO

-- Статусы заказа
CREATE TABLE [dbo].[OrderStatus] (
    [OrderStatus_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_OrderStatus PRIMARY KEY CLUSTERED ([OrderStatus_id] ASC)
)
GO

-- Статусы оплаты
CREATE TABLE [dbo].[PaymentStatus] (
    [PaymentStatus_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_PaymentStatus PRIMARY KEY CLUSTERED ([PaymentStatus_id] ASC)
)
GO

-- Статусы грузовика
CREATE TABLE [dbo].[TruckStatus] (
    [TruckStatus_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_TruckStatus PRIMARY KEY CLUSTERED ([TruckStatus_id] ASC)
)
GO

-- Типы грузов
CREATE TABLE [dbo].[CargoType] (
    [CargoType_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_CargoType PRIMARY KEY CLUSTERED ([CargoType_id] ASC)
)
GO

-- Пол
CREATE TABLE [dbo].[Gender] (
    [Gender_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(20) NOT NULL,
    CONSTRAINT PK_Gender PRIMARY KEY CLUSTERED ([Gender_id] ASC)
)
GO

-- =====================================================
-- 2. Адресная система (3НФ)
-- =====================================================

-- Страны
CREATE TABLE [dbo].[Country] (
    [Country_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(3) NULL,
    CONSTRAINT PK_Country PRIMARY KEY CLUSTERED ([Country_id] ASC)
)
GO

-- Регионы/Области
CREATE TABLE [dbo].[Region] (
    [Region_id] INT IDENTITY(1,1) NOT NULL,
    [Country_id] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Region PRIMARY KEY CLUSTERED ([Region_id] ASC)
)
GO

-- Города
CREATE TABLE [dbo].[City] (
    [City_id] INT IDENTITY(1,1) NOT NULL,
    [Region_id] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [PostalCode] NVARCHAR(20) NULL,
    CONSTRAINT PK_City PRIMARY KEY CLUSTERED ([City_id] ASC)
)
GO

-- Улицы
CREATE TABLE [dbo].[Street] (
    [Street_id] INT IDENTITY(1,1) NOT NULL,
    [City_id] INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    CONSTRAINT PK_Street PRIMARY KEY CLUSTERED ([Street_id] ASC)
)
GO

-- Адреса
CREATE TABLE [dbo].[Address] (
    [Address_id] INT IDENTITY(1,1) NOT NULL,
    [Street_id] INT NOT NULL,
    [House_number] NVARCHAR(20) NOT NULL,
    [Apartment_number] NVARCHAR(20) NULL,
    [Additional_info] NVARCHAR(200) NULL,
    CONSTRAINT PK_Address PRIMARY KEY CLUSTERED ([Address_id] ASC)
)
GO

-- =====================================================
-- 3. Основные таблицы
-- =====================================================

-- Роли пользователей
CREATE TABLE [dbo].[Role] (
    [Role_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    CONSTRAINT PK_Role PRIMARY KEY CLUSTERED ([Role_id] ASC)
)
GO

-- Пользователи (расширенная таблица)
CREATE TABLE [dbo].[User] (
    [User_id] INT IDENTITY(1,1) NOT NULL,
    [Role_id] INT NOT NULL,
    [Gender_id] INT NULL,
    [UserStatus_id] INT NOT NULL DEFAULT 1,
    [Full_name] NVARCHAR(100) NOT NULL,
    [Phone] NVARCHAR(20) NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [Created_at] DATETIME NOT NULL DEFAULT GETDATE(),
    [Updated_at] DATETIME NULL,
    [Last_login] DATETIME NULL,
    CONSTRAINT PK_User PRIMARY KEY CLUSTERED ([User_id] ASC),
    CONSTRAINT UQ_User_Email UNIQUE ([Email])
)
GO

-- Таблица авторизации (логины/пароли)
CREATE TABLE [dbo].[Login] (
    [Login_id] INT IDENTITY(1,1) NOT NULL,
    [User_id] INT NOT NULL,
    [Username] NVARCHAR(50) NOT NULL,
    [Password_hash] NVARCHAR(255) NOT NULL,
    [Is_active] BIT NOT NULL DEFAULT 1,
    [Failed_attempts] INT NOT NULL DEFAULT 0,
    [Last_login_attempt] DATETIME NULL,
    [Password_changed_at] DATETIME NULL,
    CONSTRAINT PK_Login PRIMARY KEY CLUSTERED ([Login_id] ASC),
    CONSTRAINT UQ_Login_Username UNIQUE ([Username])
)
GO

-- Водители
CREATE TABLE [dbo].[Driver] (
    [Driver_id] INT IDENTITY(1,1) NOT NULL,
    [User_id] INT NOT NULL,
    [DriverStatus_id] INT NOT NULL DEFAULT 1,
    [License_number] NVARCHAR(20) NOT NULL,
    [License_expiry_date] DATE NULL,
    [Experience_years] INT NULL,
    [Hire_date] DATE NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Driver PRIMARY KEY CLUSTERED ([Driver_id] ASC),
    CONSTRAINT UQ_Driver_License UNIQUE ([License_number])
)
GO

-- Грузовики
CREATE TABLE [dbo].[Truck] (
    [Truck_id] INT IDENTITY(1,1) NOT NULL,
    [Driver_id] INT NULL,
    [TruckStatus_id] INT NOT NULL DEFAULT 1,
    [Model] NVARCHAR(50) NOT NULL,
    [Registration_number] NVARCHAR(20) NOT NULL,
    [Capacity_kg] DECIMAL(10, 2) NOT NULL,
    [Capacity_m3] DECIMAL(10, 2) NOT NULL,
    [Dimensions] NVARCHAR(50) NULL,
    [Year_manufacture] INT NULL,
    [Fuel_consumption] DECIMAL(8, 2) NULL,
    CONSTRAINT PK_Truck PRIMARY KEY CLUSTERED ([Truck_id] ASC),
    CONSTRAINT UQ_Truck_Registration UNIQUE ([Registration_number])
)
GO

-- Тарифы
CREATE TABLE [dbo].[Tariff] (
    [Tariff_id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [Cost_per_km] DECIMAL(10, 2) NOT NULL,
    [Cost_per_kg] DECIMAL(10, 2) NOT NULL,
    [Cost_per_m3] DECIMAL(10, 2) NOT NULL,
    [Additional_cost] DECIMAL(10, 2) NOT NULL DEFAULT 0,
    [Min_price] DECIMAL(10, 2) NULL,
    [Is_active] BIT NOT NULL DEFAULT 1,
    [Created_time] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Tariff PRIMARY KEY CLUSTERED ([Tariff_id] ASC)
)
GO

-- Грузы
CREATE TABLE [dbo].[Cargo] (
    [Cargo_id] INT IDENTITY(1,1) NOT NULL,
    [Client_id] INT NOT NULL,
    [CargoType_id] INT NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Weight_kg] DECIMAL(10, 2) NOT NULL,
    [Volume_m3] DECIMAL(10, 2) NOT NULL,
    [Is_fragile] BIT NOT NULL DEFAULT 0,
    [Is_dangerous] BIT NOT NULL DEFAULT 0,
    [Special_requirements] NVARCHAR(500) NULL,
    [Created_at] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Cargo PRIMARY KEY CLUSTERED ([Cargo_id] ASC)
)
GO

-- Маршруты
CREATE TABLE [dbo].[Route] (
    [Route_id] INT IDENTITY(1,1) NOT NULL,
    [Start_address_id] INT NOT NULL,
    [End_address_id] INT NOT NULL,
    [Distance_km] DECIMAL(10, 2) NOT NULL,
    [Estimated_time_hours] DECIMAL(10, 2) NOT NULL,
    [Waypoints] NVARCHAR(MAX) NULL,
    CONSTRAINT PK_Route PRIMARY KEY CLUSTERED ([Route_id] ASC)
)
GO

-- Заказы
CREATE TABLE [dbo].[Order] (
    [Order_id] INT IDENTITY(1,1) NOT NULL,
    [Manager_id] INT NOT NULL,
    [Cargo_id] INT NOT NULL,
    [Tariff_id] INT NOT NULL,
    [Truck_id] INT NOT NULL,
    [Route_id] INT NOT NULL,
    [OrderStatus_id] INT NOT NULL DEFAULT 1,
    [Order_number] NVARCHAR(50) NOT NULL,
    [Order_date] DATETIME NOT NULL DEFAULT GETDATE(),
    [Price] DECIMAL(10, 2) NOT NULL,
    [Scheduled_pickup] DATETIME NULL,
    [Scheduled_delivery] DATETIME NULL,
    [Actual_pickup] DATETIME NULL,
    [Actual_delivery] DATETIME NULL,
    [Notes] NVARCHAR(500) NULL,
    CONSTRAINT PK_Order PRIMARY KEY CLUSTERED ([Order_id] ASC),
    CONSTRAINT UQ_Order_Number UNIQUE ([Order_number])
)
GO

-- Платежи
CREATE TABLE [dbo].[Payment] (
    [Payment_id] INT IDENTITY(1,1) NOT NULL,
    [Order_id] INT NOT NULL,
    [PaymentStatus_id] INT NOT NULL DEFAULT 1,
    [Amount] DECIMAL(10, 2) NOT NULL,
    [Payment_method] NVARCHAR(50) NULL,
    [Transaction_id] NVARCHAR(100) NULL,
    [Transaction_date] DATETIME NOT NULL DEFAULT GETDATE(),
    [Paid_date] DATETIME NULL,
    CONSTRAINT PK_Payment PRIMARY KEY CLUSTERED ([Payment_id] ASC)
)
GO

-- =====================================================
-- 4. Журналы и аудит
-- =====================================================

-- Журнал действий пользователей
CREATE TABLE [dbo].[UserActivityLog] (
    [Log_id] INT IDENTITY(1,1) NOT NULL,
    [User_id] INT NOT NULL,
    [Action_type] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IP_address] NVARCHAR(45) NULL,
    [Created_at] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UserActivityLog PRIMARY KEY CLUSTERED ([Log_id] ASC)
)
GO

-- Журнал изменений заказов
CREATE TABLE [dbo].[OrderHistory] (
    [History_id] INT IDENTITY(1,1) NOT NULL,
    [Order_id] INT NOT NULL,
    [User_id] INT NOT NULL,
    [Old_status_id] INT NULL,
    [New_status_id] INT NOT NULL,
    [Comment] NVARCHAR(500) NULL,
    [Changed_at] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_OrderHistory PRIMARY KEY CLUSTERED ([History_id] ASC)
)
GO

-- =====================================================
-- 5. Внешние ключи
-- =====================================================

-- Адресная система
ALTER TABLE [dbo].[Region] ADD CONSTRAINT FK_Region_Country FOREIGN KEY ([Country_id]) REFERENCES [dbo].[Country] ([Country_id])
GO
ALTER TABLE [dbo].[City] ADD CONSTRAINT FK_City_Region FOREIGN KEY ([Region_id]) REFERENCES [dbo].[Region] ([Region_id])
GO
ALTER TABLE [dbo].[Street] ADD CONSTRAINT FK_Street_City FOREIGN KEY ([City_id]) REFERENCES [dbo].[City] ([City_id])
GO
ALTER TABLE [dbo].[Address] ADD CONSTRAINT FK_Address_Street FOREIGN KEY ([Street_id]) REFERENCES [dbo].[Street] ([Street_id])
GO

-- Основные таблицы
ALTER TABLE [dbo].[User] ADD CONSTRAINT FK_User_Role FOREIGN KEY ([Role_id]) REFERENCES [dbo].[Role] ([Role_id])
GO
ALTER TABLE [dbo].[User] ADD CONSTRAINT FK_User_Gender FOREIGN KEY ([Gender_id]) REFERENCES [dbo].[Gender] ([Gender_id])
GO
ALTER TABLE [dbo].[User] ADD CONSTRAINT FK_User_UserStatus FOREIGN KEY ([UserStatus_id]) REFERENCES [dbo].[UserStatus] ([UserStatus_id])
GO

ALTER TABLE [dbo].[Login] ADD CONSTRAINT FK_Login_User FOREIGN KEY ([User_id]) REFERENCES [dbo].[User] ([User_id])
GO

ALTER TABLE [dbo].[Driver] ADD CONSTRAINT FK_Driver_User FOREIGN KEY ([User_id]) REFERENCES [dbo].[User] ([User_id])
GO
ALTER TABLE [dbo].[Driver] ADD CONSTRAINT FK_Driver_DriverStatus FOREIGN KEY ([DriverStatus_id]) REFERENCES [dbo].[DriverStatus] ([DriverStatus_id])
GO

ALTER TABLE [dbo].[Truck] ADD CONSTRAINT FK_Truck_Driver FOREIGN KEY ([Driver_id]) REFERENCES [dbo].[Driver] ([Driver_id])
GO
ALTER TABLE [dbo].[Truck] ADD CONSTRAINT FK_Truck_TruckStatus FOREIGN KEY ([TruckStatus_id]) REFERENCES [dbo].[TruckStatus] ([TruckStatus_id])
GO

ALTER TABLE [dbo].[Cargo] ADD CONSTRAINT FK_Cargo_User FOREIGN KEY ([Client_id]) REFERENCES [dbo].[User] ([User_id])
GO
ALTER TABLE [dbo].[Cargo] ADD CONSTRAINT FK_Cargo_CargoType FOREIGN KEY ([CargoType_id]) REFERENCES [dbo].[CargoType] ([CargoType_id])
GO

ALTER TABLE [dbo].[Route] ADD CONSTRAINT FK_Route_StartAddress FOREIGN KEY ([Start_address_id]) REFERENCES [dbo].[Address] ([Address_id])
GO
ALTER TABLE [dbo].[Route] ADD CONSTRAINT FK_Route_EndAddress FOREIGN KEY ([End_address_id]) REFERENCES [dbo].[Address] ([Address_id])
GO

ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_Manager FOREIGN KEY ([Manager_id]) REFERENCES [dbo].[User] ([User_id])
GO
ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_Cargo FOREIGN KEY ([Cargo_id]) REFERENCES [dbo].[Cargo] ([Cargo_id])
GO
ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_Tariff FOREIGN KEY ([Tariff_id]) REFERENCES [dbo].[Tariff] ([Tariff_id])
GO
ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_Truck FOREIGN KEY ([Truck_id]) REFERENCES [dbo].[Truck] ([Truck_id])
GO
ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_Route FOREIGN KEY ([Route_id]) REFERENCES [dbo].[Route] ([Route_id])
GO
ALTER TABLE [dbo].[Order] ADD CONSTRAINT FK_Order_OrderStatus FOREIGN KEY ([OrderStatus_id]) REFERENCES [dbo].[OrderStatus] ([OrderStatus_id])
GO

ALTER TABLE [dbo].[Payment] ADD CONSTRAINT FK_Payment_Order FOREIGN KEY ([Order_id]) REFERENCES [dbo].[Order] ([Order_id])
GO
ALTER TABLE [dbo].[Payment] ADD CONSTRAINT FK_Payment_PaymentStatus FOREIGN KEY ([PaymentStatus_id]) REFERENCES [dbo].[PaymentStatus] ([PaymentStatus_id])
GO

ALTER TABLE [dbo].[UserActivityLog] ADD CONSTRAINT FK_UserActivityLog_User FOREIGN KEY ([User_id]) REFERENCES [dbo].[User] ([User_id])
GO

ALTER TABLE [dbo].[OrderHistory] ADD CONSTRAINT FK_OrderHistory_Order FOREIGN KEY ([Order_id]) REFERENCES [dbo].[Order] ([Order_id])
GO
ALTER TABLE [dbo].[OrderHistory] ADD CONSTRAINT FK_OrderHistory_User FOREIGN KEY ([User_id]) REFERENCES [dbo].[User] ([User_id])
GO
ALTER TABLE [dbo].[OrderHistory] ADD CONSTRAINT FK_OrderHistory_OldStatus FOREIGN KEY ([Old_status_id]) REFERENCES [dbo].[OrderStatus] ([OrderStatus_id])
GO
ALTER TABLE [dbo].[OrderHistory] ADD CONSTRAINT FK_OrderHistory_NewStatus FOREIGN KEY ([New_status_id]) REFERENCES [dbo].[OrderStatus] ([OrderStatus_id])
GO

-- =====================================================
-- 6. Индексы для оптимизации
-- =====================================================

CREATE INDEX IX_User_Email ON [dbo].[User] ([Email])
CREATE INDEX IX_User_Role ON [dbo].[User] ([Role_id])
CREATE INDEX IX_Login_Username ON [dbo].[Login] ([Username])
CREATE INDEX IX_Login_User ON [dbo].[Login] ([User_id])
CREATE INDEX IX_Order_Manager ON [dbo].[Order] ([Manager_id])
CREATE INDEX IX_Order_Status ON [dbo].[Order] ([OrderStatus_id])
CREATE INDEX IX_Order_Date ON [dbo].[Order] ([Order_date])
CREATE INDEX IX_Payment_Order ON [dbo].[Payment] ([Order_id])
CREATE INDEX IX_Cargo_Client ON [dbo].[Cargo] ([Client_id])

-- =====================================================
-- 7. Начальные данные
-- =====================================================

-- Статусы
INSERT INTO [dbo].[UserStatus] ([Name], [Description]) VALUES 
(N'Активен', N'Пользователь активен'),
(N'Заблокирован', N'Пользователь заблокирован'),
(N'Ожидает подтверждения', N'Пользователь ожидает подтверждения email')
GO

INSERT INTO [dbo].[DriverStatus] ([Name], [Description]) VALUES 
(N'Доступен', N'Водитель доступен для назначения'),
(N'На маршруте', N'Водитель выполняет заказ'),
(N'Не доступен', N'Водитель недоступен (отпуск, больничный)'),
(N'Отдых', N'Водитель на отдыхе')
GO

INSERT INTO [dbo].[OrderStatus] ([Name], [Description]) VALUES 
(N'Новый', N'Новый заказ, ожидает обработки'),
(N'Подтвержден', N'Заказ подтвержден менеджером'),
(N'Назначен водитель', N'Водитель назначен на заказ'),
(N'Загрузка', N'Идет загрузка груза'),
(N'В пути', N'Груз в пути'),
(N'Доставлен', N'Груз доставлен получателю'),
(N'Завершен', N'Заказ завершен'),
(N'Отменен', N'Заказ отменен')
GO

INSERT INTO [dbo].[PaymentStatus] ([Name], [Description]) VALUES 
(N'Ожидает оплаты', N'Платеж ожидает оплаты'),
(N'Оплачен', N'Платеж выполнен'),
(N'Отменен', N'Платеж отменен'),
(N'Возврат', N'Произведен возврат средств')
GO

INSERT INTO [dbo].[TruckStatus] ([Name], [Description]) VALUES 
(N'Исправен', N'Грузовик исправен и готов к работе'),
(N'На ремонте', N'Грузовик на ремонте'),
(N'На маршруте', N'Грузовик выполняет заказ'),
(N'На техобслуживании', N'Грузовик на плановом ТО')
GO

INSERT INTO [dbo].[CargoType] ([Name], [Description]) VALUES 
(N'Стандартный', N'Обычные грузы'),
(N'Хрупкий', N'Хрупкие грузы, требующие осторожной перевозки'),
(N'Опасный', N'Опасные грузы'),
(N'Скоропортящийся', N'Требует температурного режима'),
(N'Крупногабаритный', N'Грузы с нестандартными размерами')
GO

INSERT INTO [dbo].[Gender] ([Name]) VALUES 
(N'Мужской'),
(N'Женский')
GO

-- Роли
INSERT INTO [dbo].[Role] ([Name], [Description]) VALUES 
(N'Администратор', N'Полный доступ к системе'),
(N'Клиент', N'Заказчик услуг'),
(N'Водитель', N'Исполнитель заказов'),
(N'Диспетчер', N'Организация перевозок')
GO

-- Страны
INSERT INTO [dbo].[Country] ([Name], [Code]) VALUES 
(N'Россия', N'RUS')
GO

-- Регионы
INSERT INTO [dbo].[Region] ([Country_id], [Name]) VALUES 
(1, N'Московская область'),
(1, N'Санкт-Петербург'),
(1, N'Татарстан'),
(1, N'Свердловская область')
GO

-- Города
INSERT INTO [dbo].[City] ([Region_id], [Name]) VALUES 
(1, N'Москва'),
(1, N'Красногорск'),
(2, N'Санкт-Петербург'),
(3, N'Казань'),
(4, N'Екатеринбург')
GO

-- Улицы
INSERT INTO [dbo].[Street] ([City_id], [Name]) VALUES 
(1, N'ул. Ленина'),
(1, N'ул. Тверская'),
(1, N'пр. Мира'),
(3, N'ул. Невский пр.'),
(4, N'ул. Баумана'),
(5, N'ул. Ленина')
GO

-- Адреса
INSERT INTO [dbo].[Address] ([Street_id], [House_number], [Apartment_number]) VALUES 
(1, N'1', NULL),
(1, N'15', N'45'),
(2, N'25', NULL),
(3, N'10', NULL),
(4, N'50', N'12'),
(5, N'7', N'3')
GO

-- Тарифы
INSERT INTO [dbo].[Tariff] ([Name], [Description], [Cost_per_km], [Cost_per_kg], [Cost_per_m3], [Additional_cost], [Min_price]) VALUES 
(N'Эконом', N'Экономичная доставка 3-5 дней', 15.00, 5.00, 10.00, 0, 1000.00),
(N'Стандарт', N'Стандартная доставка 1-3 дня', 25.00, 8.00, 12.00, 0, 1500.00),
(N'Экспресс', N'Экспресс-доставка до 24 часов', 40.00, 12.00, 15.00, 1000.00, 2500.00)
GO

-- Пользователи (для тестирования)
INSERT INTO [dbo].[User] ([Role_id], [Gender_id], [UserStatus_id], [Full_name], [Phone], [Email]) VALUES 
(1, 1, 1, N'Администраторов Админ Админович', N'+79001234567', N'admin@loadmate.ru'),
(4, 1, 1, N'Диспетчеров Дмитрий Петрович', N'+79001234568', N'dispatcher@loadmate.ru'),
(2, 1, 1, N'Клиентов Иван Иванович', N'+79001234569', N'client1@loadmate.ru'),
(3, 1, 1, N'Водителев Сергей Николаевич', N'+79001234570', N'driver1@loadmate.ru')
GO

-- Логины
INSERT INTO [dbo].[Login] ([User_id], [Username], [Password_hash], [Is_active]) VALUES 
(1, N'admin', N'$2a$11$8HsJqKZqZqZqZqZqZqZqZu', 1),  -- пароль: admin123 (хэш нужно будет пересоздать)
(2, N'dispatcher', N'$2a$11$8HsJqKZqZqZqZqZqZqZqZu', 1),
(3, N'client1', N'$2a$11$8HsJqKZqZqZqZqZqZqZqZu', 1),
(4, N'driver1', N'$2a$11$8HsJqKZqZqZqZqZqZqZqZu', 1)
GO

-- Водители
INSERT INTO [dbo].[Driver] ([User_id], [DriverStatus_id], [License_number], [Experience_years], [Hire_date]) VALUES 
(4, 1, N'77AA123456', 5, GETDATE())
GO

-- Грузовики
INSERT INTO [dbo].[Truck] ([Driver_id], [TruckStatus_id], [Model], [Registration_number], [Capacity_kg], [Capacity_m3], [Dimensions]) VALUES 
(NULL, 1, N'КамАЗ 6520', N'А123ВС77', 20000.00, 50.00, N'6.0x2.5x3.0'),
(NULL, 1, N'ГАЗель NEXT', N'В456СН77', 1500.00, 12.00, N'3.0x2.0x2.0')
GO

-- Грузы
INSERT INTO [dbo].[Cargo] ([Client_id], [CargoType_id], [Description], [Weight_kg], [Volume_m3], [Is_fragile]) VALUES 
(3, 1, N'Электроника: ноутбуки и телефоны', 150.00, 1.50, 1),
(3, 4, N'Замороженные продукты', 500.00, 3.00, 0)
GO

-- Маршруты
INSERT INTO [dbo].[Route] ([Start_address_id], [End_address_id], [Distance_km], [Estimated_time_hours]) VALUES 
(1, 4, 715.00, 10.00),
(2, 5, 850.00, 12.00)
GO

-- Заказы
INSERT INTO [dbo].[Order] ([Manager_id], [Cargo_id], [Tariff_id], [Truck_id], [Route_id], [OrderStatus_id], [Order_number], [Price], [Scheduled_pickup], [Scheduled_delivery]) VALUES 
(2, 1, 2, 1, 1, 1, N'ORD-20240001', 18500.00, DATEADD(DAY, 1, GETDATE()), DATEADD(DAY, 2, GETDATE()))
GO

-- Платежи
INSERT INTO [dbo].[Payment] ([Order_id], [PaymentStatus_id], [Amount], [Payment_method]) VALUES 
(1, 1, 18500.00, N'Банковская карта')
GO

PRINT 'База данных LoadMate успешно создана!'