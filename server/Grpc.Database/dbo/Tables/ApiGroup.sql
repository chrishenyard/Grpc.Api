CREATE SEQUENCE dbo.ApiGroupId_Seq
    AS INT
    START WITH 1
    INCREMENT BY 1
    NO CYCLE
    CACHE 100;
GO

CREATE TABLE [dbo].[ApiGroup]
(
    [ApiGroupId] INT NOT NULL,
    [GroupName] VARCHAR(50) NOT NULL,
    CONSTRAINT [PK_ApiGroup] PRIMARY KEY CLUSTERED ([ApiGroupId] ASC)
);
GO

CREATE UNIQUE INDEX [UX_ApiGroup_GroupName] ON [dbo].[ApiGroup] ([GroupName] ASC);
GO

ALTER TABLE dbo.ApiGroup
ADD CONSTRAINT DF_ApiGroup_ApiGroupId
    DEFAULT (NEXT VALUE FOR dbo.ApiGroupId_Seq)
    FOR ApiGroupId;
GO
