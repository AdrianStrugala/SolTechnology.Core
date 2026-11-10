using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleConcurrencyTests : ChronicleTestBase
{
    [Test]
    public async Task WhenAll_ParallelBranches_BuildsIsolatedSubtrees()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Root"))
        {
            await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(async () =>
            {
                using var branch = sut.Step($"Branch-{i}");
                await Task.Yield();
                using var middle = sut.Step($"Middle-{i}");
                await Task.Delay(1);
                using var leaf = sut.Step($"Leaf-{i}");
                await Task.Yield();
            })));
        }

        // Assert
        var root = Listener.Completed.Single();
        var branches = root.Node.SnapshotChildren();
        branches.Should().HaveCount(64);
        foreach (var branch in branches)
        {
            var index = branch.Name["Branch-".Length..];
            var middle = branch.SnapshotChildren().Should().ContainSingle().Subject;
            middle.Name.Should().Be($"Middle-{index}");
            var leaf = middle.SnapshotChildren().Should().ContainSingle().Subject;
            leaf.Name.Should().Be($"Leaf-{index}");
            leaf.SnapshotChildren().Should().BeEmpty();
            new[] { branch, middle, leaf }.Should().OnlyContain(n => n.IsClosed && !n.IsUnfinished);
        }
    }

    [Test]
    public void ParallelAppends_ToOneParent_LoseNoChildren()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Root"))
        {
            Parallel.For(0, 1000, _ =>
            {
                using var step = sut.Step("Append");
            });
        }

        // Assert
        Listener.Completed.Single().Node.SnapshotChildren().Should().HaveCount(1000);
    }

    [Test]
    public async Task ParallelSet_OnSharedNode_NoExceptionAndNoLostKeys()
    {
        // Arrange
        var sut = CreateRecorder();

        // Act
        using (sut.Begin("Root"))
        {
            await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(async () =>
            {
                await Task.Yield();
                sut.Current.Set($"Key-{i}", i);
            })));
        }

        // Assert
        var attributes = Listener.Completed.Single().Node.SnapshotAttributes();
        attributes.Select(a => a.Key).Should().BeEquivalentTo(Enumerable.Range(0, 64).Select(i => $"Key-{i}"));
    }
}
