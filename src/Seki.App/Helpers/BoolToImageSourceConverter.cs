using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Helpers;

public class BoolToImageSourceConverter : IValueConverter
{
    public ImageSource? TrueImage { get; set; }
    public ImageSource? FalseImage { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is true)
        {
            return TrueImage;
        }

        return FalseImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
