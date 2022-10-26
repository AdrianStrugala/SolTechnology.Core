namespace SolTechnology.Core.CQRS
{
    public interface IService<in TInput, TOutcome>
    {
        public Task<ServiceOutcome<TOutcome>> Execute(TInput input);
    }

    public class ServiceOutcome<T>
    {
        private T _value;
        private Exception _exception;

        public Exception Exception => _exception;
        public T Value => _value;
        public bool IsSuccess => _exception != null;

        public static ServiceOutcome<T> Succeeded(T result)
        {
            return new ServiceOutcome<T>
            {
                _value = result
            };
        }

        public static ServiceOutcome<T> Failed(Exception exception)
        {
            return new ServiceOutcome<T>
            {
                _exception = exception
            };
        }
    }
}
