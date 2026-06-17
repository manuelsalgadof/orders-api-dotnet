namespace OrdersApi.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private const int MaxCorrelationIdLength = 128;

        private static bool IsValidCorrelationId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (value.Length > MaxCorrelationIdLength) return false;
            foreach (var c in value)
                if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;
            return true;
        }

        public async Task Invoke(HttpContext context)
        {
            var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = IsValidCorrelationId(incoming)
                ? incoming!
                : Guid.NewGuid().ToString();

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            await _next(context);
        }
    }
}
