USE [master]
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Grpc')
BEGIN
    CREATE DATABASE Grpc;
    PRINT 'Created Grpc';
END;
GO

USE [Grpc]
GO

IF NOT EXISTS (SELECT OBJECT_ID from sys.objects where OBJECT_ID = OBJECT_ID(N'[dbo].[ApiClient]') and type = 'U')
BEGIN
	CREATE TABLE [dbo].[ApiClient](
		[ApiClientId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT (NEWID()) NOT NULL,
		[ApiKey] [VARCHAR](100) NOT NULL,
		[ClientName] [VARCHAR](200) NOT NULL,
		[IsActive] [BIT] NOT NULL,
		[CreatedUtc] [DATETIME2] NOT NULL
	) 
    PRINT 'Created ApiClient';
END;
GO

IF NOT EXISTS (SELECT OBJECT_ID from sys.objects where OBJECT_ID = OBJECT_ID(N'[dbo].[ApiClientSecret]') and type = 'U')
BEGIN
	CREATE TABLE [dbo].[ApiClientSecret](
		[ApiClientSecretId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT (NEWID()) NOT NULL,
		[ApiClientId] UNIQUEIDENTIFIER NOT NULL,
		[Secret] [VARCHAR](200) NOT NULL,
		[IsCurrent] [BIT] NOT NULL,
		[CreatedUtc] [DATETIME2] NOT NULL,
		[ExpiresUtc] [DATETIME2] NULL
	) 
    PRINT 'Created ApiClientSecret';
END;
GO

IF NOT EXISTS (Select * FROM sys.objects WHERE Name = 'FK_ApiClientSecret_ApiClient_ApiClientId')
BEGIN
	ALTER TABLE [dbo].[ApiClientSecret]  WITH CHECK ADD  CONSTRAINT [FK_ApiClientSecret_ApiClient_ApiClientId] FOREIGN KEY([ApiClientId])
	REFERENCES [dbo].[ApiClient] ([ApiClientId])
	ON DELETE CASCADE

	ALTER TABLE [dbo].[ApiClientSecret] CHECK CONSTRAINT [FK_ApiClientSecret_ApiClient_ApiClientId]
    PRINT 'Created FK_ApiClientSecret_ApiClient_ApiClientId';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('ApiClient') AND name = 'IX_ApiClient_ApiKey')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ApiClient_ApiKey
    ON ApiClient (ApiKey ASC)
    PRINT 'Created IX_ApiClient_ApiKey';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('ApiClientSecret') AND name = 'IX_ApiClientSecret_ApiClientId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApiClientSecret_ApiClientId
    ON ApiClientSecret (ApiClientId ASC)
    PRINT 'Created IX_ApiClientSecret_ApiClientId';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('ApiClientSecret') AND name = 'IX_ApiClientSecret_ApiClientId_IsCurrent')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ApiClientSecret_ApiClientId_IsCurrent
    ON ApiClientSecret (ApiClientId ASC, IsCurrent ASC)
	WHERE IsCurrent = 1
    PRINT 'Created IX_ApiClientSecret_ApiClientId_IsCurrent';
END;
GO

DECLARE @ApiClientId UNIQUEIDENTIFIER = '5E59ED97-FD8C-472A-B41D-39C595F063C0';
IF NOT EXISTS (SELECT 1 FROM [dbo].[ApiClient] WHERE ApiKey = 'test-client-001')
BEGIN
	INSERT INTO [dbo].[ApiClient] ([ApiClientId], [ApiKey], [ClientName], [IsActive], [CreatedUtc])
	VALUES (@ApiClientId, 'test-client-001', 'Sample Test Client', 1, '2025-01-01' )
    PRINT 'ApiClient seed record';
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ApiClientSecret] WHERE ApiClientId = @ApiClientId AND [Secret] = 'test-secret-001' AND IsCurrent = 1)
BEGIN
	INSERT INTO [dbo].[ApiClientSecret] ([ApiClientSecretId], [ApiClientId], [Secret], [IsCurrent], [CreatedUtc], [ExpiresUtc]) 
	VALUES (NEWID(), @ApiClientId, 'test-secret-001', 1, '2025-01-01', NULL)
    PRINT 'ApiClientSecret seed record';
END;
GO
