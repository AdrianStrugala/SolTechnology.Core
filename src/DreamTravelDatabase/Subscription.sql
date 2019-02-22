CREATE TABLE [dbo].[Subscription]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] INT NOT NULL, 
	[From] NVARCHAR(50) NOT NULL, 
	[To] NVARCHAR(50) NOT NULL, 
	[NoOfMonthsFromNow] INT NOT NULL,
	CONSTRAINT [FK_Subscription_User] FOREIGN KEY ([UserId]) REFERENCES [User]([Id])
)
