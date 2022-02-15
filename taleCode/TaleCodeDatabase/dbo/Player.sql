CREATE TABLE [dbo].[Player]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[ApiId] INT NOT NULL,
	[Name] NVARCHAR(50) NULL,
	[DateOfBirth] datetime2 NULL,
    [Nationality] NVARCHAR(50) NULL,
	[Position] NVARCHAR(50) NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[ModifiedAt] datetime2 NULL
)
GO

CREATE UNIQUE INDEX [UX_Player_ApiId] ON [dbo].[Player] ([ApiId])
GO


CREATE TRIGGER [OnUpdatePlayer_SetModifiedAtToCurrentTime]
ON dbo.[Player]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON

	UPDATE
		dbo.[Player]
	SET
		ModifiedAt = GETUTCDATE()
	FROM
		dbo.[Player]
		JOIN inserted ON inserted.Id = [Player].Id

END