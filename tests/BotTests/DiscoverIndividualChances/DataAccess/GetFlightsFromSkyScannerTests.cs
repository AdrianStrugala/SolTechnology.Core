namespace DreamTravel.BotTests.DiscoverIndividualChances.DataAccess
{
    using System.Threading.Tasks;
    using Bot.DiscoverIndividualChances.DataAccess;
    using Bot.DiscoverIndividualChances.Models;
    using Xunit;

    public class GetFlightsFromSkyScannerTests
    {
        private readonly GetFlightsFromSkyScanner _sut;

        public GetFlightsFromSkyScannerTests()
        {
            _sut = new GetFlightsFromSkyScanner();
        }

        [Fact]
        public async Task Execute_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            Subscription subscription = new Subscription();
            subscription.To = "Zanzibar";
            subscription.From = "Wroclaw";
            subscription.LengthOfStay = 5;
            subscription.UserName = "test User";
            subscription.Currency = "PLN";

            // Act
            var result = await _sut.Execute(subscription);

            // Assert
            Assert.NotNull(result);
        }
    }
}
