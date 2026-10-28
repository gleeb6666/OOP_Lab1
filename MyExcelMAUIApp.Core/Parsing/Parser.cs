using System.Numerics;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Parsing.Ast;

namespace MyExcelMAUIApp.Core.Parsing;

public sealed class Parser
{
	public const int MaxNestingDepth = 200;

	private readonly IReadOnlyList<Token> tokens;
	private int position;
	private int depth;

	private Parser(IReadOnlyList<Token> tokens)
	{
		this.tokens = tokens;
	}

	private Token Current => tokens[position];

	public static Node Parse(string text)
	{
		var parser = new Parser(Lexer.Tokenize(text));
		return parser.ParseWholeExpression();
	}

	private Node ParseWholeExpression()
	{
		if (Current.Type == TokenType.End)
			throw new SyntaxErrorException("Порожній вираз", Current.Position);

		var node = ParseExpression();

		if (Current.Type == TokenType.RightParen)
			throw new SyntaxErrorException("Зайва дужка ')'", Current.Position);
		if (Current.Type != TokenType.End)
			throw new SyntaxErrorException($"Очікувалася операція (+, -, *, /, ^) перед '{Current.Text}'", Current.Position);

		return node;
	}

	private Node ParseExpression()
	{
		var left = ParseTerm();
		while (Current.Type is TokenType.Plus or TokenType.Minus)
		{
			var op = Advance().Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
			left = new BinaryOperationNode(op, left, ParseTerm());
		}
		return left;
	}

	private Node ParseTerm()
	{
		var left = ParsePower();
		while (Current.Type is TokenType.Multiply or TokenType.Divide)
		{
			var op = Advance().Type == TokenType.Multiply ? BinaryOperator.Multiply : BinaryOperator.Divide;
			left = new BinaryOperationNode(op, left, ParsePower());
		}
		return left;
	}

	private Node ParsePower()
	{
		var value = ParsePrimary();
		if (Current.Type != TokenType.Power)
			return value;

		Advance();
		EnterNested();
		var exponent = ParsePower();
		depth--;
		return new BinaryOperationNode(BinaryOperator.Power, value, exponent);
	}

	private Node ParsePrimary()
	{
		var token = Current;
		switch (token.Type)
		{
			case TokenType.Number:
				Advance();
				return new NumberNode(BigInteger.Parse(token.Text));

			case TokenType.CellReference:
				Advance();
				if (!CellAddress.TryParse(token.Text, out var address))
					throw new SyntaxErrorException($"Некоректне посилання на клітинку '{token.Text}'", token.Position);
				return new CellReferenceNode(address);

			case TokenType.Identifier:
				return ParseFunctionCall();

			case TokenType.LeftParen:
				Advance();
				EnterNested();
				var inner = ParseExpression();
				depth--;
				Expect(TokenType.RightParen, "Очікувалася закриваюча дужка ')'");
				return inner;

			case TokenType.Plus or TokenType.Minus:
				throw new SyntaxErrorException(
					$"Унарний '{token.Text}' не підтримується; від'ємне число записуйте як 0-5", token.Position);

			case TokenType.End:
				throw new SyntaxErrorException("Вираз не завершено: очікувалося число, клітинка, функція або '('", token.Position);

			default:
				throw new SyntaxErrorException(
					$"Неочікуваний символ '{token.Text}': очікувалося число, клітинка, функція або '('", token.Position);
		}
	}

	private Node ParseFunctionCall()
	{
		var nameToken = Advance();
		if (!FunctionCallNode.TryGetFunction(nameToken.Text, out var function))
			throw new SyntaxErrorException(
				$"Невідома функція '{nameToken.Text}'. Доступні: {string.Join(", ", FunctionCallNode.FunctionNames)}",
				nameToken.Position);

		Expect(TokenType.LeftParen, $"Після '{nameToken.Text}' очікувалася дужка '('");
		if (Current.Type == TokenType.RightParen)
			throw new SyntaxErrorException($"Функція '{nameToken.Text}' потребує хоча б одного аргументу", Current.Position);

		EnterNested();
		var arguments = new List<Node> { ParseExpression() };
		while (Current.Type == TokenType.Comma)
		{
			Advance();
			arguments.Add(ParseExpression());
		}
		depth--;
		Expect(TokenType.RightParen, "Очікувалася ',' або закриваюча дужка ')'");

		if (!FunctionCallNode.IsValidArgumentCount(function, arguments.Count))
			throw new SyntaxErrorException(
				$"Функція '{nameToken.Text}' приймає рівно один аргумент, а передано {arguments.Count}", nameToken.Position);

		return new FunctionCallNode(function, arguments);
	}

	private Token Advance()
	{
		var token = Current;
		if (token.Type != TokenType.End)
			position++;
		return token;
	}

	private void Expect(TokenType type, string errorMessage)
	{
		if (Current.Type != type)
			throw new SyntaxErrorException(errorMessage, Current.Position);
		Advance();
	}

	private void EnterNested()
	{
		if (++depth > MaxNestingDepth)
			throw new SyntaxErrorException($"Занадто глибока вкладеність (більше {MaxNestingDepth})", Current.Position);
	}
}
