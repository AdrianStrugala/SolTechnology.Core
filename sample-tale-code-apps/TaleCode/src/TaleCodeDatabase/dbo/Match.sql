CREATE TABLE [dbo].[Match]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[ApiId] INT NOT NULL,
	[PlayerApiId] INT NOT NULL,
	[Date] DATETIME2 NULL, 
	[HomeTeam] NVARCHAR(50) NULL, 
	[AwayTeam] NVARCHAR(50) NULL, 
	[HomeTeamScore] INT NULL,
	[AwayTeamScore] INT NULL,
	[Winner] NVARCHAR(50) NOT NULL, 
	[CompetitionWinner] NVARCHAR(50) NOT NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL

    CONSTRAINT [FK_Match_Player] FOREIGN KEY ([PlayerApiId]) REFERENCES [Player]([ApiId])
)
GO

CREATE UNIQUE INDEX [UX_Match_ApiId] ON [dbo].[Match] ([ApiId])
GO


CREATE TRIGGER [OnUpdateMatch_SetModifiedAtToCurrentTime]
ON dbo.[Match]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON

	UPDATE
		dbo.[Match]
	SET
        UpdatedAt = GETUTCDATE()
	FROM
		dbo.[Match]
		JOIN inserted ON inserted.Id = [Match].Id

END