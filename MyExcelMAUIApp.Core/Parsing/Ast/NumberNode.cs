using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Parsing.Ast;

public sealed class NumberNode : Node
{
	public NumberNode(BigInteger value)
	{
		Value = value;
	}

	public BigInteger Value { get; }

	public override BigInteger Evaluate(IEvaluationContext context) => Value;

	public override IEnumerable<CellAddress> GetReferences() => [];
}
