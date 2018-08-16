using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface IDownloadCityNameByLocation
    {
        Task<City> Execute(City city);
    }
}