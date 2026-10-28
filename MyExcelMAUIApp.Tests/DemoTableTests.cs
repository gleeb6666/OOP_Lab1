using System.Numerics;
using MyExcelMAUIApp.Core.Exceptions;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Storage;

namespace MyExcelMAUIApp.Tests;

public class DemoTableTests
{
	private static readonly Spreadsheet Demo = SpreadsheetJsonSerializer.Deserialize(
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Examples", "demo_variant12.json")));

	[Theory]
	[InlineData("C1", "16")]
	[InlineData("D1", "26")]
	[InlineData("E1", "3")]
	[InlineData("A2", "1024")]
	[InlineData("B2", "512")]
	[InlineData("C2", "64")]
	[InlineData("D2", "1267650600228229401496703205376")]
	[InlineData("E2", "1606938044258990275541962092341162602522202993782792835301376")]
	[InlineData("A3", "11")]
	[InlineData("B3", "2")]
	[InlineData("C3", "5")]
	[InlineData("D3", "16")]
	[InlineData("E3", "3")]
	[InlineData("A4", "5")]
	[InlineData("B4", "-10")]
	[InlineData("C4", "-10")]
	[InlineData("D4", "-1")]
	[InlineData("E4", "40")]
	public void Values(string address, string expected)
	{
		var cell = Demo.GetCell(CellAddress.Parse(address));
		Assert.Null(cell.Error);
		Assert.Equal(BigInteger.Parse(expected), cell.Value);
	}

	[Theory]
	[InlineData("A5", ErrorCodes.DivideByZero)]
	[InlineData("B5", ErrorCodes.DivideByZero)]
	[InlineData("C5", ErrorCodes.Syntax)]
	[InlineData("D5", ErrorCodes.Syntax)]
	[InlineData("E5", ErrorCodes.Number)]
	[InlineData("A6", ErrorCodes.Cycle)]
	[InlineData("B6", ErrorCodes.Cycle)]
	[InlineData("C6", ErrorCodes.Reference)]
	[InlineData("D6", ErrorCodes.Syntax)]
	[InlineData("E6", ErrorCodes.Syntax)]
	public void Errors(string address, string code)
	{
		Assert.Equal(code, Demo.GetCell(CellAddress.Parse(address)).DisplayValue);
	}
}
