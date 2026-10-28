namespace MyExcelMAUIApp.Services;

public static class AppLifetime
{
	private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(150);

	public static async Task QuitAsync()
	{
		HideWindows();
		await Task.Delay(HideDelay);
		Application.Current?.Quit();
	}

	private static void HideWindows()
	{
#if WINDOWS
		foreach (var window in Application.Current?.Windows ?? [])
			(window.Handler?.PlatformView as Microsoft.UI.Xaml.Window)?.AppWindow.Hide();
#endif
	}
}
