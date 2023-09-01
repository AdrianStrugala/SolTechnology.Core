namespace SolTechnology.Core.CQRS
{
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static Result Failed(string message)
        {
            return new Result
            {
                Message = message,
                Success = false
            };
        }

        public static Result Succeeded()
        {
            return new Result
            {
                Success = true
            };
        }
    }

    public class Result<T> : Result
    {
        public T Data { get; set; }


        public static Result<T> Succeeded(T data)
        {
            return new Result<T>
            {
                Success = true,
                Data = data
            };
        }

        public static Result<T> Failed(string message)
        {
            return new Result<T>
            {
                Message = message,
                Success = false
            };
        }
    }
}
