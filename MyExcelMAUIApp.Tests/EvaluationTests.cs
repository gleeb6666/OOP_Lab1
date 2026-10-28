using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Parsing;

namespace MyExcelMAUIApp.Tests;

public class EvaluationTests
{
	private sealed class ConstantContext : IEvaluationContext
	{
		public BigInteger GetCellValue(CellAddress address) => 10;
	}

	private static BigInteger Evaluate(string text) => Parser.Parse(text).Evaluate(new ConstantContext());

	[Theory]
	[InlineData("2+3*4", "14")]
	[InlineData("(2+3)*4", "20")]
	[InlineData("10-4-3", "3")]
	[InlineData("100/10/5", "2")]
	[InlineData("7/2", "3")]
	[InlineData("0-7/2", "-3")]
	[InlineData("2^10", "1024")]
	[InlineData("2^3^2", "512")]
	[InlineData("(2^3)^2", "64")]
	[InlineData("2*3^2", "18")]
	[InlineData("0^0", "1")]
	[InlineData("inc(5)", "6")]
	[InlineData("dec(0)", "-1")]
	[InlineData("inc(dec(5))", "5")]
	[InlineData("mmax(3, 9, 2)", "9")]
	[InlineData("mmin(3, 9, 2)", "2")]
	[InlineData("mmax(5)", "5")]
	[InlineData("mmin(0-1, 0-5, 4)", "-5")]
	[InlineData("A1 * B2 + 1", "101")]
	[InlineData("mmax(inc(A1), 2^4)", "16")]
	public void Evaluate_ReturnsExpectedValue(string text, string expected)
	{
		Assert.Equal(BigInteger.Parse(expected), Evaluate(text));
	}

	[Fact]
	public void Evaluate_SupportsArbitraryLengthIntegers()
	{
		Assert.Equal(BigInteger.Parse("1267650600228229401496703205376"), Evaluate("2^100"));
		Assert.Equal(
			BigInteger.Parse("9999999999999999999800000000000000000001"),
			Evaluate("99999999999999999999 * 99999999999999999999"));
		Assert.Equal(BigInteger.Parse("100000000000000000000000000000"), Evaluate("inc(99999999999999999999999999999)"));
	}

	[Theory]
	[InlineData("1/0", ErrorCodes.DivideByZero)]
	[InlineData("5/(3-3)", ErrorCodes.DivideByZero)]
	[InlineData("2^(0-1)", ErrorCodes.Number)]
	[InlineData("10^10000000", ErrorCodes.Number)]
	[InlineData("(2^1000)^100000", ErrorCodes.Number)]
	public void Evaluate_InvalidOperation_ThrowsWithCode(string text, string code)
	{
		var ex = Assert.Throws<EvaluationException>(() => Evaluate(text));
		Assert.Equal(code, ex.ErrorCode);
	}

	[Theory]
	[InlineData("1^999999999999", "1")]
	[InlineData("(0-1)^1000001", "-1")]
	[InlineData("0^999999999999", "0")]
	public void Evaluate_PowerWithTrivialBase_DoesNotOverflow(string text, string expected)
	{
		Assert.Equal(BigInteger.Parse(expected), Evaluate(text));
	}
}
