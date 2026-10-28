namespace MyExcelMAUIApp.Core.Exceptions;

public sealed class SyntaxErrorException : ExpressionException
{
	public SyntaxErrorException(string description, int position)
		: base(ErrorCodes.Syntax, $"{description} (позиція {position + 1})")
	{
		Description = description;
		Position = position;
	}

	public string Description { get; }

	public int Position { get; }
}
