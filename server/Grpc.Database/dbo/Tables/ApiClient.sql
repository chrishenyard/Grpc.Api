CREATE TABLE [dbo].[ApiClient] (
    [ApiClientInternalId] BIGINT IDENTITY (1, 1) NOT NULL,
    [ApiClientId] UNIQUEIDENTIFIER NOT NULL,
    [ApiKey]      NVARCHAR (100)   NOT NULL,
    [ClientName]  NVARCHAR (200)   NOT NULL,
    [IsActive]    BIT              NOT NULL,
    [CreatedUtc]  DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_ApiClient] PRIMARY KEY NONCLUSTERED ([ApiClientId] ASC)
);
GO

ALTER TABLE [dbo].[ApiClient]
    ADD CONSTRAINT [DF_ApiClient_ApiClientId] DEFAULT NEWSEQUENTIALID() FOR [ApiClientId];
GO

ALTER TABLE [dbo].[ApiClient]
    ADD CONSTRAINT [UQ_ApiClient_ApiKey] UNIQUE ([ApiKey] ASC);
GO

CREATE UNIQUE CLUSTERED INDEX [UX_ApiClient_ApiClientInternalId] 
    ON [dbo].[ApiClient]([ApiClientInternalId] ASC);
GO
