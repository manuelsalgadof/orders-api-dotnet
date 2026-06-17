using Microsoft.AspNetCore.Http;
using OrdersApi.Middlewares;

namespace OrdersApi.Tests.Middlewares
{
    public class CorrelationIdMiddlewareTests
    {
        private const string HeaderName = "X-Correlation-ID";

        private static async Task<HttpContext> RunMiddleware(string? correlationIdHeader = null)
        {
            var context = new DefaultHttpContext();

            if (correlationIdHeader != null)
                context.Request.Headers[HeaderName] = correlationIdHeader;

            var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
            await middleware.Invoke(context);

            return context;
        }

        // ─── Sin header entrante ───────────────────────────────────────────────

        [Fact]
        public async Task Invoke_NoHeader_GeneratesValidUuid()
        {
            var context = await RunMiddleware();

            var correlationId = context.Items["CorrelationId"] as string;

            Assert.NotNull(correlationId);
            Assert.True(Guid.TryParse(correlationId, out _),
                $"Se esperaba un UUID válido, se obtuvo: '{correlationId}'");
        }

        [Fact]
        public async Task Invoke_NoHeader_WritesResponseHeader()
        {
            var context = await RunMiddleware();

            Assert.True(context.Response.Headers.ContainsKey(HeaderName));
            var responseHeader = context.Response.Headers[HeaderName].ToString();
            Assert.NotEmpty(responseHeader);
        }

        // ─── Header válido entrante ────────────────────────────────────────────

        [Fact]
        public async Task Invoke_ValidHeader_UsesIncomingValue()
        {
            const string incoming = "abc-123_xyz";
            var context = await RunMiddleware(incoming);

            Assert.Equal(incoming, context.Items["CorrelationId"] as string);
            Assert.Equal(incoming, context.Response.Headers[HeaderName].ToString());
        }

        [Fact]
        public async Task Invoke_ValidHeaderExactly128Chars_AcceptsIt()
        {
            // 128 chars exactos — límite máximo permitido
            var incoming = new string('a', 128);
            var context  = await RunMiddleware(incoming);

            Assert.Equal(incoming, context.Items["CorrelationId"] as string);
        }

        // ─── Header inválido → genera nuevo UUID ──────────────────────────────

        [Fact]
        public async Task Invoke_Header129Chars_GeneratesNewUuid()
        {
            // 129 chars — excede el límite → se rechaza
            var incoming = new string('a', 129);
            var context  = await RunMiddleware(incoming);

            var correlationId = context.Items["CorrelationId"] as string;

            Assert.NotNull(correlationId);
            Assert.NotEqual(incoming, correlationId);
            Assert.True(Guid.TryParse(correlationId, out _));
        }

        [Fact]
        public async Task Invoke_EmptyHeader_GeneratesNewUuid()
        {
            var context       = await RunMiddleware(string.Empty);
            var correlationId = context.Items["CorrelationId"] as string;

            Assert.NotNull(correlationId);
            Assert.True(Guid.TryParse(correlationId, out _));
        }

        [Fact]
        public async Task Invoke_HeaderWithSpace_GeneratesNewUuid()
        {
            // Espacio no es un char ASCII válido para correlationId
            var context       = await RunMiddleware("abc def");
            var correlationId = context.Items["CorrelationId"] as string;

            Assert.NotNull(correlationId);
            Assert.NotEqual("abc def", correlationId);
            Assert.True(Guid.TryParse(correlationId, out _));
        }

        // ─── Items["CorrelationId"] siempre se setea ──────────────────────────

        [Fact]
        public async Task Invoke_Always_SetsCorrelationIdInItems()
        {
            var context = await RunMiddleware();

            Assert.True(context.Items.ContainsKey("CorrelationId"),
                "context.Items debe contener 'CorrelationId'");
            Assert.NotNull(context.Items["CorrelationId"]);
        }

        [Fact]
        public async Task Invoke_ValidHeader_SetsCorrelationIdInItemsAndResponseHeader()
        {
            const string incoming = "valid-id_123";
            var context  = await RunMiddleware(incoming);

            Assert.Equal(incoming, context.Items["CorrelationId"] as string);
            Assert.Equal(incoming, context.Response.Headers[HeaderName].ToString());
        }
    }
}
