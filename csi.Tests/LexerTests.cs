using csi.Interpreter;

namespace csi.Tests
{
    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void Tokenize_ShouldReturnCorrectTokens_ForSimpleInput()
        {
            // Arrange
            var lexer = new Lexer();
            var code = "print(\"Hello, World!\");";

            // Act
            var tokens = lexer.Tokenize(code);

            // Assert
            Assert.AreEqual(5, tokens.Count, "Es sollten 5 Tokens erzeugt werden.");
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("print", tokens[0].Value);
            Assert.AreEqual(TokenType.LeftParenthesis, tokens[1].Type);
            Assert.AreEqual("(", tokens[1].Value);
            Assert.AreEqual(TokenType.StringLiteral, tokens[2].Type);
            Assert.AreEqual("\"Hello, World!\"", tokens[2].Value);
            Assert.AreEqual(TokenType.RightParenthesis, tokens[3].Type);
            Assert.AreEqual(")", tokens[3].Value);
            Assert.AreEqual(TokenType.Separator, tokens[4].Type);
            Assert.AreEqual(";", tokens[4].Value);
        }
    }
}