using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleAllocationTests : ChronicleTestBase
{
    [Test]
    public void Step_ChronicleDisabled_AllocatesNothing()
    {
        // Arrange
        var sut = CreateRecorder(o => o.Enabled = false);
        using (sut.Begin("Root"))
        {
            // Act
            var allocated = MeasureSteps(() =>
            {
                using var step = sut.Step("Disabled");
                step.Set("Key", null);
            });

            // Assert
            allocated.Should().Be(0);
        }
    }

    [Test]
    public void Step_EnabledNoRoot_AllocatesNothing()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        var allocated = MeasureSteps(() =>
        {
            using var step = sut.Step("NoRoot");
            step.Set("Key", null);
        });

        // Assert
        allocated.Should().Be(0);
    }

    private static long MeasureSteps(Action step)
    {
        for (var i = 0; i < 100; i++)
        {
            step();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            step();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
