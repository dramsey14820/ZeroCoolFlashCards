using FlashcardApp.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;

namespace FlashcardApp.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		var dataRoot = Path.Combine(FileSystem.AppDataDirectory, "Data");
		var bundledDataRoot = Path.Combine(AppContext.BaseDirectory, "Data");
		SeedBundledData(bundledDataRoot, dataRoot);
		builder.Services.AddSingleton(new StudyService(dataRoot));

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void SeedBundledData(string sourceRoot, string destinationRoot)
	{
		if (!Directory.Exists(sourceRoot))
		{
			throw new DirectoryNotFoundException($"Installed flashcard data directory was not found: {sourceRoot}");
		}

		var files = Directory.GetFiles(sourceRoot, "*.json", SearchOption.AllDirectories);
		if (files.Length == 0)
		{
			throw new InvalidDataException($"No flashcard JSON files were found in: {sourceRoot}");
		}

		foreach (var sourcePath in files)
		{
			var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
			var destination = Path.Combine(destinationRoot, relativePath);
			if (File.Exists(destination))
			{
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(sourcePath, destination);
		}
	}
}
