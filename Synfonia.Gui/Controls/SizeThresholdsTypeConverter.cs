using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Synfonia.Controls
{
    public class SizeThresholdsTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType is string;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var text = (string)value;
            var list = text.Split(',')
                           .Select(o => o.Trim())
                           .Select(o => int.Parse(o))
                           .ToList();

            if (list.Count() != 3)
            {
                throw new ArgumentException($"'{value}' Invalid value. BreakPoints must contains 3 items.");
            }

            return new SizeThresholds() { XS_SM = list[0], SM_MD = list[1], MD_LG = list[2] };
        }
    }
}