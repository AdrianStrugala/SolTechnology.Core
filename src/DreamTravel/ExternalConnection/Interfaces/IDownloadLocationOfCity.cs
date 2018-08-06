using System.Threading.Tasks;
using DreamTravel.Models;

namespace DreamTravel.ExternalConnection
{
    public interface IDownloadLocationOfCity
    {
        Task<City> Execute(string cityName);
    }
}