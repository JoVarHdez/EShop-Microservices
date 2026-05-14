namespace BuildingBlocks.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message, string details) : base(message)
        {
            Details = details;
        }

        public BadRequestException(string message) : base(message)
        {
        }

        public string? Details { get; set; }
    }
}
