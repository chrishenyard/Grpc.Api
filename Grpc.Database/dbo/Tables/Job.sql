CREATE TABLE [dbo].[Job] (
    [JobInternalId] BIGINT IDENTITY (1, 1) NOT NULL,
    [JobId]       UNIQUEIDENTIFIER NOT NULL,
    [Name]        NVARCHAR (100)   NOT NULL,
    [Description] NVARCHAR (200)   NOT NULL,
    [IsActive]    BIT              NOT NULL,
    [CreatedUtc]  DATETIME2 (7)    NOT NULL,
    CONSTRAINT [PK_Job] PRIMARY KEY NONCLUSTERED ([JobId] ASC)
);
GO

ALTER TABLE [dbo].[Job]
    ADD CONSTRAINT [DF_Job_JobId] DEFAULT NEWSEQUENTIALID() FOR [JobId];
GO

ALTER TABLE [dbo].[Job]
    ADD CONSTRAINT [DF_Job_IsActive] DEFAULT (1) FOR [IsActive];
GO

CREATE UNIQUE CLUSTERED INDEX [UX_Job_JobInternalId] 
    ON [dbo].[Job]([JobInternalId] ASC);
GO

