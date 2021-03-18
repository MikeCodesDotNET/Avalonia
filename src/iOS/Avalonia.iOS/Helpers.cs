using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

namespace Avalonia.iOS
{
    internal static class Helpers
    {
        static bool? _isiOS9OrNewer;
        static bool? _isiOS10OrNewer;
        static bool? _isiOS11OrNewer;
        static bool? _isiOS12OrNewer;
        static bool? _isiOS13OrNewer;
        static bool? _isiOS14OrNewer;
        static bool? _respondsTosetNeedsUpdateOfHomeIndicatorAutoHidden;



        internal static bool IsiOS9OrNewer
        {
            get
            {
                if (!_isiOS9OrNewer.HasValue)
                    _isiOS9OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(9, 0);
                return _isiOS9OrNewer.Value;
            }
        }

        internal static bool IsiOS10OrNewer
        {
            get
            {
                if (!_isiOS10OrNewer.HasValue)
                    _isiOS10OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(10, 0);
                return _isiOS10OrNewer.Value;
            }
        }

        internal static bool IsiOS11OrNewer
        {
            get
            {
                if (!_isiOS11OrNewer.HasValue)
                    _isiOS11OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(11, 0);
                return _isiOS11OrNewer.Value;
            }
        }

        internal static bool IsiOS12OrNewer
        {
            get
            {
                if (!_isiOS12OrNewer.HasValue)
                    _isiOS12OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(12, 0);
                return _isiOS12OrNewer.Value;
            }
        }

        internal static bool IsiOS13OrNewer
        {
            get
            {
                if (!_isiOS13OrNewer.HasValue)
                    _isiOS13OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
                return _isiOS13OrNewer.Value;
            }
        }

        internal static bool IsiOS14OrNewer
        {
            get
            {
                if (!_isiOS14OrNewer.HasValue)
                    _isiOS14OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(14, 0);
                return _isiOS14OrNewer.Value;
            }
        }

        internal static bool RespondsToSetNeedsUpdateOfHomeIndicatorAutoHidden
        {
            get
            {
                if (!_respondsTosetNeedsUpdateOfHomeIndicatorAutoHidden.HasValue)
                    _respondsTosetNeedsUpdateOfHomeIndicatorAutoHidden = new UIViewController().RespondsToSelector(new ObjCRuntime.Selector("setNeedsUpdateOfHomeIndicatorAutoHidden"));
                return _respondsTosetNeedsUpdateOfHomeIndicatorAutoHidden.Value;
            }
        }

        internal static UIViewController GetCurrentViewController(bool throwIfNull = false)
        {
            UIViewController viewController = null;

            var window = UIApplication.SharedApplication.KeyWindow;
            if (window != null && window.WindowLevel == UIWindowLevel.Normal)
                viewController = window.RootViewController;

            if (viewController == null)
            {
                window = UIApplication.SharedApplication
                    .Windows
                    .OrderByDescending(w => w.WindowLevel)
                    .FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);

                if (window == null && throwIfNull)
                    throw new InvalidOperationException("Could not find current view controller.");
                else
                    viewController = window?.RootViewController;
            }

            while (viewController?.PresentedViewController != null)
                viewController = viewController.PresentedViewController;

            if (throwIfNull && viewController == null)
                throw new InvalidOperationException("Could not find current view controller.");

            return viewController;
        }
    }
}
