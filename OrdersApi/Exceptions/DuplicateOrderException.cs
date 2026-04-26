namespace OrdersApi.Exceptions
{
    public class DuplicateOrderException : Exception
    {
        public DuplicateOrderException(string message) : base(message)
        {
        }
    }

}
