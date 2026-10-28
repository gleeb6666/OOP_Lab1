using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Storage;

namespace MyExcelMAUIApp.Tests;

public class GoogleDriveStorageTests
{
	[Theory]
	[InlineData("Звіт", "Звіт.json")]
	[InlineData("  Звіт  ", "Звіт.json")]
	[InlineData("table.json", "table.json")]
	[InlineData("table.JSON", "table.JSON")]
	public void NormalizeFileName_AddsJsonExtension(string name, string expected)
	{
		Assert.Equal(expected, GoogleDriveStorage.NormalizeFileName(name));
	}

	[Fact]
	public void NormalizeFileName_Empty_Throws()
	{
		Assert.Throws<ArgumentException>(() => GoogleDriveStorage.NormalizeFileName("   "));
	}

	[Fact]
	public async Task MissingClientSecrets_GivesUnderstandableError()
	{
		var storage = new GoogleDriveStorage(
			_ => throw new FileNotFoundException("client_secret.json"),
			Path.Combine(Path.GetTempPath(), "lr1-tests-token"));

		var ex = await Assert.ThrowsAsync<CloudStorageException>(() => storage.ListFilesAsync());
		Assert.Contains("client_secret.json", ex.Message);
		Assert.Contains("GOOGLE_DRIVE_SETUP.md", ex.Message);
	}

	[Fact]
	public async Task BrokenClientSecrets_GivesUnderstandableError()
	{
		var storage = new GoogleDriveStorage(
			_ => Task.FromResult<Stream>(new MemoryStream("not json"u8.ToArray())),
			Path.Combine(Path.GetTempPath(), "lr1-tests-token"));

		var ex = await Assert.ThrowsAsync<CloudStorageException>(() => storage.CreateAsync(new Spreadsheet(1, 1), "x"));
		Assert.Contains("пошкоджений", ex.Message);
	}
}
