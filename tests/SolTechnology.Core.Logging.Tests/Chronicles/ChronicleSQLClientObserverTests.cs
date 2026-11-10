using System.Data;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleSQLClientObserverTests : ChronicleSQLTestBase
{
    [TestCase("  select * from dbo.Cities where Id = @id", CommandType.Text, "SQL SELECT dbo.Cities")]
    [TestCase("dbo.GetCities", CommandType.StoredProcedure, "SQL EXEC dbo.GetCities")]
    public async Task Before_After_InsideRoot_RecordsLeafStep(string text, CommandType type, string expectedName)
    {
        // Arrange
        var (sut, _) = await StartHost();

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command(text, type), durationMs: 7);
            AmbientChronicle.Current!.Name.Should().Be("Import");
        }

        // Assert
        var leaf = SQLSteps(Listener.Completed.Single()).Should().ContainSingle().Subject;
        leaf.Name.Should().Be(expectedName);
        leaf.IsUnfinished.Should().BeFalse();
        Time.GetElapsedTime(leaf.StartTimestamp, leaf.EndTimestamp).Should().Be(TimeSpan.FromMilliseconds(7));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Error_RecordsFailedStep_MessageOnlyWithCommandTextCapture(bool captureText)
    {
        // Arrange
        var (sut, _) = await StartHost(o => (o.CaptureSQLCommandText, o.MaxAttributeValueLength) = (captureText, 25));
        const string message = "Violation of UNIQUE KEY constraint. The duplicate key value is (jan).";
        var expectedMessage = captureText ? message[..25] + "[truncated]" : null;

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command("INSERT INTO dbo.Items VALUES (@name)"), error: new InvalidOperationException(message));
        }

        // Assert
        var leaf = SQLSteps(Listener.Completed.Single()).Single();
        leaf.Status.Should().Be(ChronicleStepStatus.Failed);
        leaf.ErrorType.Should().Be(nameof(InvalidOperationException));
        leaf.ErrorMessage.Should().Be(expectedMessage);
    }

    [Test]
    public async Task Error_CaptureCommandTextOff_MessageNotRecorded()
    {
        // Arrange
        var (sut, _) = await StartHost();

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command("INSERT INTO dbo.Items VALUES (@name)"), error: new InvalidOperationException("The duplicate key value is (jan)."));
        }

        // Assert
        var leaf = SQLSteps(Listener.Completed.Single()).Single();
        leaf.ErrorMessage.Should().BeNull();
        ChronicleReportBuilder.Build(Listener.Completed.Single()).ToString().Should().NotContain("jan");
    }

    [Test]
    public async Task OutsideRoot_IsEnabled_ReturnsFalse()
    {
        // Arrange
        await StartHost();

        // Act
        var isEnabled = SqlClient.IsEnabled(ChronicleSQLClientObserver.Before);

        // Assert
        isEnabled.Should().BeFalse();
        SqlClient.IsEnabled(ChronicleSQLClientObserver.After).Should().BeFalse();
        SqlClient.IsEnabled("Microsoft.Data.SqlClient.WriteConnectionOpenBefore").Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CommandText_RecordedOnlyWhenCaptureEnabled(bool captureText)
    {
        // Arrange
        var (sut, _) = await StartHost(o => (o.CaptureSQLCommandText, o.MaxAttributeValueLength) = (captureText, 34));
        const string statement = "SELECT Name FROM dbo.Items WHERE Id = @id AND Name = 'jan'";
        var expectedStatement = captureText ? statement[..34] + "[truncated]" : null;

        // Act
        using (sut.Begin("Import"))
        {
            ExecuteCommand(Command(statement));
        }

        // Assert
        var attributes = SQLSteps(Listener.Completed.Single()).Single().SnapshotAttributes();
        attributes.FirstOrDefault(a => a.Key == "Statement").Value.Should().Be(expectedStatement);
    }

    [Test]
    public async Task FortyTwoIdenticalStatements_AggregatedAsNPlusOne()
    {
        // Arrange
        var (sut, _) = await StartHost();

        // Act
        using (sut.Begin("GET /cities"))
        {
            for (var i = 0; i < 42; i++)
            {
                ExecuteCommand(Command("SELECT * FROM dbo.Streets WHERE CityId = @id"));
            }
        }

        // Assert
        var report = ChronicleReportBuilder.Build(Listener.Completed.Single());
        report.IsNPlusOneSuspected.Should().BeTrue();
        report.ToString().Should().Contain("`- SQL SELECT dbo.Streets x42 [total 42 ms] [max 1 ms] [failed 0] N+1 suspected");
    }

    [Test]
    public async Task DifferentStatementsOnOneTable_SameNameButNotAggregatedTogether()
    {
        // Arrange
        var (sut, _) = await StartHost();

        // Act
        using (sut.Begin("Import"))
        {
            for (var i = 0; i < 3; i++)
            {
                ExecuteCommand(Command("SELECT * FROM dbo.Cities"));
                ExecuteCommand(Command("SELECT Id FROM dbo.Cities"));
            }
        }

        // Assert
        var lines = ChronicleReportBuilder.Build(Listener.Completed.Single()).Steps.Single().Children;
        lines.Should().HaveCount(2).And.OnlyContain(l => l.Name == "SQL SELECT dbo.Cities" && l.Count == 3);
    }

    [Test]
    public async Task CaptureDisabled_NoSubscription()
    {
        // Arrange
        var (sut, _) = await StartHost(o => o.CaptureSQLCommands = false);

        // Act
        using (sut.Begin("Import"))
        {
            SqlClient.IsEnabled(ChronicleSQLClientObserver.Before).Should().BeFalse();
            ExecuteCommand(Command("SELECT 1"));
        }

        // Assert
        SQLSteps(Listener.Completed.Single()).Should().BeEmpty();
    }
}
