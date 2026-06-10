using OrdersApi.Services;

namespace OrdersApi.Tests.Services
{
    public class PasswordHasherServiceTests
    {
        private readonly PasswordHasherService _service = new();

        [Fact]
        public void Hash_ReturnsVersionedFormat()
        {
            var hash  = _service.Hash("password123");
            var parts = hash.Split('.');

            Assert.Equal(4,              parts.Length);
            Assert.Equal("PBKDF2-SHA256", parts[0]);
            Assert.Equal("600000",        parts[1]);
            Assert.False(string.IsNullOrWhiteSpace(parts[2]));
            Assert.False(string.IsNullOrWhiteSpace(parts[3]));
        }

        [Fact]
        public void Hash_GeneratesDifferentSaltsEachCall()
        {
            var h1 = _service.Hash("same-password");
            var h2 = _service.Hash("same-password");

            Assert.NotEqual(h1, h2);
        }

        [Fact]
        public void Verify_ReturnsTrue_ForValidHash()
        {
            var hash = _service.Hash("MyPassword1!");

            Assert.True(_service.Verify("MyPassword1!", hash));
        }

        [Fact]
        public void Verify_ReturnsFalse_ForWrongPassword()
        {
            var hash = _service.Hash("CorrectPassword!");

            Assert.False(_service.Verify("WrongPassword!", hash));
        }

        [Fact]
        public void Verify_ReturnsFalse_ForMalformedHash()
        {
            Assert.False(_service.Verify("password", "not-a-valid-hash"));
            Assert.False(_service.Verify("password", "only.two.parts"));
            Assert.False(_service.Verify("password", "a.b.c"));
        }

        [Fact]
        public void Verify_ReturnsFalse_ForUnsupportedAlgorithm()
        {
            var valid  = _service.Hash("password");
            var parts  = valid.Split('.');
            var tampered = $"MD5.{parts[1]}.{parts[2]}.{parts[3]}";

            Assert.False(_service.Verify("password", tampered));
        }

        [Fact]
        public void Verify_ReturnsFalse_ForInvalidIterations()
        {
            var valid = _service.Hash("password");
            var parts = valid.Split('.');

            Assert.False(_service.Verify("password", $"PBKDF2-SHA256.0.{parts[2]}.{parts[3]}"));
            Assert.False(_service.Verify("password", $"PBKDF2-SHA256.-1.{parts[2]}.{parts[3]}"));
            Assert.False(_service.Verify("password", $"PBKDF2-SHA256.notanumber.{parts[2]}.{parts[3]}"));
        }

        [Fact]
        public void Verify_ReturnsFalse_ForInvalidBase64()
        {
            Assert.False(_service.Verify("password", "PBKDF2-SHA256.600000.!!!invalid!!!.alsoInvalid"));
        }

        [Theory]
        [InlineData(null,  "PBKDF2-SHA256.600000.abc.def")]
        [InlineData("",    "PBKDF2-SHA256.600000.abc.def")]
        [InlineData("   ", "PBKDF2-SHA256.600000.abc.def")]
        [InlineData("password", null)]
        [InlineData("password", "")]
        [InlineData("password", "   ")]
        public void Verify_ReturnsFalse_ForNullOrWhitespaceInputs(string? password, string? hash)
        {
            Assert.False(_service.Verify(password, hash));
        }
    }
}
