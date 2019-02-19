namespace DreamTravel.WebUI.NameOfCity.Interfaces
{
    using System.Threading.Tasks;
    using SharedModels;

    public interface IFindNameOfCity
    {
        Task<City> Execute(City city);
    }
}