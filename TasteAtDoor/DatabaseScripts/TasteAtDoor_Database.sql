IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Caterers] (
    [Id] int NOT NULL IDENTITY,
    [BusinessName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Caterers] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderDate] datetime2 NOT NULL,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CatererId] int NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_Caterers_CatererId] FOREIGN KEY ([CatererId]) REFERENCES [Caterers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [Quantity] int NOT NULL,
    [OrderId] int NOT NULL,
    [MenuItemId] int NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_MenuItems_CatererId] ON [MenuItems] ([CatererId]);

CREATE INDEX [IX_OrderItems_MenuItemId] ON [OrderItems] ([MenuItemId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_Orders_ApplicationUserId] ON [Orders] ([ApplicationUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417152153_AddIdentitySetup', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] DROP CONSTRAINT [FK_MenuItems_Caterers_CatererId];

DROP INDEX [IX_MenuItems_CatererId] ON [MenuItems];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'CatererId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [MenuItems] DROP COLUMN [CatererId];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'Name');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [MenuItems] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'Description');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [MenuItems] ALTER COLUMN [Description] nvarchar(500) NOT NULL;

ALTER TABLE [MenuItems] ADD [CaretakerId] nvarchar(450) NOT NULL DEFAULT N'';

CREATE INDEX [IX_MenuItems_CaretakerId] ON [MenuItems] ([CaretakerId]);

ALTER TABLE [MenuItems] ADD CONSTRAINT [FK_MenuItems_AspNetUsers_CaretakerId] FOREIGN KEY ([CaretakerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417161917_AddWeek2MenuSystem', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] ADD [ImageContentType] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [MenuItems] ADD [ImageData] varbinary(max) NOT NULL DEFAULT 0x;

ALTER TABLE [MenuItems] ADD [ImageFileName] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417164503_AddMenuImages', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [CustomizationGroups] (
    [Id] int NOT NULL IDENTITY,
    [MenuItemId] int NOT NULL,
    [Title] nvarchar(100) NOT NULL,
    [GroupType] nvarchar(30) NOT NULL,
    [IsRequired] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    CONSTRAINT [PK_CustomizationGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomizationGroups_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CustomizationOptions] (
    [Id] int NOT NULL IDENTITY,
    [CustomizationGroupId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [PriceChange] decimal(18,2) NOT NULL,
    [IsDefault] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    CONSTRAINT [PK_CustomizationOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomizationOptions_CustomizationGroups_CustomizationGroupId] FOREIGN KEY ([CustomizationGroupId]) REFERENCES [CustomizationGroups] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_CustomizationGroups_MenuItemId] ON [CustomizationGroups] ([MenuItemId]);

CREATE INDEX [IX_CustomizationOptions_CustomizationGroupId] ON [CustomizationOptions] ([CustomizationGroupId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260417165502_AddCustomizationArchitecture', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Orders] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Orders] ADD [TotalPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [OrderItems] ADD [LineTotal] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [OrderItems] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424065151_AddOrderPaymentFlow', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[OrderItems].[UnitPrice]', N'FinalUnitPrice', 'COLUMN';

ALTER TABLE [OrderItems] ADD [BaseUnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE [OrderItemCustomizations] (
    [Id] int NOT NULL IDENTITY,
    [OrderItemId] int NOT NULL,
    [CustomizationGroupId] int NOT NULL,
    [CustomizationOptionId] int NOT NULL,
    [GroupTitle] nvarchar(max) NOT NULL,
    [OptionName] nvarchar(max) NOT NULL,
    [PriceChange] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderItemCustomizations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItemCustomizations_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_OrderItemCustomizations_OrderItemId] ON [OrderItemCustomizations] ([OrderItemId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501080231_AddSelectableCustomizationsToCartAndOrder', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [OrderItemReviews] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [OrderItemId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [MenuItemId] int NOT NULL,
    [CatererId] nvarchar(450) NOT NULL,
    [MenuRating] int NOT NULL,
    [CatererRating] int NOT NULL,
    [Comment] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderItemReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItemReviews_AspNetUsers_CatererId] FOREIGN KEY ([CatererId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_OrderItemReviews_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_OrderItemReviews_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]),
    CONSTRAINT [FK_OrderItemReviews_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItemReviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id])
);

CREATE INDEX [IX_OrderItemReviews_CatererId] ON [OrderItemReviews] ([CatererId]);

CREATE INDEX [IX_OrderItemReviews_MenuItemId] ON [OrderItemReviews] ([MenuItemId]);

CREATE INDEX [IX_OrderItemReviews_OrderId] ON [OrderItemReviews] ([OrderId]);

CREATE INDEX [IX_OrderItemReviews_OrderItemId] ON [OrderItemReviews] ([OrderItemId]);

CREATE INDEX [IX_OrderItemReviews_UserId] ON [OrderItemReviews] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501100958_AddOrderItemReviews', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [AppLogs] (
    [Id] int NOT NULL IDENTITY,
    [CreatedAt] datetime2 NOT NULL,
    [Level] nvarchar(30) NOT NULL,
    [EventType] nvarchar(80) NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [UserId] nvarchar(max) NULL,
    [UserEmail] nvarchar(256) NULL,
    [Details] nvarchar(max) NULL,
    CONSTRAINT [PK_AppLogs] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_AppLogs_CreatedAt] ON [AppLogs] ([CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501105826_AddAppLogging', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'Name');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [MenuItems] ALTER COLUMN [Name] nvarchar(120) NOT NULL;

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MenuItems]') AND [c].[name] = N'Description');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [MenuItems] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [MenuItems] ALTER COLUMN [Description] nvarchar(1000) NOT NULL;

ALTER TABLE [MenuItems] ADD [LocationText] nvarchar(250) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501134051_AddMenuLocationText', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] ADD [Latitude] float NULL;

ALTER TABLE [MenuItems] ADD [Longitude] float NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504152009_AddMenuCoordinates', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Caterers]') AND [c].[name] = N'PhoneNumber');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Caterers] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Caterers] DROP COLUMN [PhoneNumber];

ALTER TABLE [MenuItems] ADD [CatererId] int NULL;

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Caterers]') AND [c].[name] = N'Address');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Caterers] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [Caterers] ALTER COLUMN [Address] nvarchar(max) NULL;

ALTER TABLE [Caterers] ADD [ApplicationUserId] nvarchar(450) NOT NULL DEFAULT N'';

ALTER TABLE [Caterers] ADD [Latitude] float NULL;

ALTER TABLE [Caterers] ADD [Longitude] float NULL;

ALTER TABLE [AspNetUsers] ADD [Address] nvarchar(300) NULL;

ALTER TABLE [AspNetUsers] ADD [Latitude] float NULL;

ALTER TABLE [AspNetUsers] ADD [Longitude] float NULL;

CREATE INDEX [IX_MenuItems_CatererId] ON [MenuItems] ([CatererId]);

CREATE INDEX [IX_Caterers_ApplicationUserId] ON [Caterers] ([ApplicationUserId]);

ALTER TABLE [Caterers] ADD CONSTRAINT [FK_Caterers_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;

ALTER TABLE [MenuItems] ADD CONSTRAINT [FK_MenuItems_Caterers_CatererId] FOREIGN KEY ([CatererId]) REFERENCES [Caterers] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504182756_AddUserRestaurantSavedLocations', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AspNetUsers] ADD [Bio] nvarchar(500) NULL;

ALTER TABLE [AspNetUsers] ADD [ProfileImageContentType] nvarchar(100) NULL;

ALTER TABLE [AspNetUsers] ADD [ProfileImageData] varbinary(max) NULL;

ALTER TABLE [AspNetUsers] ADD [ProfileImageFileName] nvarchar(255) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504212250_AddProfileFieldsToApplicationUser', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504213123_AddUserProfileImagesAndBio', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [OrderChatMessages] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [SenderUserId] nvarchar(450) NOT NULL,
    [SenderRole] nvarchar(30) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderChatMessages_AspNetUsers_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_OrderChatMessages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_OrderChatMessages_OrderId_SentAt] ON [OrderChatMessages] ([OrderId], [SentAt]);

CREATE INDEX [IX_OrderChatMessages_SenderUserId] ON [OrderChatMessages] ([SenderUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260505203248_AddOrderChatMessages', N'10.0.6');

COMMIT;
GO

