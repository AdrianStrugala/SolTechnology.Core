CREATE TABLE [dbo].[City]
(
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[CityId] BIGINT NOT NULL,
    [Country] NVARCHAR(50) NOT NULL,
	[Latitude] FLOAT NOT NULL,
	[Longitude] FLOAT NOT NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL
)
GO

CREATE UNIQUE INDEX [UX_City_CityId] ON [dbo].[City] ([CityId])
GO
