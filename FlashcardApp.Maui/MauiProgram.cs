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
#if ANDROID
		SeedBundledAndroidData(dataRoot);
#else
		var bundledDataRoot = Path.Combine(AppContext.BaseDirectory, "Data");
		SeedBundledData(bundledDataRoot, dataRoot);
#endif
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

#if ANDROID
	private static void SeedBundledAndroidData(string destinationRoot)
	{
		var assets = Android.App.Application.Context?.Assets
			?? throw new InvalidOperationException("The Android application context is unavailable.");
		var bundledFiles = new List<string>();
		CollectAndroidJsonAssets(assets, "Data", bundledFiles);
		if (bundledFiles.Count == 0)
		{
			throw new InvalidDataException("No flashcard JSON assets were found in the Android package.");
		}

		foreach (var assetPath in bundledFiles)
		{
			var relativePath = assetPath["Data/".Length..].Replace('/', Path.DirectorySeparatorChar);
			var destination = Path.Combine(destinationRoot, relativePath);
			if (File.Exists(destination))
			{
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			using var source = assets.Open(assetPath);
			using var target = File.Create(destination);
			source.CopyTo(target);
		}
	}

	private static void CollectAndroidJsonAssets(Android.Content.Res.AssetManager assets, string directory, List<string> files)
	{
		var entries = assets.List(directory)
			?? throw new InvalidDataException($"Android data asset directory was not found: {directory}");
		foreach (var entry in entries)
		{
			var assetPath = $"{directory}/{entry}";
			var children = assets.List(assetPath) ?? Array.Empty<string>();
			if (children.Length > 0)
			{
				CollectAndroidJsonAssets(assets, assetPath, files);
			}
			else if (Path.GetExtension(entry).Equals(".json", StringComparison.OrdinalIgnoreCase))
			{
				files.Add(assetPath);
			}
		}
	}
#endif
}
