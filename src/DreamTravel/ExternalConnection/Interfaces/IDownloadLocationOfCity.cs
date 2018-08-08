using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection.Interfaces
{
    public interface IDownloadLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}