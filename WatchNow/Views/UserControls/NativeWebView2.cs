using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Web.WebView2.Core;

namespace WatchNow.Avalonia.Views.UserControls
{
    /// <summary>
    /// A native WebView2 control for Avalonia using NativeControlHost.
    /// Provides direct access to the CoreWebView2 API without third-party wrappers.
    /// </summary>
    public class NativeWebView2 : NativeControlHost
    {
        private CoreWebView2Controller _controller;
        private CoreWebView2Environment _environment;
        private bool _isInitialized;
        private string _pendingUrl;

        public static readonly StyledProperty<Uri> UrlProperty =
            AvaloniaProperty.Register<NativeWebView2, Uri>(nameof(Url));

        public static readonly StyledProperty<CoreWebView2PreferredColorScheme> PreferredColorSchemeProperty =
            AvaloniaProperty.Register<NativeWebView2, CoreWebView2PreferredColorScheme>(
                nameof(PreferredColorScheme), CoreWebView2PreferredColorScheme.Auto);

        public static readonly StyledProperty<bool> UseEdgeCookiesProperty =
            AvaloniaProperty.Register<NativeWebView2, bool>(nameof(UseEdgeCookies), false);

        public Uri Url
        {
            get => GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public CoreWebView2PreferredColorScheme PreferredColorScheme
        {
            get => GetValue(PreferredColorSchemeProperty);
            set => SetValue(PreferredColorSchemeProperty, value);
        }

        /// <summary>
        /// When true, uses Edge's default profile user data folder so that
        /// WebView2 shares the same cookies/sessions as the user's Edge browser.
        /// </summary>
        public bool UseEdgeCookies
        {
            get => GetValue(UseEdgeCookiesProperty);
            set => SetValue(UseEdgeCookiesProperty, value);
        }

        /// <summary>
        /// Provides access to the underlying CoreWebView2 instance after initialization.
        /// </summary>
        public CoreWebView2 CoreWebView2 => _controller?.CoreWebView2;

        /// <summary>Raised when the CoreWebView2 has been initialized and is ready to use.</summary>
        public event EventHandler WebView2Initialized;

        /// <summary>Raised when a navigation completes.</summary>
        public event EventHandler<CoreWebView2NavigationCompletedEventArgs> NavigationCompleted;

        /// <summary>Raised when the ContainsFullScreenElement property changes.</summary>
        public event EventHandler<object> ContainsFullScreenElementChanged;

        public NativeWebView2()
        {
            this.GetObservable(BoundsProperty).Subscribe(new BoundsObserver(this));

            if (Application.Current != null)
            {
                SyncColorSchemeToTheme(Application.Current.ActualThemeVariant);
                Application.Current.ActualThemeVariantChanged += (_, _) =>
                {
                    SyncColorSchemeToTheme(Application.Current.ActualThemeVariant);
                };
            }
        }

        private void SyncColorSchemeToTheme(ThemeVariant theme)
        {
            PreferredColorScheme = theme == ThemeVariant.Dark
                ? CoreWebView2PreferredColorScheme.Dark
                : CoreWebView2PreferredColorScheme.Light;
        }

        private class BoundsObserver : IObserver<Rect>
        {
            private readonly NativeWebView2 _owner;
            public BoundsObserver(NativeWebView2 owner) => _owner = owner;
            public void OnNext(Rect value) => _owner.UpdateControllerBounds();
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }

        static NativeWebView2()
        {
            UrlProperty.Changed.AddClassHandler<NativeWebView2>((control, args) =>
            {
                control.OnUrlChanged(args.GetNewValue<Uri>());
            });

            PreferredColorSchemeProperty.Changed.AddClassHandler<NativeWebView2>((control, args) =>
            {
                control.OnPreferredColorSchemeChanged(args.GetNewValue<CoreWebView2PreferredColorScheme>());
            });
        }

        private void OnUrlChanged(Uri newUrl)
        {
            if (newUrl == null) return;

            if (_isInitialized && _controller?.CoreWebView2 != null)
            {
                _controller.CoreWebView2.Navigate(newUrl.AbsoluteUri);
            }
            else
            {
                _pendingUrl = newUrl.AbsoluteUri;
            }
        }

        private void OnPreferredColorSchemeChanged(CoreWebView2PreferredColorScheme scheme)
        {
            if (_isInitialized && _controller?.CoreWebView2 != null)
            {
                _controller.CoreWebView2.Profile.PreferredColorScheme = scheme;
            }
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var handle = base.CreateNativeControlCore(parent);
            _ = InitializeWebView2Async(handle.Handle);
            return handle;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_controller != null)
            {
                _controller.Close();
                _controller = null;
            }

            _isInitialized = false;
            base.DestroyNativeControlCore(control);
        }

        private async Task InitializeWebView2Async(IntPtr parentHwnd)
        {
            try
            {
                string userDataFolder;
                if (UseEdgeCookies)
                {
                    // Point to Edge's default profile to share cookies/auth sessions.
                    userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "Edge", "User Data");
                }
                else
                {
                    userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "WatchNow", "WebView2");
                }

                var options = new CoreWebView2EnvironmentOptions
                {
                    AllowSingleSignOnUsingOSPrimaryAccount = true
                };

                _environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: options);

                _controller = await _environment.CreateCoreWebView2ControllerAsync(parentHwnd);

                _controller.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                _controller.IsVisible = true;

                _controller.CoreWebView2.Profile.PreferredColorScheme = PreferredColorScheme;

                // Wire up events before raising Initialized so subscribers can use CoreWebView2
                _controller.CoreWebView2.NavigationCompleted += (s, e) =>
                    NavigationCompleted?.Invoke(this, e);
                _controller.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
                    ContainsFullScreenElementChanged?.Invoke(this, e);

                _isInitialized = true;

                Dispatcher.UIThread.Post(() => UpdateControllerBounds(), DispatcherPriority.Render);

                WebView2Initialized?.Invoke(this, EventArgs.Empty);

                if (!string.IsNullOrEmpty(_pendingUrl))
                {
                    _controller.CoreWebView2.Navigate(_pendingUrl);
                    _pendingUrl = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            UpdateControllerBounds();
            return result;
        }

        private void UpdateControllerBounds()
        {
            if (_controller == null) return;

            var scaling = VisualRoot is TopLevel topLevel
                ? topLevel.RenderScaling
                : 1.0;

            var bounds = Bounds;
            int width = Math.Max(1, (int)(bounds.Width * scaling));
            int height = Math.Max(1, (int)(bounds.Height * scaling));

            _controller.Bounds = new System.Drawing.Rectangle(0, 0, width, height);
            _controller.IsVisible = true;
        }

        /// <summary>Executes JavaScript in the WebView2 context.</summary>
        public Task<string> ExecuteScriptAsync(string script)
        {
            if (_controller?.CoreWebView2 != null)
            {
                return _controller.CoreWebView2.ExecuteScriptAsync(script);
            }
            return Task.FromResult<string>(null);
        }

        /// <summary>Opens the WebView2 DevTools window.</summary>
        public void OpenDevToolsWindow()
        {
            _controller?.CoreWebView2?.OpenDevToolsWindow();
        }
    }
}
