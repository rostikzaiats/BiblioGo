namespace BiblioGo.Application.Common
{
    // Результат операції, що при успіху повертає значення (створюється через Result.Success / Result.Failure)
    public class Result<T> : Result
    {
        private readonly T? _value;

        internal Result(T? value, bool isSuccess, string error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        // Значення доступне лише для успішного результату, щоб помилку не можна було випадково проігнорувати
        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Неуспішний результат не має значення. Перевірте IsSuccess перед зверненням до Value.");
    }
}
