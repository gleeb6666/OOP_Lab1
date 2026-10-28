using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Parsing;

namespace MyExcelMAUIApp.Tests;

public class LexerTests
{
	[Fact]
	public void Tokenize_SplitsExpressionIntoTokensWithPositions()
	{
		var tokens = Lexer.Tokenize("A1 + inc(25)");

		Assert.Equal(
			[TokenType.CellReference, TokenType.Plus, TokenType.Identifier, TokenType.LeftParen,
			 TokenType.Number, TokenType.RightParen, TokenType.End],
			tokens.Select(t => t.Type));
		Assert.Equal([0, 3, 5, 8, 9, 11, 12], tokens.Select(t => t.Position));
		Assert.Equal("25", tokens[4].Text);
	}

	[Fact]
	public void Tokenize_RecognizesAllOperators()
	{
		var types = Lexer.Tokenize("+-*/^(),").Select(t => t.Type);

		Assert.Equal(
			[TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide, TokenType.Power,
			 TokenType.LeftParen, TokenType.RightParen, TokenType.Comma, TokenType.End],
			types);
	}

	[Theory]
	[InlineData("2 % 3", 2)]
	[InlineData("1 = 1", 2)]
	[InlineData("5.5", 1)]
	public void Tokenize_InvalidCharacter_ReportsPosition(string text, int position)
	{
		var ex = Assert.Throws<SyntaxErrorException>(() => Lexer.Tokenize(text));
		Assert.Equal(position, ex.Position);
	}

	[Fact]
	public void Tokenize_CyrillicLetters_AreRejected()
	{
		var ex = Assert.Throws<SyntaxErrorException>(() => Lexer.Tokenize("2+А1"));
		Assert.Equal(2, ex.Position);
		Assert.Contains("латинськими", ex.Message);
	}

	[Fact]
	public void Tokenize_LettersAfterDigits_IsInvalidName()
	{
		var ex = Assert.Throws<SyntaxErrorException>(() => Lexer.Tokenize("A1B"));
		Assert.Equal(0, ex.Position);
	}
}
