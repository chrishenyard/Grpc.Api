IF OBJECT_ID('dbo.ApiClientSecret') IS NOT NULL
BEGIN
	IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = 'IsCurrent' AND Object_ID = Object_ID('dbo.ApiClientSecret'))
	BEGIN
		ALTER TABLE dbo.ApiClientSecret DROP COLUMN IsCurrent
	END
END
GO
