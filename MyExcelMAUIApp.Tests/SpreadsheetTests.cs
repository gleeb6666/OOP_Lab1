using System.Numerics;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Tests;

public class SpreadsheetTests
{
	private static CellAddress At(string name) => CellAddress.Parse(name);

	private static Spreadsheet CreateSheet(params (string Address, string Expression)[] values)
	{
		var sheet = new Spreadsheet(10, 5);
		foreach (var (address, expression) in values)
			sheet.SetExpression(At(address), expression);
		return sheet;
	}

	[Fact]
	public void NewSpreadsheet_HasDefaultSizeAndNoCells()
	{
		var sheet = new Spreadsheet();

		Assert.Equal(Spreadsheet.DefaultRowCount, sheet.RowCount);
		Assert.Equal(Spreadsheet.DefaultColumnCount, sheet.ColumnCount);
		Assert.Empty(sheet.NonEmptyCells);
		Assert.True(sheet.GetCell(At("A1")).IsEmpty);
	}

	[Fact]
	public void SetExpression_StoresExpressionAndValue()
	{
		var sheet = CreateSheet(("A1", " 2 + 3 "));
		var cell = sheet.GetCell(At("A1"));

		Assert.Equal("2 + 3", cell.Expression);
		Assert.Equal(new BigInteger(5), cell.Value);
		Assert.Equal("5", cell.DisplayValue);
		Assert.Null(cell.Error);
	}

	[Fact]
	public void References_UseValuesOfOtherCells()
	{
		var sheet = CreateSheet(("A1", "10"), ("B1", "A1 * 2"), ("C1", "mmax(a1, b1, 3)"));

		Assert.Equal(new BigInteger(20), sheet.GetCell(At("B1")).Value);
		Assert.Equal(new BigInteger(20), sheet.GetCell(At("C1")).Value);
	}

	[Fact]
	public void ChangingCell_RecalculatesDependentCells()
	{
		var sheet = CreateSheet(("A1", "1"), ("B1", "A1 + 1"), ("C1", "B1 * 10"));

		sheet.SetExpression(At("A1"), "5");

		Assert.Equal(new BigInteger(6), sheet.GetCell(At("B1")).Value);
		Assert.Equal(new BigInteger(60), sheet.GetCell(At("C1")).Value);
	}

	[Fact]
	public void EmptyCell_HasValueZero()
	{
		var sheet = CreateSheet(("A1", "B1 + 7"));
		Assert.Equal(new BigInteger(7), sheet.GetCell(At("A1")).Value);
	}

	[Fact]
	public void ClearingCell_RemovesIt()
	{
		var sheet = CreateSheet(("A1", "3"), ("B1", "A1 + 1"));

		sheet.SetExpression(At("A1"), "  ");

		Assert.Single(sheet.NonEmptyCells);
		Assert.Equal(new BigInteger(1), sheet.GetCell(At("B1")).Value);
	}

	[Fact]
	public void SyntaxError_IsShownInCell()
	{
		var sheet = CreateSheet(("A1", "(1+2"));
		var cell = sheet.GetCell(At("A1"));

		Assert.Null(cell.Value);
		Assert.Equal(ErrorCodes.Syntax, cell.DisplayValue);
		Assert.Contains("позиція 5", cell.Error!.Message);
		Assert.Equal("(1+2", cell.Expression);
	}

	[Fact]
	public void ReferenceOutsideTable_IsRefError()
	{
		var sheet = CreateSheet(("A1", "Z99 + 1"));
		Assert.Equal(ErrorCodes.Reference, sheet.GetCell(At("A1")).Error!.Code);
	}

	[Fact]
	public void ErrorsPropagateToDependentCells()
	{
		var sheet = CreateSheet(("A1", "1/0"), ("B1", "A1 + 1"), ("C1", "inc(B1)"));

		Assert.Equal(ErrorCodes.DivideByZero, sheet.GetCell(At("A1")).DisplayValue);
		Assert.Equal(ErrorCodes.DivideByZero, sheet.GetCell(At("C1")).DisplayValue);
		Assert.Contains("B1", sheet.GetCell(At("C1")).Error!.Message);
	}

	[Fact]
	public void SelfReference_IsCycle()
	{
		var sheet = CreateSheet(("A1", "A1 + 1"));
		Assert.Equal(ErrorCodes.Cycle, sheet.GetCell(At("A1")).DisplayValue);
	}

	[Fact]
	public void CircularReferences_MarkWholeCycle()
	{
		var sheet = CreateSheet(("D1", "A1 + 1"), ("A1", "B1"), ("B1", "C1"), ("C1", "A1"), ("E1", "5"));

		foreach (var name in new[] { "A1", "B1", "C1", "D1" })
			Assert.Equal(ErrorCodes.Cycle, sheet.GetCell(At(name)).DisplayValue);
		Assert.Contains("→", sheet.GetCell(At("B1")).Error!.Message);
		Assert.Equal(new BigInteger(5), sheet.GetCell(At("E1")).Value);
	}

	[Fact]
	public void BreakingCycle_RestoresValues()
	{
		var sheet = CreateSheet(("A1", "B1"), ("B1", "A1"));

		sheet.SetExpression(At("B1"), "3");

		Assert.Equal(new BigInteger(3), sheet.GetCell(At("A1")).Value);
	}

	[Fact]
	public void AddRowAndColumn_ExtendTable()
	{
		var sheet = CreateSheet();
		sheet.AddRow();
		sheet.AddColumn();

		Assert.Equal(11, sheet.RowCount);
		Assert.Equal(6, sheet.ColumnCount);
		sheet.SetExpression(At("F11"), "1");
		Assert.Equal(new BigInteger(1), sheet.GetCell(At("F11")).Value);
	}

	[Fact]
	public void RemoveLastRow_DeletesItsCellsAndBreaksReferences()
	{
		var sheet = CreateSheet(("A10", "4"), ("B10", "A10"), ("A1", "A10 + 1"));

		Assert.Equal([At("A1")], sheet.GetCellsReferencingRow(10));

		sheet.RemoveLastRow();

		Assert.Equal(9, sheet.RowCount);
		Assert.Single(sheet.NonEmptyCells);
		Assert.Equal(ErrorCodes.Reference, sheet.GetCell(At("A1")).DisplayValue);
	}

	[Fact]
	public void RemoveLastColumn_DeletesItsCellsAndBreaksReferences()
	{
		var sheet = CreateSheet(("E1", "4"), ("A1", "E1 * 2"));

		Assert.Equal([At("A1")], sheet.GetCellsReferencingColumn(5));

		sheet.RemoveLastColumn();

		Assert.Equal(4, sheet.ColumnCount);
		Assert.Equal(ErrorCodes.Reference, sheet.GetCell(At("A1")).DisplayValue);
	}

	[Fact]
	public void CannotRemoveLastRemainingRowOrColumn()
	{
		var sheet = new Spreadsheet(1, 1);

		Assert.Throws<InvalidOperationException>(sheet.RemoveLastRow);
		Assert.Throws<InvalidOperationException>(sheet.RemoveLastColumn);
	}

	[Fact]
	public void CannotGrowBeyondMaximumSize()
	{
		var sheet = new Spreadsheet(Spreadsheet.MaxRowCount, Spreadsheet.MaxColumnCount);

		Assert.Throws<InvalidOperationException>(sheet.AddRow);
		Assert.Throws<InvalidOperationException>(sheet.AddColumn);
	}

	[Fact]
	public void SetExpressions_FillsManyCellsAtOnce()
	{
		var sheet = new Spreadsheet(3, 3);
		sheet.SetExpressions(new Dictionary<CellAddress, string>
		{
			[At("A1")] = "B1 + 1",
			[At("B1")] = "10",
			[At("C1")] = " "
		});

		Assert.Equal(2, sheet.NonEmptyCells.Count());
		Assert.Equal(new BigInteger(11), sheet.GetCell(At("A1")).Value);
	}

	[Fact]
	public void SetExpression_OutsideTable_Throws()
	{
		var sheet = CreateSheet();
		Assert.Throws<ArgumentOutOfRangeException>(() => sheet.SetExpression(At("Z1"), "1"));
	}
}
