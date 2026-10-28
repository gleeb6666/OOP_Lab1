using MyExcelMAUIApp.Core.Exceptions;

namespace MyExcelMAUIApp.Core.Parsing;

public static class Lexer
{
	public static IReadOnlyList<Token> Tokenize(string text)
	{
		var tokens = new List<Token>();
		int i = 0;

		while (i < text.Length)
		{
			char c = text[i];
			int start = i;

			if (char.IsWhiteSpace(c))
			{
				i++;
				continue;
			}

			if (char.IsDigit(c))
			{
				while (i < text.Length && char.IsDigit(text[i]))
					i++;
				tokens.Add(new Token(TokenType.Number, text[start..i], start));
				continue;
			}

			if (char.IsLetter(c))
			{
				tokens.Add(ReadWord(text, ref i));
				continue;
			}

			TokenType? type = c switch
			{
				'+' => TokenType.Plus,
				'-' => TokenType.Minus,
				'*' => TokenType.Multiply,
				'/' => TokenType.Divide,
				'^' => TokenType.Power,
				'(' => TokenType.LeftParen,
				')' => TokenType.RightParen,
				',' => TokenType.Comma,
				_ => null
			};

			if (type is null)
				throw new SyntaxErrorException($"Недопустимий символ '{c}'", start);

			tokens.Add(new Token(type.Value, c.ToString(), start));
			i++;
		}

		tokens.Add(new Token(TokenType.End, "", text.Length));
		return tokens;
	}

	private static Token ReadWord(string text, ref int i)
	{
		int start = i;
		while (i < text.Length && char.IsLetter(text[i]))
			i++;
		int lettersEnd = i;
		while (i < text.Length && char.IsDigit(text[i]))
			i++;

		if (i < text.Length && char.IsLetter(text[i]))
		{
			while (i < text.Length && char.IsLetterOrDigit(text[i]))
				i++;
			throw new SyntaxErrorException($"Некоректне ім'я '{text[start..i]}'", start);
		}

		string word = text[start..i];
		if (!text[start..lettersEnd].All(char.IsAsciiLetter))
			throw new SyntaxErrorException($"Ім'я '{word}' має бути записане латинськими літерами", start);

		var type = i > lettersEnd ? TokenType.CellReference : TokenType.Identifier;
		return new Token(type, word, start);
	}
}
