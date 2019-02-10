namespace WebUI.BestPath.Interfaces
{
    using SharedModels;

    public interface IDownloadCostBetweenTwoCities
    {
        (double, double) Execute(City origin, City destination);
    }
}