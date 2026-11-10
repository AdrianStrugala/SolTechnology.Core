using System.Data;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class SQLStepNamerTests
{
    private const string EFCoreSubquery = "SELECT [c2].[Id], [c2].[Name] FROM (SELECT TOP(@p) [c].[Id], [c].[Name] FROM [Cities] AS [c] "
        + "WHERE [c].[Name] = @name) AS [c2] LEFT JOIN [CityAlternativeName] AS [c0] ON [c2].[Id] = [c0].[CityId]";

    private const string HangfireEnqueue = "set xact_abort on;set nocount on;\ndeclare @jobId bigint;\nbegin tran;\n"
        + "insert into [HangFire].Job (InvocationData, Arguments, CreatedAt, ExpireAt) values (@data, @args, @createdAt, @expireAt);\n"
        + "select @jobId = scope_identity();\ncommit tran;";

    private static IEnumerable<TestCaseData> Commands()
    {
        TestCaseData Text(string? text, string expected, string scenario)
            => new TestCaseData(text, CommandType.Text, expected).SetName($"Name_{scenario}");

        yield return Text(EFCoreSubquery, "SQL SELECT Cities", "EFCoreSubquery_FirstNamedSource");
        yield return Text(HangfireEnqueue, "SQL INSERT HangFire.Job", "HangfireBatch_SetDeclareBeginTranSkipped");
        yield return Text("select top (1) Value from [HangFire].JobParameter with (forceseek) where JobId = @id and Name = @name",
            "SQL SELECT HangFire.JobParameter", "HangfireJobParameter_TableHint");
        yield return Text("insert into [HangFire].JobQueue (JobId, Queue) values (@jobId, @queue)",
            "SQL INSERT HangFire.JobQueue", "HangfireJobQueue_Insert");
        yield return Text("SET IMPLICIT_TRANSACTIONS OFF;\nSET NOCOUNT ON;\nINSERT INTO [Cities] ([Name]) OUTPUT INSERTED.[Id] VALUES (@p0);",
            "SQL INSERT Cities", "EFCoreSaveChanges_SetSkipped");
        yield return Text("-- FindCityByName\n\nselect TOP(1) [c].[Id] FROM [Cities] AS [c]", "SQL SELECT Cities", "TagWithComment_Skipped");
        yield return Text("UPDATE [dbo].[Cities] SET [Name] = @p0 WHERE [Id] = @p1", "SQL UPDATE dbo.Cities", "Update");
        yield return Text("/* tag */ DELETE FROM x WHERE Id = @id", "SQL DELETE x", "DeleteFrom");
        yield return Text("MERGE INTO dbo.Cities WITH (HOLDLOCK) AS target USING @rows AS source ON target.Id = source.Id",
            "SQL MERGE dbo.Cities", "MergeInto");
        yield return Text("EXEC @r = dbo.P @a = 1", "SQL EXEC dbo.P", "ExecWithReturnVariable");
        yield return Text("TRUNCATE TABLE [dbo].[Staging]", "SQL TRUNCATE dbo.Staging", "TruncateTable");
        yield return Text("WITH recent AS (SELECT * FROM dbo.Cities) SELECT * FROM recent", "SQL WITH", "OtherOperation_NoTarget");
        yield return Text("SELECT 1", "SQL SELECT", "SelectWithoutFrom");
        yield return Text("SELECT @@VERSION", "SQL SELECT", "SelectServerVariable");
        yield return Text("SELECT Id FROM @t", "SQL SELECT", "TableVariable_NoTarget");
        yield return Text("INSERT INTO @t VALUES (1)", "SQL INSERT", "TableVariableInsert_NoTarget");
        yield return Text("SELECT * FROM #staging", "SQL SELECT #staging", "TempTable");
        yield return Text("SELECT 1 /* FROM fake */ -- FROM fake\n; SELECT * FROM dbo.Next", "SQL SELECT", "FromInComment_Ignored");
        yield return Text("SELECT 'it''s FROM fake' AS Label FROM dbo.Real", "SQL SELECT dbo.Real", "FromInStringLiteral_Ignored");
        yield return Text("SELECT [x FROM fake] FROM dbo.Real", "SQL SELECT dbo.Real", "FromInBracketedName_Ignored");
        yield return Text("SELECT * FROM [db].[HangFire].[Job]", "SQL SELECT HangFire.Job", "ThreePartName_LastTwoParts");
        yield return Text("SELECT * FROM \"db\".`dbo`.Cities", "SQL SELECT dbo.Cities", "QuotedName_QuotesRemoved");
        yield return Text("SELECT * FROM " + new string('t', 128), "SQL SELECT " + new string('t', 128), "Target128Characters_Kept");
        yield return Text("SELECT * FROM " + new string('t', 129), "SQL SELECT", "TargetOver128Characters_NoTarget");
        yield return Text("SELECT *" + new string(' ', 4096) + "FROM dbo.Far", "SQL SELECT", "FromBeyond4096Characters_NotScanned");
        yield return Text("SET XACT_ABORT ON", "SQL SET", "SetWithoutSemicolon_IsOperation");
        yield return Text("-- only a comment", "SQL", "OnlyComment");
        yield return Text("", "SQL", "Empty");
        yield return Text("  \r\n ", "SQL", "Whitespace");
        yield return Text(null, "SQL", "Null");
        yield return new TestCaseData(" [dbo].[ImportCities] ", CommandType.StoredProcedure, "SQL EXEC dbo.ImportCities")
            .SetName("Name_StoredProcedure_ExecWithNormalizedName");
    }

    [TestCaseSource(nameof(Commands))]
    public void Name_Command_OperationAndTarget(string? commandText, CommandType commandType, string expected)
    {
        // Act
        var name = SQLStepNamer.Name(commandText, commandType);

        // Assert
        name.Should().Be(expected);
    }

    [Test]
    public void Name_AllocatesOnlyTheName()
    {
        // Arrange
        const string text = "SET NOCOUNT ON; SELECT [c].[Id] FROM [dbo].[Cities] AS [c] WHERE [c].[Name] = N'Krakow'";
        var name = SQLStepNamer.Name(text, CommandType.Text);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        SQLStepNamer.Name(text, CommandType.Text);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        name.Should().Be("SQL SELECT dbo.Cities");
        allocated.Should().BeLessThanOrEqualTo(32 + 2 * name.Length, "only the name is allocated; any other object adds 24 bytes or more");
    }
}
