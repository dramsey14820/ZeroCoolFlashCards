using FlashcardApp.Services;
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
		SeedBundledData(dataRoot);
		builder.Services.AddSingleton(new StudyService(dataRoot));

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void SeedBundledData(string dataRoot)
	{
		var assembly = typeof(MauiProgram).Assembly;
		foreach (var resourceName in assembly.GetManifestResourceNames())
		{
			var normalizedName = resourceName.Replace('\\', '/');
			var dataPrefix = "Data/";
			var prefixIndex = normalizedName.IndexOf(dataPrefix, StringComparison.OrdinalIgnoreCase);
			if (prefixIndex < 0)
			{
				continue;
			}

			var relativePath = normalizedName[(prefixIndex + dataPrefix.Length)..];
			var destination = Path.Combine(dataRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
			if (File.Exists(destination))
			{
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			using var source = assembly.GetManifestResourceStream(resourceName);
			if (source is null)
			{
				continue;
			}
			using var destinationStream = File.Create(destination);
			source.CopyTo(destinationStream);
		}
	}
}
