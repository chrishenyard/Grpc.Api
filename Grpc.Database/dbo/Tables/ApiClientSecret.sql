CREATE TABLE [dbo].[ApiClientSecret] (
    [ApiClientSecretInternalId] BIGINT IDENTITY (1, 1) NOT NULL,
    [ApiClientSecretId] UNIQUEIDENTIFIER NOT NULL,
    [ApiClientId]       UNIQUEIDENTIFIER NOT NULL,
    [Salt]              NVARCHAR (100)   NOT NULL,
    [Secret]            NVARCHAR (200)   NOT NULL,
    [IsCurrent]         BIT              NOT NULL,
    [CreatedUtc]        DATETIME2 (7)    NOT NULL,
    [ExpiresUtc]        DATETIME2 (7)    NULL,
    CONSTRAINT [PK_ApiClientSecret] PRIMARY KEY NONCLUSTERED ([ApiClientSecretId] ASC),
    CONSTRAINT [FK_ApiClientSecret_ApiClient_ApiClientId] FOREIGN KEY ([ApiClientId]) 
        REFERENCES [dbo].[ApiClient] ([ApiClientId]) ON DELETE CASCADE
);
GO

ALTER TABLE [dbo].[ApiClientSecret]
    ADD CONSTRAINT [DF_ApiClientSecret_ApiClientSecretId] DEFAULT NEWSEQUENTIALID() FOR [ApiClientSecretId];
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_ApiClientSecret_ApiClientId]
    ON [dbo].[ApiClientSecret]([ApiClientId] ASC);
GO

CREATE UNIQUE CLUSTERED INDEX [UX_ApiClientSecret_ApiClientSecretInternalId] 
    ON [dbo].[ApiClientSecret]([ApiClientSecretInternalId] ASC);
GO

