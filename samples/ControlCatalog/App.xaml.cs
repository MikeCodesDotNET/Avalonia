using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;

namespace ControlCatalog
{
    public class App : Application
    {
        private static readonly StyleInclude DataGridFluent = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        };

        private static readonly StyleInclude DataGridDefault = new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Default.xaml")
        };

        public static Styles FluentDark = new Styles
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/FluentDark.xaml")
            },
            DataGridFluent
        };

        public static Styles FluentLight = new Styles
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/FluentLight.xaml")
            },
            DataGridFluent
        };

        public static Styles DefaultLight = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
            },
            DataGridDefault
        };

        public static Styles DefaultDark = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
            {
                Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
            },
            DataGridDefault
        };

        private void ChangeTheme(PlatformTheme theme)
        {
            switch (theme)
            {
                case PlatformTheme.Dark:
                    Styles.Insert(0, FluentDark);
                    break;
                default:
                    Styles.Insert(0, FluentLight);
                    break;
            }
        }

        public override void Initialize()
        {
            var themeProvider = AvaloniaLocator.Current.GetService<IPlatformThemeProvider>();
            if(themeProvider.SelectedTheme == PlatformTheme.Light)
                Styles.Insert(0, FluentLight);
            else
                Styles.Insert(0, FluentDark);


            themeProvider.ThemeDidChange += (s, e) => ChangeTheme(e.NewTheme);
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                desktopLifetime.MainWindow = new MainWindow();
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
                singleViewLifetime.MainView = new MainView();

            base.OnFrameworkInitializationCompleted();
        }
    }
}
