namespace SolTechnology.Core.CQRS
{
    public interface IService<in TInput, TOutcome>
    {
        public Task<Outcome<TOutcome>> Execute(TInput input);
    }

    public class Outcome<T>
    {
        private T _value;
        private Exception _exception;

        public Exception Exception => _exception;
        public T Value => _value;
        public bool IsSuccess => _exception != null;

        public static Outcome<T> Succeeded(T result)
        {
            return new Outcome<T>
            {
                _value = result
            };
        }

        public static Outcome<T> Failed(Exception exception)
        {
            return new Outcome<T>
            {
                _exception = exception
            };
        }
    }
}
