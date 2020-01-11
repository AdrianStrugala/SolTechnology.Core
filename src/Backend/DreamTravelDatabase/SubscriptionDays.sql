CREATE TABLE [dbo].[SubscriptionDays]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [FlightEmailSubscriptionId] BIGINT NOT NULL,
	[Monday] BIT NOT NULL DEFAULT 0,
	[Tuesday] BIT NOT NULL DEFAULT 0,
	[Wednesday] BIT NOT NULL DEFAULT 0,
	[Thursday] BIT NOT NULL DEFAULT 0,
	[Friday] BIT NOT NULL DEFAULT 0,
	[Saturday] BIT NOT NULL DEFAULT 0,
	[Sunday] BIT NOT NULL DEFAULT 0

	CONSTRAINT [FK_SubscriptionDays_FlightEmailSubscription] FOREIGN KEY ([FlightEmailSubscriptionId]) REFERENCES [FlightEmailSubscription]([Id]),  
)
