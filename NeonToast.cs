using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace DJRussoFXHub;

// Non-modal, auto-dismissed feedback for informational outcomes only.
// Important Yes/No confirmations and failures remain synchronous NeonDialog windows.
internal static class NeonToast
{
    public static void Show(Window owner, string message, bool warning = false)
    {
        if (!owner.IsLoaded || !owner.IsVisible) return;
        Brush From(string key, Brush fallback) => owner.TryFindResource(key) as Brush ?? fallback;
        var surface = From("PanelBrush", new SolidColorBrush(Color.FromRgb(21, 26, 48)));
        var foreground = From("FgBrush", Brushes.White);
        var stroke = warning
            ? (Brush)new SolidColorBrush(Color.FromRgb(255, 183, 95))
            : From("SecondaryBrush", new SolidColorBrush(Color.FromRgb(86, 232, 255)));
        var window = new Window
        {
            Owner = owner,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowActivated = false,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Width = 390,
            SizeToContent = SizeToContent.Height,
            Topmost = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            FontFamily = new FontFamily("Segoe UI"),
            UseLayoutRounding = true,
            IsHitTestVisible = false
        };
        var border = new Border
        {
            Background = surface, BorderBrush = stroke, BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(11), Padding = new Thickness(16, 13, 16, 13),
            Margin = new Thickness(8),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(115, 86, 224),
                BlurRadius = 18,
                ShadowDepth = 0, Opacity = 0.48
            }
        };
        border.Child = new TextBlock
        {
            Text = (warning ? "⚠  " : "✓  ") + message,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            MaxWidth = 340
        };
        window.Content = border;
        void Position()
        {
            window.Left = Math.Max(SystemParameters.WorkArea.Left,
                owner.Left + owner.ActualWidth - window.Width - 22);
            window.Top = Math.Max(SystemParameters.WorkArea.Top,
                owner.Top + owner.ActualHeight - window.ActualHeight - 45);
        }
        window.Loaded += (_, _) => Position();
        owner.LocationChanged += OwnerMoved;
        owner.SizeChanged += OwnerResized;
        owner.Closed += OwnerClosed;
        void OwnerMoved(object? sender, EventArgs args) { if (window.IsVisible) Position(); }
        void OwnerResized(object? sender, SizeChangedEventArgs args) { if (window.IsVisible) Position(); }
        void OwnerClosed(object? sender, EventArgs args) { if (window.IsVisible) window.Close(); }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
        timer.Tick += (_, _) => { timer.Stop(); window.Close(); };
        window.Closed += (_, _) =>
        {
            timer.Stop();
            owner.LocationChanged -= OwnerMoved;
            owner.SizeChanged -= OwnerResized;
            owner.Closed -= OwnerClosed;
        };
        window.Show();
        timer.Start();
    }
}
