CREATE TABLE [dbo].[City]
(
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,
    [Country] NVARCHAR(50) NOT NULL,
    [Region] NVARCHAR(50) NOT NULL,
	[Population] INT NULL,
	[Latitude] FLOAT NOT NULL,
	[Longtitude] FLOAT NOT NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL
)
GO