namespace MyExcelMAUIApp.Core.Exceptions;

public sealed class EvaluationException : ExpressionException
{
	public EvaluationException(string errorCode, string message) : base(errorCode, message)
	{
	}
}
