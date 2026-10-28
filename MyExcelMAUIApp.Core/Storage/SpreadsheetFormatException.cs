namespace MyExcelMAUIApp.Core.Storage;

public sealed class SpreadsheetFormatException : Exception
{
	public SpreadsheetFormatException(string message, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
