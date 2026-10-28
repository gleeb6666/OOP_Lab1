using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Parsing;
using MyExcelMAUIApp.Core.Parsing.Ast;

namespace MyExcelMAUIApp.Tests;

public class ParserTests
{
	[Theory]
	[InlineData("42")]
	[InlineData("A1")]
	[InlineData("(1+2)*3")]
	[InlineData("2^3^2")]
	[InlineData("inc(dec(5))")]
	[InlineData("mmax(1, A1, 3)")]
	[InlineData("mmin(7)")]
	[InlineData("  1 +   2 ")]
	[InlineData("INC(1) + MMax(2,3)")]
	public void Parse_ValidExpression_DoesNotThrow(string text)
	{
		Assert.NotNull(Parser.Parse(text));
	}

	[Theory]
	[InlineData("", 0)]
	[InlineData("   ", 3)]
	[InlineData("(1+2", 4)]
	[InlineData("1+", 2)]
	[InlineData("1 2", 2)]
	[InlineData("1+2)", 3)]
	[InlineData("-5", 0)]
	[InlineData("2*-3", 2)]
	[InlineData("foo(1)", 0)]
	[InlineData("inc 1", 4)]
	[InlineData("inc(1,2)", 0)]
	[InlineData("mmax()", 5)]
	[InlineData("mmax(1,)", 7)]
	[InlineData("mmax(1 2)", 7)]
	[InlineData("A0", 0)]
	[InlineData("*3", 0)]
	public void Parse_InvalidExpression_ReportsPosition(string text, int position)
	{
		var ex = Assert.Throws<SyntaxErrorException>(() => Parser.Parse(text));
		Assert.Equal(position, ex.Position);
		Assert.Equal(ErrorCodes.Syntax, ex.ErrorCode);
	}

	[Fact]
	public void Parse_UnaryMinus_ExplainsHowToWriteNegativeNumber()
	{
		var ex = Assert.Throws<SyntaxErrorException>(() => Parser.Parse("-5"));
		Assert.Contains("0-5", ex.Message);
	}

	[Fact]
	public void Parse_MultiplicationBindsTighterThanAddition()
	{
		var root = Assert.IsType<BinaryOperationNode>(Parser.Parse("1+2*3"));

		Assert.Equal(BinaryOperator.Add, root.Operator);
		Assert.Equal(BinaryOperator.Multiply, Assert.IsType<BinaryOperationNode>(root.Right).Operator);
	}

	[Fact]
	public void Parse_PowerIsRightAssociative()
	{
		var root = Assert.IsType<BinaryOperationNode>(Parser.Parse("2^3^2"));

		Assert.IsType<NumberNode>(root.Left);
		Assert.Equal(BinaryOperator.Power, Assert.IsType<BinaryOperationNode>(root.Right).Operator);
	}

	[Fact]
	public void Parse_CollectsCellReferences()
	{
		var references = Parser.Parse("A1 + mmax(B2, inc(c3))").GetReferences();

		Assert.Equal([new CellAddress(1, 1), new CellAddress(2, 2), new CellAddress(3, 3)], references);
	}

	[Fact]
	public void Parse_TooDeepNesting_IsSyntaxError()
	{
		string text = new string('(', 500) + "1" + new string(')', 500);
		Assert.Throws<SyntaxErrorException>(() => Parser.Parse(text));
	}
}
