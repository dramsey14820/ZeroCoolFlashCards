using Microsoft.AspNetCore.Components.WebView;

namespace FlashcardApp.Maui;

public partial class MainPage : ContentPage
{
#if WINDOWS
	private string? _webViewProfilePath;
	private bool _blazorStartupCheckStarted;
	private Microsoft.UI.Xaml.Controls.WebView2? _observedWebView;
#endif

	public MainPage()
	{
		InitializeComponent();
		blazorWebView.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
		blazorWebView.HandlerChanged += OnBlazorWebViewHandlerChanged;
		blazorWebView.Loaded += OnBlazorWebViewLoaded;
		if (blazorWebView.Handler is not null)
		{
			OnBlazorWebViewHandlerChanged(blazorWebView, EventArgs.Empty);
		}
	}

	private void OnBlazorWebViewLoaded(object? sender, EventArgs e)
	{
		OnBlazorWebViewHandlerChanged(sender, e);
	}

	private void OnBlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
	{
#if WINDOWS
		var userDataFolder = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"ZeroCoolFlashCards",
		"WebView2");
		Directory.CreateDirectory(userDataFolder);
		_webViewProfilePath = userDataFolder;
		e.UserDataFolder = userDataFolder;
		SetStartupStatus($"WebView2 profile configured: {e.UserDataFolder}");
#endif
	}

	private void OnBlazorWebViewHandlerChanged(object? sender, EventArgs e)
	{
#if WINDOWS
		if (blazorWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 webView)
		{
			if (ReferenceEquals(_observedWebView, webView))
			{
				return;
			}

			_observedWebView = webView;
			SetStartupStatus($"WebView handler created: {blazorWebView.Handler.GetType().Name}");
			webView.CoreWebView2Initialized += OnCoreWebView2Initialized;
			webView.NavigationCompleted += OnNavigationCompleted;
			if (webView.CoreWebView2 is not null)
			{
				OnCoreWebView2Ready(webView);
			}
		}
#endif
	}

#if WINDOWS
	private void OnCoreWebView2Initialized(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.UI.Xaml.Controls.CoreWebView2InitializedEventArgs args)
	{
		if (args.Exception is not null)
		{
			SetStartupStatus($"CoreWebView2 initialization failed for configured profile '{_webViewProfilePath}': {args.Exception}");
			return;
		}

		if (sender.CoreWebView2 is null)
		{
			SetStartupStatus("CoreWebView2 initialization event completed, but CoreWebView2 is null.");
			return;
		}

		OnCoreWebView2Ready(sender);
	}

	private void OnCoreWebView2Ready(Microsoft.UI.Xaml.Controls.WebView2 webView)
	{
		var core = webView.CoreWebView2;
		if (core is null)
		{
			SetStartupStatus("CoreWebView2 is not available yet.");
			return;
		}

		if (_blazorStartupCheckStarted)
		{
			return;
		}

		_blazorStartupCheckStarted = true;
		core.NavigationStarting += (_, args) => SetStartupStatus($"Navigating to: {args.Uri}");
		core.ProcessFailed += (_, args) => SetStartupStatus($"WebView2 process failed: {args.ProcessFailedKind}");
		SetStartupStatus($"CoreWebView2 available. URL: {core.Source ?? "(none)"}");
		_ = CheckBlazorStartedAsync(webView);
	}
#endif

	private void SetStartupStatus(string message, bool visible = true)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			startupStatusLabel.Text = message;
			startupStatusPanel.IsVisible = visible;
		});
	}

#if WINDOWS
	private async Task CheckBlazorStartedAsync(Microsoft.UI.Xaml.Controls.WebView2 webView)
	{
		await Task.Delay(TimeSpan.FromSeconds(5));
		var running = await blazorWebView.TryDispatchAsync(_ => { });
		var scriptResult = await webView.CoreWebView2.ExecuteScriptAsync(
			"JSON.stringify({url:location.href,readyState:document.readyState,body:document.body?.innerText,app:document.querySelector('#app')?.innerText,bootstrap:!!document.querySelector('script[src*=blazor]'),error:document.querySelector('#blazor-error-ui')?.innerText})");
		SetStartupStatus($"Blazor running: {running}\nWebView: {scriptResult}", !running);
	}
#endif

	private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
	{
#if WINDOWS
		SetStartupStatus("WebView initialized; loading Blazor...");

		if (e.WebView is Microsoft.UI.Xaml.Controls.WebView2 webView)
		{
			if (webView.CoreWebView2 is not null)
			{
				OnCoreWebView2Ready(webView);
			}
		}
#endif
	}

#if WINDOWS
	private void OnNavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
	{
		SetStartupStatus(args.IsSuccess
			? "WebView navigation completed; waiting for Blazor..."
			: $"Blazor navigation failed: {args.WebErrorStatus}");
	}
#endif
}
