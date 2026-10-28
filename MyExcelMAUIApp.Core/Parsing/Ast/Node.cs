using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Parsing.Ast;

public abstract class Node
{
	public abstract BigInteger Evaluate(IEvaluationContext context);

	public abstract IEnumerable<CellAddress> GetReferences();
}
