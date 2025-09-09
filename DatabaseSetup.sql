-- RentAPlace Database Setup Script
-- Run this script to create the database and tables

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RentAPlaceDB')
BEGIN
    CREATE DATABASE RentAPlaceDB;
END
GO

USE RentAPlaceDB;
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);
END
GO

-- Create Properties table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Properties]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Properties] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [OwnerId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [Type] int NOT NULL,
        [Features] nvarchar(1000) NULL,
        [PricePerNight] decimal(18,2) NOT NULL,
        [MaxGuests] int NOT NULL,
        [Bedrooms] int NOT NULL,
        [Bathrooms] int NOT NULL,
        [IsAvailable] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Properties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Properties_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Create PropertyImages table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyImages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PropertyImages] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [PropertyId] int NOT NULL,
        [ImagePath] nvarchar(500) NOT NULL,
        [AltText] nvarchar(200) NULL,
        [IsPrimary] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PropertyImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyImages_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[Properties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Create Reservations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reservations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Reservations] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [PropertyId] int NOT NULL,
        [CheckInDate] datetime2 NOT NULL,
        [CheckOutDate] datetime2 NOT NULL,
        [NumberOfGuests] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [SpecialRequests] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ConfirmedAt] datetime2 NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reservations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE  NO ACTION,
        CONSTRAINT [FK_Reservations_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[Properties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Create Messages table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Messages] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [SenderId] int NOT NULL,
        [ReceiverId] int NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [Timestamp] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [IsRead] bit NOT NULL DEFAULT 0,
        [PropertyId] int NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[Properties] ([Id]) ON DELETE NO ACTION
    );
END
GO

-- Create Notifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [Type] int NOT NULL,
        [IsRead] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [RelatedEntityId] int NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Insert sample data into Users
DECLARE @User1Id INT, @User2Id INT;

-- Insert John Doe (Role = User)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'john.doe@example.com')
BEGIN
    INSERT INTO Users (Name, Email, PasswordHash, Role)
    VALUES ('John Doe', 'john.doe@example.com', 'hashedpwd1', 0);
    SET @User1Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @User1Id = Id FROM Users WHERE Email = 'john.doe@example.com';

-- Insert John Smith (Role = Owner)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'john.smith@example.com')
BEGIN
    INSERT INTO Users (Name, Email, PasswordHash, Role)
    VALUES ('John Smith', 'john.smith@example.com', 'hashedpwd2', 1);
    SET @User2Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @User2Id = Id FROM Users WHERE Email = 'john.smith@example.com';

-- Insert sample data into Properties
DECLARE @Prop1Id INT, @Prop2Id INT;

INSERT INTO [dbo].[Properties] 
    ([OwnerId], [Title], [Description], [Location], [Type], [Features], [PricePerNight], [MaxGuests], [Bedrooms], [Bathrooms], [IsAvailable])
VALUES 
    (@User2Id, 'Beautiful Beach House', 'A stunning beachfront property with amazing ocean views', 'Miami, FL', 1, '["Pool", "WiFi", "Sea View"]', 250.00, 6, 3, 2, 1);

SET @Prop1Id = SCOPE_IDENTITY();

INSERT INTO [dbo].[Properties] 
    ([OwnerId], [Title], [Description], [Location], [Type], [Features], [PricePerNight], [MaxGuests], [Bedrooms], [Bathrooms], [IsAvailable])
VALUES 
    (@User2Id, 'Downtown Apartment', 'Modern apartment in the heart of the city', 'New York, NY', 0, '["WiFi", "Kitchen", "Air Conditioning"]', 150.00, 4, 2, 1, 1);

SET @Prop2Id = SCOPE_IDENTITY();


-- Insert sample data into PropertyImages
INSERT INTO [dbo].[PropertyImages] ([PropertyId], [ImagePath], [AltText], [IsPrimary])
VALUES
    (@Prop1Id, 'https://example.com/images/beachhouse1.jpg', 'Front view of the beach house', 1),
    (@Prop1Id, 'https://example.com/images/beachhouse2.jpg', 'Living room', 0),
    (@Prop2Id, 'https://example.com/images/downtown1.jpg', 'Apartment exterior', 1),
    (@Prop2Id, 'https://example.com/images/downtown2.jpg', 'Bedroom view', 0);


-- Insert sample data into Reservations
INSERT INTO [dbo].[Reservations] ([UserId], [PropertyId], [CheckInDate], [CheckOutDate], [NumberOfGuests], [TotalAmount], [Status])
VALUES
    (@User1Id, @Prop1Id, '2025-09-15', '2025-09-20', 4, 1250.00, 0),
    (@User1Id, @Prop2Id, '2025-10-01', '2025-10-05', 2, 600.00, 1);


-- Insert sample data into Messages
INSERT INTO [dbo].[Messages] ([SenderId], [ReceiverId], [Content], [PropertyId])
VALUES
    (@User1Id, @User2Id, 'Hi, I am interested in your beach house.', @Prop1Id),
    (@User2Id, @User1Id, 'Sure! The property is available on your requested dates.', @Prop1Id);


-- Insert sample data into Notifications
INSERT INTO [dbo].[Notifications] ([UserId], [Message], [Type], [RelatedEntityId], [RelatedEntityType])
VALUES
    (@User1Id, 'Your reservation for the Beach House is pending.', 0, 1, 'Reservation'),
    (@User2Id, 'You have received a new message from John Doe.', 3, 1, 'Message');


PRINT 'Database setup completed successfully!';
