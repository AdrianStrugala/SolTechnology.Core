CREATE TABLE [dbo].[CityStatistics]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [CityId] BIGINT NOT NULL,
    [SearchCount] INT NOT NULL DEFAULT 0,
    );
GO

ALTER TABLE [dbo].[CityStatistics]
    ADD CONSTRAINT FK_CityStatistics_City
    FOREIGN KEY (CityId)
    REFERENCES [dbo].[City](Id);
GO
