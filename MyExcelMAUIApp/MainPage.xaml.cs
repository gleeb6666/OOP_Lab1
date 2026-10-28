using MyExcelMAUIApp.Controls;
using MyExcelMAUIApp.Core.Model;
using MyExcelMAUIApp.Core.Storage;
using MyExcelMAUIApp.Services;

namespace MyExcelMAUIApp;

public partial class MainPage : ContentPage
{
	const string AppTitle = "Електронна таблиця";
	const string DefaultFileName = "Таблиця" + SpreadsheetJsonSerializer.FileExtension;

	const string StudentName = "Максимов Гліб Максимович";
	const string StudentGroup = "група К-28";

	const string HelpText =
		"Лабораторна робота №1 (і №1*). Електронна таблиця. Варіант 12.\n" +
		"Виконав: " + StudentName + ", " + StudentGroup + ".\n\n" +
		"Клітинка містить вираз із цілих чисел довільної довжини, круглих дужок " +
		"та посилань на інші клітинки (A1, B12, AA3).\n\n" +
		"Операції:\n" +
		"  +  -  *  /    — додавання, віднімання, множення, цілочисельне ділення (7/2 = 3)\n" +
		"  ^                — піднесення до степеня (2^3^2 = 2^9)\n" +
		"  inc(x), dec(x) — x+1 і x−1\n" +
		"  mmax(x1, …, xN), mmin(x1, …, xN) — найбільше та найменше з N ≥ 1 чисел\n\n" +
		"Унарного мінуса немає: від'ємне число записуйте як 0-5.\n" +
		"Порожня клітинка дорівнює 0.\n\n" +
		"Як працювати:\n" +
		"  • клацніть клітинку і введіть вираз у неї або в рядок формул угорі, Enter — підтвердити;\n" +
		"  • «Режим» перемикає показ ВИРАЗІВ або ЗНАЧЕНЬ;\n" +
		"  • під рядком формул видно значення або опис помилки виділеної клітинки;\n" +
		"  • «Зберегти» записує таблицю у JSON-файл, «Прочитати» відкриває збережену таблицю;\n" +
		"  • «Зберегти в Google Drive» і «Відкрити з Google Drive» працюють з вашим Google Drive " +
		"(уперше відкриється браузер для входу в Google). Програма бачить лише файли, які сама створила.\n\n" +
		"Помилки у клітинках: #SYNTAX! — синтаксична помилка (з позицією), #DIV/0! — ділення на нуль, " +
		"#NUM! — некоректний степінь, #REF! — посилання за межі таблиці, #CYCLE! — циклічне посилання.";

	static readonly Color ErrorColor = Colors.Red;
	static readonly Color InfoColor = Colors.Gray;

	const string ClientSecretsFileName = "client_secret.json";

	static readonly TimeSpan DriveTimeout = TimeSpan.FromMinutes(5);

	static readonly TimeSpan DialogCloseDelay = TimeSpan.FromMilliseconds(400);

	readonly LocalFileStorage localStorage = new();
	readonly GoogleDriveStorage driveStorage = new(
		_ => FileSystem.OpenAppPackageFileAsync(ClientSecretsFileName),
		Path.Combine(FileSystem.AppDataDirectory, "GoogleDrive"));

	DocumentLocation? currentDocument;

	public MainPage()
	{
		InitializeComponent();

		spreadsheetView.CellSelected += SpreadsheetView_CellSelected;
		spreadsheetView.SelectedCellTextChanged += (_, text) => formulaEntry.Text = text;
		spreadsheetView.SheetChanged += SpreadsheetView_SheetChanged;
		UpdateModeButton();
	}

	public bool HasUnsavedChanges { get; private set; }

	public void CommitPendingEdits()
	{
		if (formulaEntry.IsFocused)
			CommitFormula();
		spreadsheetView.CommitFocusedCell();
	}

	public async Task<bool> ConfirmDiscardChangesAsync()
	{
		CommitPendingEdits();
		if (!HasUnsavedChanges)
			return true;

		const string save = "Зберегти";
		const string discard = "Не зберігати";
		var choice = await DisplayActionSheet("У таблиці є незбережені зміни. Зберегти їх?", "Скасувати", null, save, discard);
		await Task.Delay(DialogCloseDelay);
		return choice switch
		{
			save => await SaveAsync(chooseFile: false),
			discard => true,
			_ => false
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		UpdateTitle();
	}

	private void SpreadsheetView_CellSelected(object? sender, CellAddress? address)
	{
		selectedCellLabel.Text = address?.ToString() ?? string.Empty;
		formulaEntry.Text = spreadsheetView.SelectedCell?.Expression ?? string.Empty;
		formulaEntry.IsEnabled = address is not null;
		UpdateStatus();
	}

	private void SpreadsheetView_SheetChanged(object? sender, EventArgs e)
	{
		HasUnsavedChanges = true;
		UpdateTitle();
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		var cell = spreadsheetView.SelectedCell;
		if (cell is null || cell.IsEmpty)
			ShowStatus(string.Empty);
		else if (cell.Error is { } error)
			ShowStatus($"{cell.Address}: {error.Message}", ErrorColor);
		else
			ShowStatus($"{cell.Address} = {cell.Value}");
	}

	private void ShowStatus(string text, Color? color = null)
	{
		statusLabel.Text = text;
		statusLabel.TextColor = color ?? InfoColor;
	}

	private void UpdateTitle()
	{
		if (Window is null)
			return;
		string fileName = currentDocument?.DisplayName ?? "Нова таблиця";
		Window.Title = $"{fileName}{(HasUnsavedChanges ? "*" : "")} — {AppTitle}";
	}

	private void UpdateModeButton()
	{
		modeButton.Text = spreadsheetView.Mode == DisplayMode.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗ";
	}

	private void CommitFormula()
	{
		if (spreadsheetView.SelectedAddress is { } address)
		{
			spreadsheetView.CommitExpression(address, formulaEntry.Text);
			UpdateStatus();
		}
	}

	private void FormulaEntry_Completed(object? sender, EventArgs e) => CommitFormula();

	private void FormulaEntry_Unfocused(object? sender, FocusEventArgs e) => CommitFormula();

	private void FormulaEntry_TextChanged(object? sender, TextChangedEventArgs e)
	{
		if (formulaEntry.IsFocused)
			spreadsheetView.PreviewSelectedCellText(e.NewTextValue ?? string.Empty);
	}

	private void ModeButton_Clicked(object? sender, EventArgs e)
	{
		spreadsheetView.Mode = spreadsheetView.Mode == DisplayMode.Value ? DisplayMode.Expression : DisplayMode.Value;
		UpdateModeButton();
	}

	private void CalculateButton_Clicked(object? sender, EventArgs e)
	{
		spreadsheetView.Recalculate();
		UpdateStatus();
	}

	private async void SaveButton_Clicked(object? sender, EventArgs e) => await SaveAsync(chooseFile: false);

	private async void SaveAsButton_Clicked(object? sender, EventArgs e) => await SaveAsync(chooseFile: true);

	private async Task<bool> SaveAsync(bool chooseFile)
	{
		CommitPendingEdits();
		var sheet = spreadsheetView.Sheet;

		if (!chooseFile && currentDocument is DriveDocument driveDocument)
		{
			return await RunDriveOperationAsync("Збереження на Google Drive…", async cancellationToken =>
			{
				var saved = await driveStorage.UpdateAsync(sheet, driveDocument.FileId, cancellationToken);
				MarkSaved(new DriveDocument(saved.Id, saved.Name), "на Google Drive");
				return true;
			});
		}

		try
		{
			if (!chooseFile && currentDocument is LocalDocument localDocument)
			{
				await localStorage.SaveAsync(sheet, localDocument.FilePath);
				MarkSaved(localDocument, $"у файл {localDocument.FilePath}");
			}
			else
			{
				var path = await localStorage.SaveAsAsync(sheet, currentDocument?.FileName ?? DefaultFileName);
				if (path is null)
					return false;
				MarkSaved(new LocalDocument(path), $"у файл {path}");
			}
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			await DisplayAlert("Помилка збереження", ex.Message, "OK");
			return false;
		}
	}

	private void MarkSaved(DocumentLocation document, string where)
	{
		currentDocument = document;
		HasUnsavedChanges = false;
		UpdateTitle();
		ShowStatus($"Таблицю збережено {where}");
	}

	private void LoadSheet(Spreadsheet sheet, DocumentLocation document, string from)
	{
		spreadsheetView.Sheet = sheet;
		currentDocument = document;
		HasUnsavedChanges = false;
		UpdateTitle();
		ShowStatus($"Відкрито {from}");
	}

	private async void SaveToDriveButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdits();
		string suggestedName = Path.GetFileNameWithoutExtension(currentDocument?.FileName ?? DefaultFileName);
		var name = await DisplayPromptAsync("Зберегти в Google Drive", "Назва файлу на Google Drive:",
			"Зберегти", "Скасувати", initialValue: suggestedName, maxLength: 100);
		if (string.IsNullOrWhiteSpace(name))
			return;
		await Task.Delay(DialogCloseDelay);

		string fileName = GoogleDriveStorage.NormalizeFileName(name);
		var sheet = spreadsheetView.Sheet;

		await RunDriveOperationAsync("Збереження на Google Drive…", async cancellationToken =>
		{
			var files = await driveStorage.ListFilesAsync(cancellationToken);
			var existing = files.FirstOrDefault(file => string.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase));

			DriveFileInfo saved;
			if (existing is null)
			{
				saved = await driveStorage.CreateAsync(sheet, fileName, cancellationToken);
			}
			else
			{
				if (!await DisplayAlert("Файл уже існує", $"На Google Drive вже є таблиця «{existing.Name}». Замінити її?", "Замінити", "Скасувати"))
					return false;
				await Task.Delay(DialogCloseDelay);
				saved =await driveStorage.UpdateAsync(sheet, existing.Id, cancellationToken);
			}

			MarkSaved(new DriveDocument(saved.Id, saved.Name), "на Google Drive");
			return true;
		});
	}

	private async void OpenFromDriveButton_Clicked(object? sender, EventArgs e)
	{
		if (!await ConfirmDiscardChangesAsync())
			return;

		await RunDriveOperationAsync("Отримання списку таблиць з Google Drive…", async cancellationToken =>
		{
			var files = await driveStorage.ListFilesAsync(cancellationToken);
			if (files.Count == 0)
			{
				await DisplayAlert("Google Drive", "На Google Drive ще немає таблиць, збережених цією програмою.", "OK");
				return false;
			}

			var items = files
				.Select((file, i) => $"{i + 1}. {file.Name}" + (file.ModifiedTime is { } time ? $"  ({time.LocalDateTime:dd.MM.yyyy HH:mm})" : ""))
				.ToArray();
			var choice = await DisplayActionSheet("Відкрити з Google Drive", "Скасувати", null, items);
			int index = Array.IndexOf(items, choice);
			if (index < 0)
				return false;
			await Task.Delay(DialogCloseDelay);

			var file = files[index];
			ShowStatus($"Завантаження «{file.Name}» з Google Drive…");
			var sheet = await driveStorage.OpenAsync(file.Id, cancellationToken);
			LoadSheet(sheet, new DriveDocument(file.Id, file.Name), $"«{file.Name}» з Google Drive");
			return true;
		});
	}

	private async Task<bool> RunDriveOperationAsync(string busyMessage, Func<CancellationToken, Task<bool>> operation)
	{
		SetBusy(true);
		ShowStatus(busyMessage);
		using var timeout = new CancellationTokenSource(DriveTimeout);
		try
		{
			return await operation(timeout.Token);
		}
		catch (CloudStorageException ex)
		{
			ShowStatus(string.Empty);
			await DisplayAlert("Google Drive", ex.Message, "OK");
		}
		catch (SpreadsheetFormatException ex)
		{
			ShowStatus(string.Empty);
			await DisplayAlert("Помилка читання", $"Файл з Google Drive не вдалося прочитати як таблицю.\n{ex.Message}", "OK");
		}
		finally
		{
			SetBusy(false);
		}
		return false;
	}

	private void SetBusy(bool busy)
	{
		toolbar.IsEnabled = !busy;
		formulaEntry.IsEnabled = !busy && spreadsheetView.SelectedAddress is not null;
		busyOverlay.IsVisible = busy;
	}

	private async void ReadButton_Clicked(object? sender, EventArgs e)
	{
		if (!await ConfirmDiscardChangesAsync())
			return;

		try
		{
			var opened = await localStorage.OpenAsync();
			if (opened is not { } result)
				return;

			LoadSheet(result.Sheet, new LocalDocument(result.FilePath), $"файл {result.FilePath}");
		}
		catch (SpreadsheetFormatException ex)
		{
			await DisplayAlert("Помилка читання", $"Не вдалося прочитати таблицю.\n{ex.Message}", "OK");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			await DisplayAlert("Помилка читання", $"Не вдалося відкрити файл.\n{ex.Message}", "OK");
		}
	}

	private async void ExitButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdits();
		bool canExit = HasUnsavedChanges
			? await ConfirmDiscardChangesAsync()
			: await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
		if (canExit)
			await AppLifetime.QuitAsync();
	}

	private async void HelpButton_Clicked(object? sender, EventArgs e)
	{
		await DisplayAlert("Довідка", HelpText, "OK");
	}

	private async void AddRowButton_Clicked(object? sender, EventArgs e)
	{
		if (spreadsheetView.Sheet.RowCount == Spreadsheet.MaxRowCount)
		{
			await DisplayAlert("Додавання рядка", $"У таблиці може бути не більше {Spreadsheet.MaxRowCount} рядків.", "OK");
			return;
		}
		spreadsheetView.AddRow();
	}

	private async void AddColumnButton_Clicked(object? sender, EventArgs e)
	{
		if (spreadsheetView.Sheet.ColumnCount == Spreadsheet.MaxColumnCount)
		{
			await DisplayAlert("Додавання стовпчика", $"У таблиці може бути не більше {Spreadsheet.MaxColumnCount} стовпчиків.", "OK");
			return;
		}
		spreadsheetView.AddColumn();
	}

	private async void DeleteRowButton_Clicked(object? sender, EventArgs e)
	{
		var sheet = spreadsheetView.Sheet;
		if (sheet.RowCount == 1)
		{
			await DisplayAlert("Видалення рядка", "У таблиці має залишитися хоча б один рядок.", "OK");
			return;
		}

		int row = sheet.RowCount;
		bool hasData = sheet.NonEmptyCells.Any(cell => cell.Address.Row == row);
		if (await ConfirmRemovalAsync($"Рядок {row}", hasData, sheet.GetCellsReferencingRow(row)))
			spreadsheetView.RemoveLastRow();
	}

	private async void DeleteColumnButton_Clicked(object? sender, EventArgs e)
	{
		var sheet = spreadsheetView.Sheet;
		if (sheet.ColumnCount == 1)
		{
			await DisplayAlert("Видалення стовпчика", "У таблиці має залишитися хоча б один стовпчик.", "OK");
			return;
		}

		int column = sheet.ColumnCount;
		bool hasData = sheet.NonEmptyCells.Any(cell => cell.Address.Column == column);
		var name = CellAddress.GetColumnName(column);
		if (await ConfirmRemovalAsync($"Стовпчик {name}", hasData, sheet.GetCellsReferencingColumn(column)))
			spreadsheetView.RemoveLastColumn();
	}

	private async Task<bool> ConfirmRemovalAsync(string target, bool hasData, IReadOnlyList<CellAddress> referencingCells)
	{
		if (!hasData && referencingCells.Count == 0)
			return true;

		var message = hasData ? $"{target} містить дані, вони будуть втрачені." : $"{target} порожній.";
		if (referencingCells.Count > 0)
			message += $"\nНа нього посилаються клітинки {string.Join(", ", referencingCells)} — після видалення в них буде помилка #REF!.";
		message += "\n\nВидалити?";

		return await DisplayAlert("Підтвердження видалення", message, "Видалити", "Скасувати");
	}
}
