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
			var webViewDataFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ZeroCoolFlashCards",
				"WebView2");
			Directory.CreateDirectory(webViewDataFolder);
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webViewDataFolder, EnvironmentVariableTarget.Process);

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
