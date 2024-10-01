CREATE TABLE [dbo].[User]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
	[Password] NVARCHAR(50) NOT NULL DEFAULT 'Password',
    [Email] NVARCHAR(50) NOT NULL UNIQUE,
	[IsActive] BIT NOT NULL DEFAULT 0,
	[Currency] NVARCHAR(5) NULL DEFAULT 'EUR',

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL
)
GO

CREATE UNIQUE INDEX [UX_User_UserId] ON [dbo].[User] ([UserId])
GO


CREATE TRIGGER [OnUpdateUser_SetUpdatedAtToCurrentTime]
ON dbo.[User]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON

	UPDATE
		dbo.[User]
	SET
		UpdatedAt = GETUTCDATE()
	FROM
		dbo.[User]
		JOIN inserted ON inserted.Id = [User].Id

END