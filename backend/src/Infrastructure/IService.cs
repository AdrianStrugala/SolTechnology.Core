namespace DreamTravel.Infrastructure
{
    public interface IService<in TInput, out TOutput>
    {
        public TOutput Execute(TInput command);
    }
}
