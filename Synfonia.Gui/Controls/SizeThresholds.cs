using System.ComponentModel;
using Avalonia;

namespace Synfonia.Controls
{
    [TypeConverter(typeof(SizeThresholdsTypeConverter))]
    public class SizeThresholds : AvaloniaObject
    {
        public static readonly DirectProperty<SizeThresholds, double> XS_SMProperty =
            AvaloniaProperty.RegisterDirect<SizeThresholds, double>(
                nameof(XS_SM),
                o => o.XS_SM,
                (o, v) => o.XS_SM = v, 768.0);

        public double XS_SM
        {
            get { return GetValue(XS_SMProperty); }
            set { SetValue(XS_SMProperty, value); }
        }

        public static readonly DirectProperty<SizeThresholds, double> SM_MDProperty =
            AvaloniaProperty.RegisterDirect<SizeThresholds, double>(
                nameof(SM_MD),
                o => o.SM_MD,
                (o, v) => o.SM_MD = v, 992.0);

        public double SM_MD
        {
            get { return GetValue(SM_MDProperty); }
            set { SetValue(SM_MDProperty, value); }
        }

        public static readonly DirectProperty<SizeThresholds, double> MD_LGProperty =
            AvaloniaProperty.RegisterDirect<SizeThresholds, double>(
                nameof(MD_LG),
                o => o.MD_LG,
                (o, v) => o.MD_LG = v, 1200.0);

        public double MD_LG
        {
            get { return GetValue(MD_LGProperty); }
            set { SetValue(MD_LGProperty, value); }
        }
    }
}