CREATE TABLE [dbo].[Session]
(
	[Id] INT NOT NULL PRIMARY KEY,
	[NoOfCities] INT NULL,
    [FreeDistances] NVARCHAR(MAX) NULL, 
    [TollDistances] NVARCHAR(MAX) NULL, 
    [Costs] NVARCHAR(MAX) NULL
)
