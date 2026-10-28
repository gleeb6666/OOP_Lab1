namespace MyExcelMAUIApp.Core.Parsing;

public sealed record Token(TokenType Type, string Text, int Position);
