using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;
using UIKit;

namespace Avalonia.iOS
{
    public class PlatformThemeProvider : IPlatformThemeProvider
    {
        public PlatformTheme SelectedTheme
        {
            get
            {
                if (!Helpers.IsiOS13OrNewer)
                    return PlatformTheme.Unspecified;

                var uiStyle = Helpers.GetCurrentViewController()?.TraitCollection?.UserInterfaceStyle ??
                        UITraitCollection.CurrentTraitCollection.UserInterfaceStyle;

                switch (uiStyle)
                {
                    case UIUserInterfaceStyle.Light:
                        return PlatformTheme.Light;
                    case UIUserInterfaceStyle.Dark:
                        return PlatformTheme.Dark;
                    default:
                        return PlatformTheme.Unspecified;
                };
            }
        }

        public event EventHandler<PlatformThemeChangedEventArgs> ThemeDidChange;

        internal void SetTheme(PlatformTheme newtheme)
        {
            OnThemeDidChange(new PlatformThemeChangedEventArgs(SelectedTheme, newtheme));
        }

        void OnThemeDidChange(PlatformThemeChangedEventArgs e)
        {
            ThemeDidChange?.Invoke(this, e);
        }
    }
}
