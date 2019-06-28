namespace DreamTravel.WebUI.NameOfCity.Interfaces
{
    using System.Threading.Tasks;
    using Contract;

    public interface IFindNameOfCity
    {
        Task<City> Execute(City city);
    }
}