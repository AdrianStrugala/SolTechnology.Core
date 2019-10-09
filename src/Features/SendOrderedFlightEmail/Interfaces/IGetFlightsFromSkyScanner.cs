using System.Threading.Tasks;
using DreamTravel.Bot.DiscoverIndividualChances.Models;
using DreamTravel.Features.SendOrderedFlightEmail.Models;

namespace DreamTravel.Features.SendOrderedFlightEmail.Interfaces
{
    public interface IGetFlightsFromSkyScanner
    {
        Task<Chance> Execute(Subscription subscription);
    }
}