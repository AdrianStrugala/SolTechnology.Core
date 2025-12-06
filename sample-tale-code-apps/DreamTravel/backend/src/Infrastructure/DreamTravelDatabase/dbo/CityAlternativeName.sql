CREATE TABLE [dbo].[CityAlternativeName]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [CityId] BIGINT NOT NULL,
    [AlternativeName] NVARCHAR(100) NOT NULL,

    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] datetime2 NULL
    
    CONSTRAINT FK_CityAlternativeName_City
    FOREIGN KEY ([CityId]) REFERENCES [dbo].[City]([Id])
    ON DELETE CASCADE,

    INDEX IX_CityAlternativeName_CityId ([CityId])
)
    GO