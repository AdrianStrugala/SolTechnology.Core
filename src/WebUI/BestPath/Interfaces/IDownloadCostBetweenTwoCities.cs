namespace DreamTravel.WebUI.BestPath.Interfaces
{
    using Contract;

    public interface IDownloadCostBetweenTwoCities
    {
        (double, double) Execute(City origin, City destination);
    }
}