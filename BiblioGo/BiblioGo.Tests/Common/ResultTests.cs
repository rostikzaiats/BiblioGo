using BiblioGo.Application.Common;

namespace BiblioGo.Tests.Common
{
    // Unit-тести спільного класу Result (Result патерн замість винятків при помилках валідації)
    public class ResultTests
    {
        private const string ErrorMessage = "Вкажіть назву.";

        [Fact]
        public void Success_WithoutValue_IsSuccessWithEmptyError()
        {
            // Act
            var result = Result.Success();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Empty(result.Error);
        }

        [Fact]
        public void Failure_WithoutValue_IsFailureWithError()
        {
            // Act
            var result = Result.Failure(ErrorMessage);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage, result.Error);
        }

        [Fact]
        public void Success_WithValue_ReturnsValue()
        {
            // Act
            var result = Result.Success(42);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
            Assert.Empty(result.Error);
        }

        [Fact]
        public void Failure_WithValueType_IsFailureWithError()
        {
            // Act
            var result = Result.Failure<int>(ErrorMessage);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorMessage, result.Error);
        }

        [Fact]
        public void Value_OfFailure_ThrowsInvalidOperationException()
        {
            // Arrange
            var result = Result.Failure<int>(ErrorMessage);

            // Act
            var exception = Record.Exception(() => result.Value);

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Failure_WithEmptyError_ThrowsArgumentException(string error)
        {
            // Act
            var exception = Record.Exception(() => Result.Failure(error));

            // Assert
            Assert.IsAssignableFrom<ArgumentException>(exception);
        }
    }
}
