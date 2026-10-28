using MyExcelMAUIApp.Core.Model;
using Cell = MyExcelMAUIApp.Core.Model.Cell;
using Grid = Microsoft.Maui.Controls.Grid;

namespace MyExcelMAUIApp.Controls;

public sealed class SpreadsheetGridView : ContentView
{
	const double CellWidth = 110;
	static readonly Color ErrorColor = Colors.Red;

	readonly Grid grid = new() { ColumnSpacing = 1, RowSpacing = 1 };
	readonly Dictionary<CellAddress, Entry> entries = new();
	Spreadsheet sheet = new();
	DisplayMode mode = DisplayMode.Value;

	public SpreadsheetGridView()
	{
		Content = new ScrollView { Orientation = ScrollOrientation.Both, Content = grid };
		BuildGrid();
	}

	public event EventHandler<CellAddress?>? CellSelected;

	public event EventHandler<string>? SelectedCellTextChanged;

	public event EventHandler? SheetChanged;

	public Spreadsheet Sheet
	{
		get => sheet;
		set
		{
			sheet = value;
			BuildGrid();
			Select(null);
		}
	}

	public DisplayMode Mode
	{
		get => mode;
		set
		{
			mode = value;
			Refresh();
		}
	}

	public CellAddress? SelectedAddress { get; private set; }

	public Cell? SelectedCell => SelectedAddress is { } address ? sheet.GetCell(address) : null;

	public void CommitExpression(CellAddress address, string? expression)
	{
		string text = expression?.Trim() ?? string.Empty;
		if (text != sheet.GetCell(address).Expression)
		{
			sheet.SetExpression(address, text);
			Refresh();
			SheetChanged?.Invoke(this, EventArgs.Empty);
		}
		else if (entries.TryGetValue(address, out var entry) && !entry.IsFocused)
		{
			ShowCell(entry, address);
		}
	}

	public void CommitFocusedCell()
	{
		foreach (var (address, entry) in entries)
		{
			if (entry.IsFocused)
			{
				CommitExpression(address, entry.Text);
				return;
			}
		}
	}

	public void PreviewSelectedCellText(string text)
	{
		if (SelectedAddress is { } address && entries.TryGetValue(address, out var entry))
		{
			entry.TextColor = null!;
			entry.Text = text;
		}
	}

	public void Recalculate()
	{
		sheet.Recalculate();
		Refresh();
	}

	public void AddRow()
	{
		sheet.AddRow();
		AddRowControls(sheet.RowCount);
		SheetChanged?.Invoke(this, EventArgs.Empty);
	}

	public void AddColumn()
	{
		sheet.AddColumn();
		AddColumnControls(sheet.ColumnCount);
		SheetChanged?.Invoke(this, EventArgs.Empty);
	}

	public void RemoveLastRow()
	{
		int row = sheet.RowCount;
		sheet.RemoveLastRow();
		RemoveControls(view => grid.GetRow(view) == row);
		grid.RowDefinitions.RemoveAt(row);
		AfterRemove();
	}

	public void RemoveLastColumn()
	{
		int column = sheet.ColumnCount;
		sheet.RemoveLastColumn();
		RemoveControls(view => grid.GetColumn(view) == column);
		grid.ColumnDefinitions.RemoveAt(column);
		AfterRemove();
	}

	public void Refresh()
	{
		foreach (var (address, entry) in entries)
		{
			if (!entry.IsFocused)
				ShowCell(entry, address);
		}
	}

	private void BuildGrid()
	{
		grid.Children.Clear();
		grid.RowDefinitions.Clear();
		grid.ColumnDefinitions.Clear();
		entries.Clear();

		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

		for (int column = 1; column <= sheet.ColumnCount; column++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			grid.Add(CreateHeader(CellAddress.GetColumnName(column)), column, 0);
		}

		for (int row = 1; row <= sheet.RowCount; row++)
			AddRowControls(row);
	}

	private void AddRowControls(int row)
	{
		if (grid.RowDefinitions.Count <= row)
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		grid.Add(CreateHeader(row.ToString()), 0, row);

		for (int column = 1; column <= sheet.ColumnCount; column++)
			AddCellEntry(new CellAddress(row, column));
	}

	private void AddColumnControls(int column)
	{
		grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		grid.Add(CreateHeader(CellAddress.GetColumnName(column)), column, 0);

		for (int row = 1; row <= sheet.RowCount; row++)
			AddCellEntry(new CellAddress(row, column));
	}

	private static Label CreateHeader(string text) => new()
	{
		Text = text,
		FontAttributes = FontAttributes.Bold,
		Padding = new Thickness(8, 4),
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center
	};

	private void AddCellEntry(CellAddress address)
	{
		var entry = new Entry { WidthRequest = CellWidth };
		entry.Focused += Entry_Focused;
		entry.Unfocused += Entry_Unfocused;
		entry.TextChanged += Entry_TextChanged;
		entry.Completed += Entry_Completed;

		entries[address] = entry;
		ShowCell(entry, address);
		grid.Add(entry, address.Column, address.Row);
	}

	private void ShowCell(Entry entry, CellAddress address)
	{
		var cell = sheet.GetCell(address);
		bool showValue = mode == DisplayMode.Value;
		entry.Text = showValue ? cell.DisplayValue : cell.Expression;
		entry.TextColor = showValue && cell.Error is not null ? ErrorColor : null!;
	}

	private void RemoveControls(Func<IView, bool> predicate)
	{
		foreach (var view in grid.Children.Where(predicate).ToList())
		{
			grid.Children.Remove(view);
			if (view is Entry entry)
				entries.Remove(AddressOf(entry));
		}
	}

	private void AfterRemove()
	{
		if (SelectedAddress is { } address && !sheet.IsInRange(address))
			Select(null);
		Refresh();
		SheetChanged?.Invoke(this, EventArgs.Empty);
	}

	private CellAddress AddressOf(Entry entry) => new(grid.GetRow(entry), grid.GetColumn(entry));

	private void Select(CellAddress? address)
	{
		SelectedAddress = address;
		CellSelected?.Invoke(this, address);
	}

	private void Entry_Focused(object? sender, FocusEventArgs e)
	{
		var entry = (Entry)sender!;
		var address = AddressOf(entry);
		entry.TextColor = null!;
		entry.Text = sheet.GetCell(address).Expression;
		Select(address);
	}

	private void Entry_Unfocused(object? sender, FocusEventArgs e)
	{
		var entry = (Entry)sender!;
		var address = AddressOf(entry);
		if (!sheet.IsInRange(address))
			return;

		CommitExpression(address, entry.Text);
		ShowCell(entry, address);
	}

	private void Entry_TextChanged(object? sender, TextChangedEventArgs e)
	{
		if (((Entry)sender!).IsFocused)
			SelectedCellTextChanged?.Invoke(this, e.NewTextValue ?? string.Empty);
	}

	private void Entry_Completed(object? sender, EventArgs e)
	{
		var entry = (Entry)sender!;
		var address = AddressOf(entry);
		if (address.Row < sheet.RowCount && entries.TryGetValue(new CellAddress(address.Row + 1, address.Column), out var below))
			below.Focus();
		else
			entry.Unfocus();
	}
}
