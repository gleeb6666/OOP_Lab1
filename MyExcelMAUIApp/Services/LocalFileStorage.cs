using CommunityToolkit.Maui.Storage;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Storage;

namespace MyExcelMAUIApp.Services;

public sealed class LocalFileStorage
{
	private static readonly FilePickerFileType JsonFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
	{
		[DevicePlatform.WinUI] = [SpreadsheetJsonSerializer.FileExtension]
	});

	public async Task<string?> SaveAsAsync(Spreadsheet sheet, string suggestedFileName, CancellationToken cancellationToken = default)
	{
		using var stream = new MemoryStream();
		await SpreadsheetJsonSerializer.SerializeAsync(sheet, stream, cancellationToken);
		stream.Position = 0;

		var result = await FileSaver.Default.SaveAsync(suggestedFileName, stream, cancellationToken);
		if (result.IsSuccessful)
			return result.FilePath;
		if (IsCancellation(result.Exception))
			return null;
		throw new IOException($"Не вдалося зберегти файл: {result.Exception?.Message}", result.Exception);
	}

	public async Task SaveAsync(Spreadsheet sheet, string filePath, CancellationToken cancellationToken = default)
	{
		await using var stream = File.Create(filePath);
		await SpreadsheetJsonSerializer.SerializeAsync(sheet, stream, cancellationToken);
	}

	public async Task<(Spreadsheet Sheet, string FilePath)?> OpenAsync(CancellationToken cancellationToken = default)
	{
		var file = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = "Відкрити таблицю",
			FileTypes = JsonFileType
		});
		if (file is null)
			return null;

		await using var stream = await file.OpenReadAsync();
		var sheet = await SpreadsheetJsonSerializer.DeserializeAsync(stream, cancellationToken);
		return (sheet, file.FullPath);
	}

	private static bool IsCancellation(Exception? exception) =>
		exception is OperationCanceledException
		|| exception?.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;
}
