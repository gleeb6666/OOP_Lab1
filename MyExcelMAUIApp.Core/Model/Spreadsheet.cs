using System.Diagnostics.CodeAnalysis;
using MyExcelMAUIApp.Core.Evaluation;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Parsing;

namespace MyExcelMAUIApp.Core.Model;

public sealed class Spreadsheet
{
	public const int DefaultRowCount = 50;
	public const int DefaultColumnCount = 20;

	public const int MaxRowCount = 1000;
	public const int MaxColumnCount = 100;

	private readonly Dictionary<CellAddress, Cell> cells = new();

	public Spreadsheet(int rowCount = DefaultRowCount, int columnCount = DefaultColumnCount)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCount, MaxRowCount);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(columnCount, MaxColumnCount);
		RowCount = rowCount;
		ColumnCount = columnCount;
	}

	public int RowCount { get; private set; }
	public int ColumnCount { get; private set; }

	public IEnumerable<Cell> NonEmptyCells => cells.Values;

	public bool IsInRange(CellAddress address) =>
		address.Row <= RowCount && address.Column <= ColumnCount;

	public bool TryGetCell(CellAddress address, [NotNullWhen(true)] out Cell? cell) =>
		cells.TryGetValue(address, out cell);

	public Cell GetCell(CellAddress address)
	{
		EnsureInRange(address);
		return cells.TryGetValue(address, out var cell) ? cell : new Cell(address);
	}

	public void SetExpression(CellAddress address, string? expression)
	{
		StoreExpression(address, expression);
		Recalculate();
	}

	public void SetExpressions(IEnumerable<KeyValuePair<CellAddress, string>> expressions)
	{
		foreach (var (address, expression) in expressions)
			StoreExpression(address, expression);
		Recalculate();
	}

	private void StoreExpression(CellAddress address, string? expression)
	{
		EnsureInRange(address);
		string text = expression?.Trim() ?? string.Empty;

		if (text.Length == 0)
		{
			cells.Remove(address);
			return;
		}

		if (!cells.TryGetValue(address, out var cell))
		{
			cell = new Cell(address);
			cells.Add(address, cell);
		}
		cell.Expression = text;
		ParseCell(cell);
	}

	public void Recalculate() => new SpreadsheetEvaluator(this).EvaluateAll();

	public void AddRow()
	{
		if (RowCount == MaxRowCount)
			throw new InvalidOperationException($"У таблиці може бути не більше {MaxRowCount} рядків");
		RowCount++;
	}

	public void AddColumn()
	{
		if (ColumnCount == MaxColumnCount)
			throw new InvalidOperationException($"У таблиці може бути не більше {MaxColumnCount} стовпчиків");
		ColumnCount++;
	}

	public void RemoveLastRow()
	{
		if (RowCount == 1)
			throw new InvalidOperationException("У таблиці має залишитися хоча б один рядок");
		RemoveCells(cell => cell.Address.Row == RowCount);
		RowCount--;
		Recalculate();
	}

	public void RemoveLastColumn()
	{
		if (ColumnCount == 1)
			throw new InvalidOperationException("У таблиці має залишитися хоча б один стовпчик");
		RemoveCells(cell => cell.Address.Column == ColumnCount);
		ColumnCount--;
		Recalculate();
	}

	public IReadOnlyList<CellAddress> GetCellsReferencingRow(int row) =>
		GetCellsReferencing(reference => reference.Row == row, cell => cell.Address.Row != row);

	public IReadOnlyList<CellAddress> GetCellsReferencingColumn(int column) =>
		GetCellsReferencing(reference => reference.Column == column, cell => cell.Address.Column != column);

	private IReadOnlyList<CellAddress> GetCellsReferencing(Func<CellAddress, bool> isTarget, Func<Cell, bool> isOutside) =>
		cells.Values
			.Where(cell => isOutside(cell) && cell.Ast is not null && cell.Ast.GetReferences().Any(isTarget))
			.Select(cell => cell.Address)
			.OrderBy(address => address.Row)
			.ThenBy(address => address.Column)
			.ToList();

	private static void ParseCell(Cell cell)
	{
		try
		{
			cell.Ast = Parser.Parse(cell.Expression);
			cell.SyntaxError = null;
		}
		catch (SyntaxErrorException ex)
		{
			cell.Ast = null;
			cell.SyntaxError = new CellError(ex.ErrorCode, ex.Message);
		}
	}

	private void RemoveCells(Func<Cell, bool> predicate)
	{
		foreach (var cell in cells.Values.Where(predicate).ToList())
			cells.Remove(cell.Address);
	}

	private void EnsureInRange(CellAddress address)
	{
		if (!IsInRange(address))
			throw new ArgumentOutOfRangeException(nameof(address), $"Клітинка {address} знаходиться поза межами таблиці");
	}
}
