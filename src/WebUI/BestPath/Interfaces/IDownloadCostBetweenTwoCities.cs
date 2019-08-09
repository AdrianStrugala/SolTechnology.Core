namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using System.Threading.Tasks;
    using Contract;

    public interface IDownloadCostBetweenTwoCities
    {
        Task<(double, double)> Execute(City origin, City destination);
    }
}