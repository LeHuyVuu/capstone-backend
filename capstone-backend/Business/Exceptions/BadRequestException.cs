namespace capstone_backend.Business.Exceptions
{
    public class BadRequestException : Exception
    {
        public string ErrorCode { get; }
        public object? Meta { get; }

        public BadRequestException(string message, string errorCode, object? meta = null)
            : base(message)
        {
            ErrorCode = errorCode;
            Meta = meta;
        }
    }
}
