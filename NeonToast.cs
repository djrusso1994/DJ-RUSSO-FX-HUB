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
        // Keep the toast inside both the owning window and the usable desktop area.
        // WPF coordinates and SystemParameters.WorkArea are device-independent pixels.
        void Position()
        {
            var work = SystemParameters.WorkArea;
            const double inset = 12;
            const double gap = 16;
            double ownerLeft = owner.Left;
            double ownerTop = owner.Top;
            double ownerWidth = owner.ActualWidth;
            double ownerHeight = owner.ActualHeight;
            if (owner.WindowState == WindowState.Maximized)
            {
                ownerLeft = work.Left;
                ownerTop = work.Top;
                ownerWidth = work.Width;
                ownerHeight = work.Height;
            }
            double left = Math.Max(work.Left, ownerLeft);
            double right = Math.Min(work.Right, ownerLeft + ownerWidth);
            double top = Math.Max(work.Top, ownerTop);
            double bottom = Math.Min(work.Bottom, ownerTop + ownerHeight);
            if (right <= left || bottom <= top)
            {
                left = work.Left;
                right = work.Right;
                top = work.Top;
                bottom = work.Bottom;
            }
            window.Width = Math.Max(120, Math.Min(390, Math.Min(work.Width, right - left) - 2 * inset));
            window.UpdateLayout();
            double height = window.ActualHeight;
            double x = right - window.Width - inset;
            double y = bottom - height - gap;
            window.Left = Math.Max(work.Left, Math.Min(x, work.Right - window.Width));
            window.Top = Math.Max(work.Top, Math.Min(y, work.Bottom - height));
        }
        window.Loaded += (_, _) => Position();
        owner.LocationChanged += OwnerMoved;
        owner.SizeChanged += OwnerResized;
        owner.StateChanged += OwnerStateChanged;
        owner.Closed += OwnerClosed;
        void OwnerMoved(object? sender, EventArgs args) { if (window.IsVisible) Position(); }
        void OwnerResized(object? sender, SizeChangedEventArgs args) { if (window.IsVisible) Position(); }
        void OwnerStateChanged(object? sender, EventArgs args) { if (window.IsVisible) Position(); }
        void OwnerClosed(object? sender, EventArgs args) { if (window.IsVisible) window.Close(); }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
        timer.Tick += (_, _) => { timer.Stop(); window.Close(); };
        window.Closed += (_, _) =>
        {
            timer.Stop();
            owner.LocationChanged -= OwnerMoved;
            owner.SizeChanged -= OwnerResized;
            owner.StateChanged -= OwnerStateChanged;
            owner.Closed -= OwnerClosed;
        };
        window.Show();
        timer.Start();
    }
}
