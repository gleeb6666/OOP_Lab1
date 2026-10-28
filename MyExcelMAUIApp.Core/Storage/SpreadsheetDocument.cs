namespace MyExcelMAUIApp.Core.Storage;

internal sealed class SpreadsheetDocument
{
	public string? Format { get; set; }
	public int Version { get; set; }
	public int RowCount { get; set; }
	public int ColumnCount { get; set; }
	public List<CellDocument>? Cells { get; set; }
}

internal sealed class CellDocument
{
	public string? Address { get; set; }
	public string? Expression { get; set; }
}
