using System.Diagnostics.CodeAnalysis;

namespace MyExcelMAUIApp.Core.Model;

public readonly record struct CellAddress
{
	public CellAddress(int row, int column)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(row, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);
		Row = row;
		Column = column;
	}

	public int Row { get; }
	public int Column { get; }

	public override string ToString() => GetColumnName(Column) + Row;

	public static string GetColumnName(int column)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);

		string name = string.Empty;
		while (column > 0)
		{
			int modulo = (column - 1) % 26;
			name = (char)('A' + modulo) + name;
			column = (column - modulo - 1) / 26;
		}
		return name;
	}

	public static CellAddress Parse(string text) =>
		TryParse(text, out var address) ? address : throw new FormatException($"Некоректна адреса клітинки '{text}'");

	public static bool TryParse(string? text, [NotNullWhen(true)] out CellAddress address)
	{
		address = default;
		if (string.IsNullOrEmpty(text))
			return false;

		int lettersEnd = 0;
		while (lettersEnd < text.Length && char.IsAsciiLetter(text[lettersEnd]))
			lettersEnd++;
		if (lettersEnd == 0 || lettersEnd == text.Length || !text[lettersEnd..].All(char.IsAsciiDigit))
			return false;

		long column = 0;
		foreach (char c in text[..lettersEnd])
		{
			column = column * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
			if (column > int.MaxValue)
				return false;
		}

		if (!int.TryParse(text[lettersEnd..], out int row) || row < 1)
			return false;

		address = new CellAddress(row, (int)column);
		return true;
	}
}
