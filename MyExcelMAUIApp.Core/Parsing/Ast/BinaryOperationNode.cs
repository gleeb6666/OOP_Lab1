using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Parsing.Ast;

public enum BinaryOperator
{
	Add,
	Subtract,
	Multiply,
	Divide,
	Power
}

public sealed class BinaryOperationNode : Node
{
	public const long MaxPowerResultBits = 1_000_000;

	public BinaryOperationNode(BinaryOperator op, Node left, Node right)
	{
		Operator = op;
		Left = left;
		Right = right;
	}

	public BinaryOperator Operator { get; }
	public Node Left { get; }
	public Node Right { get; }

	public override BigInteger Evaluate(IEvaluationContext context)
	{
		var left = Left.Evaluate(context);
		var right = Right.Evaluate(context);

		return Operator switch
		{
			BinaryOperator.Add => left + right,
			BinaryOperator.Subtract => left - right,
			BinaryOperator.Multiply => left * right,
			BinaryOperator.Divide => Divide(left, right),
			BinaryOperator.Power => Power(left, right),
			_ => throw new InvalidOperationException($"Невідома операція {Operator}")
		};
	}

	public override IEnumerable<CellAddress> GetReferences() => Left.GetReferences().Concat(Right.GetReferences());

	private static BigInteger Divide(BigInteger left, BigInteger right)
	{
		if (right.IsZero)
			throw new EvaluationException(ErrorCodes.DivideByZero, "Ділення на нуль");
		return BigInteger.Divide(left, right);
	}

	private static BigInteger Power(BigInteger value, BigInteger exponent)
	{
		if (exponent.Sign < 0)
			throw new EvaluationException(ErrorCodes.Number, "Від'ємний показник степеня не підтримується: результат має бути цілим числом");
		if (value.IsZero)
			return exponent.IsZero ? BigInteger.One : BigInteger.Zero;
		if (value.IsOne)
			return BigInteger.One;
		if (value == BigInteger.MinusOne)
			return exponent.IsEven ? BigInteger.One : BigInteger.MinusOne;

		if (exponent > MaxPowerResultBits || value.GetBitLength() * (long)exponent > MaxPowerResultBits)
			throw new EvaluationException(ErrorCodes.Number, "Результат піднесення до степеня занадто великий");

		return BigInteger.Pow(value, (int)exponent);
	}
}
