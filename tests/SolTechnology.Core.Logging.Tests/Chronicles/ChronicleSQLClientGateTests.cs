using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles.SQL;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleSQLClientGateTests : ChronicleTestBase
{
    [TestCase(ChronicleSQLClientObserver.Before)]
    [TestCase(ChronicleSQLClientObserver.After)]
    [TestCase(ChronicleSQLClientObserver.Error)]
    public void OnNext_NoOpenRootAndNothingPending_NeverReadsPayload(string eventName)
    {
        // Arrange
        var sut = new ChronicleSQLClientObserver(CreateRecorder().Writer);
        var payload = new CountingPayload();

        // Act
        sut.OnNext(new KeyValuePair<string, object?>(eventName, payload));

        // Assert
        payload.Reads.Should().Be(0);
    }

    [Test]
    public void OnNext_BeforeInsideOwnRoot_ReadsPayload()
    {
        // Arrange
        var recorder = CreateRecorder();
        var sut = new ChronicleSQLClientObserver(recorder.Writer);
        var payload = new CountingPayload();

        // Act
        using (recorder.Begin("Import"))
        {
            sut.OnNext(new KeyValuePair<string, object?>(ChronicleSQLClientObserver.Before, payload));
        }

        // Assert
        payload.Reads.Should().BePositive();
    }

    private sealed class CountingPayload : IReadOnlyList<KeyValuePair<string, object>>
    {
        public int Reads { get; private set; }

        public int Count
        {
            get
            {
                Reads++;
                return 0;
            }
        }

        public KeyValuePair<string, object> this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            Reads++;
            return Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
