using System;
using Avalonia.Media;
using CoreGraphics;
using UIKit;

namespace Avalonia.iOS
{
    static class Extensions
    {

        public static Size ToAvalonia(this CGSize size) => new Size(size.Width, size.Height);

        public static Point ToAvalonia(this CGPoint point) => new Point(point.X, point.Y);

        static nfloat ColorComponent(byte c) => ((float) c) / 255;

        public static CGSize ToNative(this Size size) => new CGSize(size.Width, size.Height);

        public static CGRect ToNative(this Rect rect) => new CGRect(rect.X, rect.Y, rect.Width, rect.Height);

        public static CGPoint ToNative(this Point point) => new CGPoint(point.X, point.Y);

        public static UIColor ToUiColor(this Color color) => new UIColor(
            ColorComponent(color.R),
            ColorComponent(color.G),
            ColorComponent(color.B),
            ColorComponent(color.A));
    }
}
