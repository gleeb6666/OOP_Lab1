using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Tests;

public class CellAddressTests
{
	[Theory]
	[InlineData(1, "A")]
	[InlineData(26, "Z")]
	[InlineData(27, "AA")]
	[InlineData(52, "AZ")]
	[InlineData(703, "AAA")]
	public void GetColumnName_ConvertsLikeExcel(int column, string name)
	{
		Assert.Equal(name, CellAddress.GetColumnName(column));
	}

	[Theory]
	[InlineData("A1", 1, 1)]
	[InlineData("b3", 3, 2)]
	[InlineData("AA10", 10, 27)]
	public void TryParse_ValidAddress(string text, int row, int column)
	{
		Assert.True(CellAddress.TryParse(text, out var address));
		Assert.Equal(new CellAddress(row, column), address);
		Assert.Equal(text.ToUpperInvariant(), address.ToString());
	}

	[Theory]
	[InlineData("")]
	[InlineData("A")]
	[InlineData("12")]
	[InlineData("A0")]
	[InlineData("1A")]
	[InlineData("A1B")]
	[InlineData("A99999999999")]
	[InlineData("ZZZZZZZZZZ1")]
	public void TryParse_InvalidAddress(string text)
	{
		Assert.False(CellAddress.TryParse(text, out _));
	}
}
