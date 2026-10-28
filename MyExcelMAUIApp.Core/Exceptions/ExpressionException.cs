namespace MyExcelMAUIApp.Core.Exceptions;

public abstract class ExpressionException : Exception
{
	protected ExpressionException(string errorCode, string message) : base(message)
	{
		ErrorCode = errorCode;
	}

	public string ErrorCode { get; }
}
