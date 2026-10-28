using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using MyExcelMAUIApp.Services;

namespace MyExcelMAUIApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureLifecycleEvents(events =>
			{
#if WINDOWS
				events.AddWindows(windows => windows.OnWindowCreated(ConfirmCloseWithUnsavedChanges));
#endif
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

#if WINDOWS
	private static void ConfirmCloseWithUnsavedChanges(Microsoft.UI.Xaml.Window window)
	{
		bool closeConfirmed = false;
		window.AppWindow.Closing += async (_, args) =>
		{
			if (closeConfirmed || Shell.Current?.CurrentPage is not MainPage page)
				return;

			args.Cancel = true;
			page.CommitPendingEdits();
			if (!page.HasUnsavedChanges || await page.ConfirmDiscardChangesAsync())
			{
				closeConfirmed = true;
				await AppLifetime.QuitAsync();
			}
		};
	}
#endif
}
