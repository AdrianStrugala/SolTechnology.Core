CREATE TABLE [dbo].[Team]
(
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[PlayerApiId] INT NOT NULL,
	[DateFrom] DATETIME2 NOT NULL, 
	[DateTo] DATETIME2 NOT NULL, 
	[Name] NVARCHAR(50) NOT NULL, 

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL

    CONSTRAINT [FK_Team_Player] FOREIGN KEY ([PlayerApiId]) REFERENCES [Player]([ApiId])
)
GO

CREATE TRIGGER [OnUpdateTeam_SetModifiedAtToCurrentTime]
ON dbo.[Team]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON

	UPDATE
		dbo.[Team]
	SET
        UpdatedAt = GETUTCDATE()
	FROM
		dbo.[Team]
		JOIN inserted ON inserted.Id = [Team].Id

END