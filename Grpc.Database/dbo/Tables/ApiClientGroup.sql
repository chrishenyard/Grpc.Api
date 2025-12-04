CREATE TABLE [dbo].[ApiClientGroup]
(
    [ApiClientId] UNIQUEIDENTIFIER NOT NULL,
    [ApiGroupId] INT NOT NULL,
    CONSTRAINT [PK_ApiClientGroup] PRIMARY KEY CLUSTERED ([ApiClientId] ASC, [ApiGroupId] ASC),
    CONSTRAINT [FK_ApiClientGroup_ApiClient_ApiClientId] FOREIGN KEY ([ApiClientId]) 
        REFERENCES [dbo].[ApiClient] ([ApiClientId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ApiClientGroup_ApiGroup_ApiGroupId] FOREIGN KEY ([ApiGroupId]) 
        REFERENCES [dbo].[ApiGroup] ([ApiGroupId]) ON DELETE CASCADE
);
GO

