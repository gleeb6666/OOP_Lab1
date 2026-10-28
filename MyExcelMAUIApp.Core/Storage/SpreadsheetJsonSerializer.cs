using System.Text.Encodings.Web;
using System.Text.Json;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Storage;

public static class SpreadsheetJsonSerializer
{
	public const string FileExtension = ".json";
	public const string FormatName = "MyExcelMAUIApp.Spreadsheet";
	public const int CurrentVersion = 1;

	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static string Serialize(Spreadsheet sheet) => JsonSerializer.Serialize(ToDocument(sheet), Options);

	public static Task SerializeAsync(Spreadsheet sheet, Stream stream, CancellationToken cancellationToken = default) =>
		JsonSerializer.SerializeAsync(stream, ToDocument(sheet), Options, cancellationToken);

	public static Spreadsheet Deserialize(string json)
	{
		try
		{
			return FromDocument(JsonSerializer.Deserialize<SpreadsheetDocument>(json, Options));
		}
		catch (JsonException ex)
		{
			throw new SpreadsheetFormatException("Файл пошкоджений або не є JSON-файлом таблиці.", ex);
		}
	}

	public static async Task<Spreadsheet> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
	{
		try
		{
			var document = await JsonSerializer.DeserializeAsync<SpreadsheetDocument>(stream, Options, cancellationToken);
			return FromDocument(document);
		}
		catch (JsonException ex)
		{
			throw new SpreadsheetFormatException("Файл пошкоджений або не є JSON-файлом таблиці.", ex);
		}
	}

	private static SpreadsheetDocument ToDocument(Spreadsheet sheet) => new()
	{
		Format = FormatName,
		Version = CurrentVersion,
		RowCount = sheet.RowCount,
		ColumnCount = sheet.ColumnCount,
		Cells = sheet.NonEmptyCells
			.OrderBy(cell => cell.Address.Row)
			.ThenBy(cell => cell.Address.Column)
			.Select(cell => new CellDocument { Address = cell.Address.ToString(), Expression = cell.Expression })
			.ToList()
	};

	private static Spreadsheet FromDocument(SpreadsheetDocument? document)
	{
		if (document is null || document.Format != FormatName)
			throw new SpreadsheetFormatException("Файл не є таблицею цієї програми.");
		if (document.Version < 1 || document.Version > CurrentVersion)
			throw new SpreadsheetFormatException($"Непідтримувана версія файлу: {document.Version}.");
		if (document.RowCount < 1 || document.RowCount > Spreadsheet.MaxRowCount)
			throw new SpreadsheetFormatException(
				$"Некоректна кількість рядків: {document.RowCount} (має бути від 1 до {Spreadsheet.MaxRowCount}).");
		if (document.ColumnCount < 1 || document.ColumnCount > Spreadsheet.MaxColumnCount)
			throw new SpreadsheetFormatException(
				$"Некоректна кількість стовпчиків: {document.ColumnCount} (має бути від 1 до {Spreadsheet.MaxColumnCount}).");

		var sheet = new Spreadsheet(document.RowCount, document.ColumnCount);
		var expressions = new Dictionary<CellAddress, string>();

		foreach (var cell in document.Cells ?? [])
		{
			if (!CellAddress.TryParse(cell.Address, out var address))
				throw new SpreadsheetFormatException($"Некоректна адреса клітинки у файлі: '{cell.Address}'.");
			if (!sheet.IsInRange(address))
				throw new SpreadsheetFormatException($"Клітинка {address} знаходиться поза межами таблиці {document.RowCount}×{document.ColumnCount}.");
			if (!expressions.TryAdd(address, cell.Expression ?? string.Empty))
				throw new SpreadsheetFormatException($"Клітинка {address} записана у файлі двічі.");
		}

		sheet.SetExpressions(expressions);
		return sheet;
	}
}
