using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Parsing.Ast;

public sealed class CellReferenceNode : Node
{
	public CellReferenceNode(CellAddress address)
	{
		Address = address;
	}

	public CellAddress Address { get; }

	public override BigInteger Evaluate(IEvaluationContext context) => context.GetCellValue(Address);

	public override IEnumerable<CellAddress> GetReferences() => [Address];
}
