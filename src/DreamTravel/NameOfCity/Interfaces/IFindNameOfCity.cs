using System.Threading.Tasks;
using DreamTravel.SharedModels;

namespace DreamTravel.NameOfCity.Interfaces
{
    public interface IFindNameOfCity
    {
        Task<City> Execute(City city);
    }
}