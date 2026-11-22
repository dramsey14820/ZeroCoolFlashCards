namespace FlashcardApp.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
#if WINDOWS
			var webViewDataFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ZeroCoolFlashCards",
				"WebView2");
			Directory.CreateDirectory(webViewDataFolder);
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webViewDataFolder, EnvironmentVariableTarget.Process);
#endif

			return new Window(new MainPage()) { Title = "FlashcardApp.Maui" };
		}
		catch (Exception ex)
		{
			return new Window(new ContentPage
			{
				Content = new ScrollView
				{
					Content = new Label
					{
						Text = $"Startup failed:\n\n{ex}",
						Padding = 24
					}
				}
			}) { Title = "FlashcardApp.Maui - Startup Error" };
		}
	}
}
