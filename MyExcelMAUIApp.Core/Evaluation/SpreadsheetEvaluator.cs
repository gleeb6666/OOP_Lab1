using System.Numerics;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Evaluation;

internal sealed class SpreadsheetEvaluator : IEvaluationContext
{
	private readonly Spreadsheet sheet;
	private readonly HashSet<CellAddress> evaluated = new();
	private readonly List<CellAddress> stack = new();

	public SpreadsheetEvaluator(Spreadsheet sheet)
	{
		this.sheet = sheet;
	}

	public void EvaluateAll()
	{
		foreach (var cell in sheet.NonEmptyCells)
		{
			cell.Value = null;
			cell.Error = null;
		}

		foreach (var cell in sheet.NonEmptyCells)
			EvaluateCell(cell);
	}

	public BigInteger GetCellValue(CellAddress address)
	{
		if (!sheet.IsInRange(address))
			throw new EvaluationException(ErrorCodes.Reference, $"Клітинка {address} знаходиться поза межами таблиці");

		if (!sheet.TryGetCell(address, out var cell))
			return BigInteger.Zero;

		EvaluateCell(cell);

		if (cell.Error is not null)
			throw new EvaluationException(cell.Error.Code, $"Клітинка {address} містить помилку: {cell.Error.Message}");
		return cell.Value!.Value;
	}

	private void EvaluateCell(Cell cell)
	{
		var address = cell.Address;
		if (evaluated.Contains(address))
			return;

		int cycleStart = stack.IndexOf(address);
		if (cycleStart >= 0)
		{
			var path = stack.Skip(cycleStart).Append(address).Select(a => a.ToString());
			throw new CircularReferenceException(address, string.Join(" → ", path));
		}

		stack.Add(address);
		try
		{
			if (cell.SyntaxError is not null)
				cell.Error = cell.SyntaxError;
			else
				cell.Value = cell.Ast!.Evaluate(this);
		}
		catch (CircularReferenceException ex)
		{
			cell.Error = new CellError(ErrorCodes.Cycle, $"Циклічне посилання: {ex.Path}");
			if (ex.Start != address)
				throw;
		}
		catch (ExpressionException ex)
		{
			cell.Error = new CellError(ex.ErrorCode, ex.Message);
		}
		finally
		{
			stack.RemoveAt(stack.Count - 1);
			evaluated.Add(address);
		}
	}

	private sealed class CircularReferenceException : Exception
	{
		public CircularReferenceException(CellAddress start, string path)
		{
			Start = start;
			Path = path;
		}

		public CellAddress Start { get; }
		public string Path { get; }
	}
}
