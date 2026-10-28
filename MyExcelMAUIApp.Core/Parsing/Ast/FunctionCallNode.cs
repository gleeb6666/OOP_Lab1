using System.Numerics;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Parsing.Ast;

public enum FunctionKind
{
	Inc,
	Dec,
	MMax,
	MMin
}

public sealed class FunctionCallNode : Node
{
	private static readonly Dictionary<string, FunctionKind> FunctionsByName = new(StringComparer.OrdinalIgnoreCase)
	{
		["inc"] = FunctionKind.Inc,
		["dec"] = FunctionKind.Dec,
		["mmax"] = FunctionKind.MMax,
		["mmin"] = FunctionKind.MMin
	};

	public FunctionCallNode(FunctionKind function, IReadOnlyList<Node> arguments)
	{
		Function = function;
		Arguments = arguments;
	}

	public FunctionKind Function { get; }
	public IReadOnlyList<Node> Arguments { get; }

	public static IEnumerable<string> FunctionNames => FunctionsByName.Keys;

	public static bool TryGetFunction(string name, out FunctionKind function) =>
		FunctionsByName.TryGetValue(name, out function);

	public static bool IsValidArgumentCount(FunctionKind function, int count) => function switch
	{
		FunctionKind.Inc or FunctionKind.Dec => count == 1,
		_ => count >= 1
	};

	public override BigInteger Evaluate(IEvaluationContext context)
	{
		var values = Arguments.Select(argument => argument.Evaluate(context)).ToList();

		return Function switch
		{
			FunctionKind.Inc => values[0] + 1,
			FunctionKind.Dec => values[0] - 1,
			FunctionKind.MMax => values.Max(),
			FunctionKind.MMin => values.Min(),
			_ => throw new InvalidOperationException($"Невідома функція {Function}")
		};
	}

	public override IEnumerable<CellAddress> GetReferences() => Arguments.SelectMany(argument => argument.GetReferences());
}
