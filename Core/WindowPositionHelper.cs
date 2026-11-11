using System;
using System.Windows;
using System.Windows.Media;

namespace SOLUM_UI.Core
{
    public static class WindowPositionHelper
    {
        /// <summary>
        /// Centers the dialog cleanly over its Owner window (or primary work area if no owner).
        /// Safely sets Owner ONLY if the dialog is not yet shown, and sets WindowStartupLocation.CenterOwner.
        /// </summary>
        public static void CenterOnOwner(Window dialog, Window owner = null)
        {
            if (dialog == null) return;
            try
            {
                var targetOwner = owner ?? dialog.Owner;
                if (targetOwner == null && Application.Current?.MainWindow != null && Application.Current.MainWindow != dialog && Application.Current.MainWindow.IsVisible)
                {
                    targetOwner = Application.Current.MainWindow;
                }

                if (targetOwner != null && targetOwner != dialog && targetOwner.IsVisible)
                {
                    // Only set Owner if not already set, and only if dialog is not yet shown/loaded
                    if (dialog.Owner == null && !dialog.IsLoaded && !dialog.IsVisible)
                    {
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(dialog).Handle;
                        if (hwnd == IntPtr.Zero)
                        {
                            dialog.Owner = targetOwner;
                        }
                    }
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterOnOwner error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sizes and positions a transparent/dimmed modal Window so it exactly covers the entire Owner window,
        /// including the TitleBar, Sidebar (Navbar), and TopBar (Header).
        /// Properly calculates the DIP screen position even when Owner is Maximized or on high-DPI displays.
        /// NOTE: Does NOT assign dialog.Owner, avoiding "Cannot set Owner property after Dialog is shown".
        /// </summary>
        public static void CoverOwnerWindow(Window dialog, FrameworkElement dialogShell = null)
        {
            if (dialog == null) return;
            var owner = dialog.Owner ?? Application.Current?.MainWindow;
            double screenW, screenH, screenLeft, screenTop;

            if (owner != null && owner != dialog && owner.ActualWidth > 0 && owner.ActualHeight > 0 && owner.IsVisible)
            {
                try
                {
                    // PointToScreen(0,0) returns physical screen pixel coordinates of top-left client area
                    Point pt = owner.PointToScreen(new Point(0, 0));
                    var source = PresentationSource.FromVisual(owner);
                    double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
                    screenLeft = pt.X / dpiX;
                    screenTop  = pt.Y / dpiY;
                    screenW    = owner.ActualWidth;
                    screenH    = owner.ActualHeight;
                }
                catch
                {
                    if (owner.WindowState == WindowState.Maximized)
                    {
                        screenW    = SystemParameters.WorkArea.Width;
                        screenH    = SystemParameters.WorkArea.Height;
                        screenLeft = SystemParameters.WorkArea.Left;
                        screenTop  = SystemParameters.WorkArea.Top;
                    }
                    else
                    {
                        screenW    = owner.ActualWidth;
                        screenH    = owner.ActualHeight;
                        screenLeft = owner.Left;
                        screenTop  = owner.Top;
                    }
                }
            }
            else
            {
                screenW    = SystemParameters.WorkArea.Width;
                screenH    = SystemParameters.WorkArea.Height;
                screenLeft = SystemParameters.WorkArea.Left;
                screenTop  = SystemParameters.WorkArea.Top;
            }

            if (Math.Abs(dialog.Width - screenW) > 0.5) dialog.Width = screenW;
            if (Math.Abs(dialog.Height - screenH) > 0.5) dialog.Height = screenH;
            if (Math.Abs(dialog.Left - screenLeft) > 0.5) dialog.Left = screenLeft;
            if (Math.Abs(dialog.Top - screenTop) > 0.5) dialog.Top = screenTop;

            if (dialogShell != null)
            {
                // Cap dialog at a comfortable fixed max (1050px) so it stays compact on large displays.
                // On small screens (<1024px) it scales down proportionally but never below 540px.
                double targetW   = Math.Min(Math.Max(screenW * 0.72, 680), 1050);
                double minW      = Math.Min(Math.Max(screenW * 0.55, 540), targetW);
                dialogShell.MaxHeight = Math.Max(screenH * 0.90, 480);
                dialogShell.Width     = targetW;
                dialogShell.MinWidth  = minW;
                dialogShell.MaxWidth  = Math.Min(screenW * 0.95, 1050);
            }

            // When this dialog closes, reactivate its owner so the main window
            // comes back to the foreground instead of minimizing or going behind.
            if (dialog.Owner != null)
            {
                dialog.Closed += (s, e) =>
                {
                    try { dialog.Owner?.Activate(); } catch { }
                };
            }
        }

        /// <summary>
        /// Safely sets the DialogResult on a Window.
        /// Catches InvalidOperationException if the window is not currently running an active modal loop (ShowDialog)
        /// or is already in the closing/closed state, and safely falls back to Close() if still open.
        /// </summary>
        public static void SafeSetDialogResult(Window window, bool? result)
        {
            if (window == null) return;
            try
            {
                window.DialogResult = result;
            }
            catch (InvalidOperationException)
            {
                try
                {
                    if (window.IsLoaded || window.IsVisible)
                    {
                        window.Close();
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeSetDialogResult error: {ex.Message}");
            }
        }
    }
}
