using System.Data;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class SQLClientPayloadReaderTests
{
    [TestCase("select * from Cities", "SQL SELECT")]
    [TestCase("SET IMPLICIT_TRANSACTIONS OFF;\nSET NOCOUNT ON;\nINSERT INTO [Cities] ([Name]) OUTPUT INSERTED.[Id] VALUES (@p0);", "SQL INSERT")]
    [TestCase("SET NOCOUNT ON;\r\nUPDATE [Cities] SET [Name] = @p0;", "SQL UPDATE")]
    [TestCase("-- FindCityByName\n\nSELECT TOP(1) [c].[Id] FROM [Cities] AS [c]", "SQL SELECT")]
    [TestCase("/* tag */ /* another */ DELETE FROM Cities", "SQL DELETE")]
    [TestCase("SET XACT_ABORT ON", "SQL SET")]
    [TestCase("-- only a comment", "SQL")]
    [TestCase("  ", "SQL")]
    public void StepName_TextCommand_NamedAfterFirstStatementKeyword(string commandText, string expected)
    {
        // Arrange
        var command = Substitute.For<IDbCommand>();
        command.CommandText.Returns(commandText);
        command.CommandType.Returns(CommandType.Text);

        // Act
        var name = SQLClientPayloadReader.StepName(command);

        // Assert
        name.Should().Be(expected);
    }

    [Test]
    public void StepName_StoredProcedure_NamedAfterProcedure()
    {
        // Arrange
        var command = Substitute.For<IDbCommand>();
        command.CommandText.Returns(" dbo.ImportCities ");
        command.CommandType.Returns(CommandType.StoredProcedure);

        // Act
        var name = SQLClientPayloadReader.StepName(command);

        // Assert
        name.Should().Be("SQL dbo.ImportCities");
    }
}
