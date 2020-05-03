using System;
using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;
using System.Linq;
using Avalonia.Media;

namespace Symphony.Converters
{
    public class DefaultBGImageConverter : IValueConverter
    {
        static Color[] XMB_Colors = new Color[]
        {
            Color.Parse("#CBCBCB"),
            Color.Parse("#D8BF1A"),
            Color.Parse("#6DB217"),
            Color.Parse("#E17E9A"),
            Color.Parse("#178816"),
            Color.Parse("#9A61C8"),
            Color.Parse("#02CDC7"),
            Color.Parse("#0C76C0"),
            Color.Parse("#B444C0"),
            Color.Parse("#E5A708"),
            Color.Parse("#875B1E"),
            Color.Parse("#E3412A")
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                uint selector = s.Select(x => (uint)x)
                                 .Aggregate((x, y) => x ^ y) % 12;

                return new LinearGradientBrush()
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops()
                    {
                        new GradientStop(Colors.DarkGray, 0),
                        new GradientStop(XMB_Colors[selector], 1),
                    }
                };
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
