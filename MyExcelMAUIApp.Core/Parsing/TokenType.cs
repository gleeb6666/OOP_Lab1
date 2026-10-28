namespace MyExcelMAUIApp.Core.Parsing;

public enum TokenType
{
	Number,
	CellReference,
	Identifier,
	Plus,
	Minus,
	Multiply,
	Divide,
	Power,
	LeftParen,
	RightParen,
	Comma,
	End
}
