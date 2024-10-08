﻿CREATE TABLE [dbo].[ExecutionError]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[ReferenceType] NVARCHAR(50) NOT NULL,
	[ReferenceId] INT NOT NULL,
	[Message] NVARCHAR(MAX) NULL,
	[Valid] BIT NOT NULL,

	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL
)
GO


CREATE TRIGGER [OnUpdateExecutionError_SetModifiedAtToCurrentTime]
ON dbo.[ExecutionError]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON

	UPDATE
		dbo.[ExecutionError]
	SET
        UpdatedAt = GETUTCDATE()
	FROM
		dbo.[ExecutionError]
		JOIN inserted ON inserted.Id = [ExecutionError].Id

END