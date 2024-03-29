﻿IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220806103408_EFCoreStore_Initial'
)
BEGIN
    CREATE TABLE [TenantInfo] (
        [Id] nvarchar(64) NOT NULL,
        [Website] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Identifier] nvarchar(450) NULL,
        [Name] nvarchar(max) NULL,
        [ConnectionString] nvarchar(max) NULL,
        CONSTRAINT [PK_TenantInfo] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220806103408_EFCoreStore_Initial'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConnectionString', N'Identifier', N'Name', N'State', N'Website') AND [object_id] = OBJECT_ID(N'[TenantInfo]'))
        SET IDENTITY_INSERT [TenantInfo] ON;
    EXEC(N'INSERT INTO [TenantInfo] ([Id], [ConnectionString], [Identifier], [Name], [State], [Website])
    VALUES (N''9D36C579-9D45-4ACE-8260-7673DBF53572'', N''Data Source=tcp:u3admin-server.database.windows.net,1433;Initial Catalog=U3A;User Id=u3admin-server-admin@u3admin-server.database.windows.net;Password=1JYL735FP13D1T1R$'', N''demo'', N''U3A Demonstration Only'', N''NSW'', N''https://eastlakes.u3anet.org.au/'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConnectionString', N'Identifier', N'Name', N'State', N'Website') AND [object_id] = OBJECT_ID(N'[TenantInfo]'))
        SET IDENTITY_INSERT [TenantInfo] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220806103408_EFCoreStore_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TenantInfo_Identifier] ON [TenantInfo] ([Identifier]) WHERE [Identifier] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220806103408_EFCoreStore_Initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20220806103408_EFCoreStore_Initial', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220824073049_EFCoreStore_EFCoreStore_SendGridAPIKey'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [SendGridAPIKey] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220824073049_EFCoreStore_EFCoreStore_SendGridAPIKey'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20220824073049_EFCoreStore_EFCoreStore_SendGridAPIKey', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220901022532_EFCoreStore_EmailTestEnv'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [UseEmailTestEnviroment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220901022532_EFCoreStore_EmailTestEnv'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20220901022532_EFCoreStore_EmailTestEnv', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220908050744_EFCoreStore_TwilioSMS'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [TwilioAccountSID] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220908050744_EFCoreStore_TwilioSMS'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [TwilioAuthToken] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220908050744_EFCoreStore_TwilioSMS'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [TwilioPhoneNo] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220908050744_EFCoreStore_TwilioSMS'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20220908050744_EFCoreStore_TwilioSMS', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220909035907_EFCoreStore_EFCoreStore_TwilioSMSTextEnv'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [UseSMSTestEnviroment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20220909035907_EFCoreStore_EFCoreStore_TwilioSMSTextEnv'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20220909035907_EFCoreStore_EFCoreStore_TwilioSMSTextEnv', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20221125194407_EFCoreStore_Eway'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [EwayAPIKey] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20221125194407_EFCoreStore_Eway'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [EwayPassword] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20221125194407_EFCoreStore_Eway'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [UseEwayTestEnviroment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20221125194407_EFCoreStore_Eway'
)
BEGIN
    EXEC(N'UPDATE [TenantInfo] SET [EwayAPIKey] = NULL, [EwayPassword] = NULL, [UseEwayTestEnviroment] = CAST(0 AS bit)
    WHERE [Id] = N''9D36C579-9D45-4ACE-8260-7673DBF53572'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20221125194407_EFCoreStore_Eway'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20221125194407_EFCoreStore_Eway', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230107033050_Postmark'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [PostmarkAPIKey] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230107033050_Postmark'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [PostmarkSandboxAPIKey] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230107033050_Postmark'
)
BEGIN
    ALTER TABLE [TenantInfo] ADD [UsePostmarkTestEnviroment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230107033050_Postmark'
)
BEGIN
    EXEC(N'UPDATE [TenantInfo] SET [PostmarkAPIKey] = NULL, [PostmarkSandboxAPIKey] = NULL, [UsePostmarkTestEnviroment] = CAST(0 AS bit)
    WHERE [Id] = N''9D36C579-9D45-4ACE-8260-7673DBF53572'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230107033050_Postmark'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230107033050_Postmark', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230829214618_ContactRequest'
)
BEGIN
    CREATE TABLE [ContactRequest] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [CreatedOn] datetime2 NULL,
        [UpdatedOn] datetime2 NULL,
        [User] nvarchar(max) NULL,
        CONSTRAINT [PK_ContactRequest] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230829214618_ContactRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230829214618_ContactRequest', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230829220435_ContactRequest_u3a'
)
BEGIN
    ALTER TABLE [ContactRequest] ADD [U3A] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20230829220435_ContactRequest_u3a'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230829220435_ContactRequest_u3a', N'8.0.3');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240323025122_TenantInfo_Id'
)
BEGIN
    DROP INDEX [IX_TenantInfo_Identifier] ON [TenantInfo];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240323025122_TenantInfo_Id'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TenantInfo]') AND [c].[name] = N'Identifier');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TenantInfo] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [TenantInfo] ALTER COLUMN [Identifier] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240323025122_TenantInfo_Id'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240323025122_TenantInfo_Id', N'8.0.3');
END;
GO

COMMIT;
GO

