CREATE TABLE [dbo].[FlightEmailSubscription]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[UserId] INT NOT NULL, 
	[From] NVARCHAR(50) NOT NULL, 
	[To] NVARCHAR(50) NOT NULL, 
	[DepartureDate] DATETIME2 NOT NULL, 
	[ArrivalDate] DATETIME2 NOT NULL, 
	[MinDaysOfStay] INT NOT NULL,
	[MaxDaysOfStay] INT NOT NULL,
	[OneWay] BIT NOT NULL default 0

    CONSTRAINT [FK_FlightEmailSubscription_User] FOREIGN KEY ([UserId]) REFERENCES [User]([Id])
)
