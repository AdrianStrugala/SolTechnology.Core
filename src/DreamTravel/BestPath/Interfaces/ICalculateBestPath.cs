namespace DreamTravel.BestPath.Interfaces
{
    using System.Threading.Tasks;

    public interface ICalculateBestPath
    {
        Task<Result> Execute(Command command);
    }
}