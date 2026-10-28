namespace MyExcelMAUIApp.Core.Storage;

public sealed record DriveFileInfo(string Id, string Name, DateTimeOffset? ModifiedTime);
