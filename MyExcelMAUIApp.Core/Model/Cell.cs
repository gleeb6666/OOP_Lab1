using System.Numerics;
using MyExcelMAUIApp.Core.Parsing.Ast;

namespace MyExcelMAUIApp.Core.Model;

public sealed class Cell
{
	internal Cell(CellAddress address)
	{
		Address = address;
	}

	public CellAddress Address { get; }

	public string Expression { get; internal set; } = string.Empty;

	public BigInteger? Value { get; internal set; }

	public CellError? Error { get; internal set; }

	public bool IsEmpty => Expression.Length == 0;

	public string DisplayValue => Error?.Code ?? Value?.ToString() ?? string.Empty;

	internal Node? Ast { get; set; }
	internal CellError? SyntaxError { get; set; }
}
