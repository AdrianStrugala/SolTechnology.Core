CREATE TABLE [dbo].[City]
(
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[CityId] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Country] NVARCHAR(50) NOT NULL,
	[Latitude] FLOAT NOT NULL,
	[Longitude] FLOAT NOT NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL
)
GO
