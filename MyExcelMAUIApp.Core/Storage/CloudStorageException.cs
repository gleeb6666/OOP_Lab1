namespace MyExcelMAUIApp.Core.Storage;

public sealed class CloudStorageException : Exception
{
	public CloudStorageException(string message, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
