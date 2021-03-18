using System;

namespace Avalonia.Platform
{
    public interface IPlatformThemeProvider
    {
        /// <summary>
        /// Get the current theme of host platform. 
        /// </summary>
        PlatformTheme SelectedTheme { get; }

        event EventHandler<PlatformThemeChangedEventArgs> ThemeDidChange;
    }

    public class PlatformThemeChangedEventArgs : EventArgs
    {
        public PlatformTheme OldTheme { get; }

        public PlatformTheme NewTheme { get; }

        public PlatformThemeChangedEventArgs(PlatformTheme oldTheme, PlatformTheme newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }
}
