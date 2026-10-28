namespace MyExcelMAUIApp.Services;

public abstract record DocumentLocation
{
	public abstract string FileName { get; }

	public abstract string DisplayName { get; }
}

public sealed record LocalDocument(string FilePath) : DocumentLocation
{
	public override string FileName => Path.GetFileName(FilePath);
	public override string DisplayName => FileName;
}

public sealed record DriveDocument(string FileId, string Name) : DocumentLocation
{
	public override string FileName => Name;
	public override string DisplayName => $"{Name} (Google Drive)";
}
