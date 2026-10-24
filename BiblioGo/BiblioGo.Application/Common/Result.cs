namespace BiblioGo.Application.Common
{
    // Result патерн: замість винятку при помилці валідації сервіс повертає результат з текстом помилки.
    // Для методів без значення — Result, для методів, що повертають дані, — Result<T>
    public class Result
    {
        protected Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        // Текст помилки для користувача; порожній рядок, якщо операція успішна
        public string Error { get; }

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Failure(string error)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error);
            return new Result(false, error);
        }

        public static Result<T> Success<T>(T value)
        {
            return new Result<T>(value, true, string.Empty);
        }

        public static Result<T> Failure<T>(string error)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error);
            return new Result<T>(default, false, error);
        }
    }
}
