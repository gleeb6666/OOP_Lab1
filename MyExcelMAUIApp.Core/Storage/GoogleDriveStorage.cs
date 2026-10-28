using System.Runtime.ExceptionServices;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using MyExcelMAUIApp.Core.Model;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace MyExcelMAUIApp.Core.Storage;

public sealed class GoogleDriveStorage
{
	public const string ApplicationName = "MyExcelMAUIApp";
	private const string JsonMimeType = "application/json";
	private const string FileFields = "id, name, modifiedTime";

	private const string AppPropertyKey = "createdBy";
	private const string AppPropertyValue = "MyExcelMAUIApp.Spreadsheet";

	private readonly Func<CancellationToken, Task<Stream>> openClientSecrets;
	private readonly string tokenDirectory;
	private DriveService? service;

	public GoogleDriveStorage(Func<CancellationToken, Task<Stream>> openClientSecrets, string tokenDirectory)
	{
		this.openClientSecrets = openClientSecrets;
		this.tokenDirectory = tokenDirectory;
	}

	public async Task<IReadOnlyList<DriveFileInfo>> ListFilesAsync(CancellationToken cancellationToken = default)
	{
		return await RunAsync(async drive =>
		{
			var request = drive.Files.List();
			request.Q = $"appProperties has {{ key='{AppPropertyKey}' and value='{AppPropertyValue}' }} and trashed = false";
			request.Spaces = "drive";
			request.OrderBy = "modifiedTime desc";
			request.PageSize = 100;
			request.Fields = $"files({FileFields})";

			var result = await request.ExecuteAsync(cancellationToken);
			return (IReadOnlyList<DriveFileInfo>)result.Files.Select(ToInfo).ToList();
		}, cancellationToken);
	}

	public async Task<DriveFileInfo> CreateAsync(Spreadsheet sheet, string fileName, CancellationToken cancellationToken = default)
	{
		var metadata = new DriveFile
		{
			Name = NormalizeFileName(fileName),
			MimeType = JsonMimeType,
			AppProperties = new Dictionary<string, string> { [AppPropertyKey] = AppPropertyValue }
		};

		return await RunAsync(async drive =>
		{
			using var content = await SerializeAsync(sheet, cancellationToken);
			var request = drive.Files.Create(metadata, content, JsonMimeType);
			request.Fields = FileFields;
			EnsureUploaded(await request.UploadAsync(cancellationToken));
			return ToInfo(request.ResponseBody);
		}, cancellationToken);
	}

	public async Task<DriveFileInfo> UpdateAsync(Spreadsheet sheet, string fileId, CancellationToken cancellationToken = default)
	{
		return await RunAsync(async drive =>
		{
			using var content = await SerializeAsync(sheet, cancellationToken);
			var request = drive.Files.Update(new DriveFile(), fileId, content, JsonMimeType);
			request.Fields = FileFields;
			EnsureUploaded(await request.UploadAsync(cancellationToken));
			return ToInfo(request.ResponseBody);
		}, cancellationToken);
	}

	public async Task<Spreadsheet> OpenAsync(string fileId, CancellationToken cancellationToken = default)
	{
		return await RunAsync(async drive =>
		{
			using var content = new MemoryStream();
			var progress = await drive.Files.Get(fileId).DownloadAsync(content, cancellationToken);
			if (progress.Status != DownloadStatus.Completed)
				throw new CloudStorageException("Не вдалося завантажити файл з Google Drive.", progress.Exception);

			content.Position = 0;
			return await SpreadsheetJsonSerializer.DeserializeAsync(content, cancellationToken);
		}, cancellationToken);
	}

	public async Task SignOutAsync()
	{
		service?.Dispose();
		service = null;
		await new FileDataStore(tokenDirectory, fullPath: true).ClearAsync();
	}

	public static string NormalizeFileName(string fileName)
	{
		string name = fileName.Trim();
		if (name.Length == 0)
			throw new ArgumentException("Назва файлу не може бути порожньою", nameof(fileName));
		return name.EndsWith(SpreadsheetJsonSerializer.FileExtension, StringComparison.OrdinalIgnoreCase)
			? name
			: name + SpreadsheetJsonSerializer.FileExtension;
	}

	private async Task<DriveService> GetServiceAsync(CancellationToken cancellationToken)
	{
		if (service is not null)
			return service;

		GoogleClientSecrets secrets;
		try
		{
			await using var stream = await openClientSecrets(cancellationToken);
			secrets = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken);
		}
		catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
		{
			throw new CloudStorageException(
				"Не знайдено файл client_secret.json з ключем доступу до Google Drive. " +
				"Як його отримати — див. GOOGLE_DRIVE_SETUP.md у папці проєкту.", ex);
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not CloudStorageException)
		{
			throw new CloudStorageException("Файл client_secret.json пошкоджений або має неправильний формат.", ex);
		}

		var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
			secrets.Secrets,
			[DriveService.ScopeConstants.DriveFile],
			"user",
			cancellationToken,
			new FileDataStore(tokenDirectory, fullPath: true));

		service = new DriveService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName
		});
		return service;
	}

	private async Task<T> RunAsync<T>(Func<DriveService, Task<T>> action, CancellationToken cancellationToken)
	{
		try
		{
			return await action(await GetServiceAsync(cancellationToken));
		}
		catch (OperationCanceledException ex)
		{
			throw new CloudStorageException("Операцію з Google Drive скасовано або перевищено час очікування.", ex);
		}
		catch (TokenResponseException ex)
		{
			throw new CloudStorageException($"Google не надав доступ: {ex.Error?.ErrorDescription ?? ex.Error?.Error ?? ex.Message}", ex);
		}
		catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
		{
			throw new CloudStorageException("Файл не знайдено на Google Drive (можливо, його видалено).", ex);
		}
		catch (GoogleApiException ex)
		{
			throw new CloudStorageException($"Помилка Google Drive: {ex.Error?.Message ?? ex.Message}", ex);
		}
		catch (HttpRequestException ex)
		{
			throw new CloudStorageException("Немає з'єднання з Google Drive. Перевірте підключення до інтернету.", ex);
		}
	}

	private static async Task<Stream> SerializeAsync(Spreadsheet sheet, CancellationToken cancellationToken)
	{
		var stream = new MemoryStream();
		await SpreadsheetJsonSerializer.SerializeAsync(sheet, stream, cancellationToken);
		stream.Position = 0;
		return stream;
	}

	private static void EnsureUploaded(IUploadProgress progress)
	{
		if (progress.Status == UploadStatus.Completed)
			return;
		if (progress.Exception is GoogleApiException or HttpRequestException)
			ExceptionDispatchInfo.Throw(progress.Exception);
		throw new CloudStorageException("Не вдалося завантажити файл на Google Drive.", progress.Exception);
	}

	private static DriveFileInfo ToInfo(DriveFile file) =>
		new(file.Id, file.Name, file.ModifiedTimeDateTimeOffset);
}
