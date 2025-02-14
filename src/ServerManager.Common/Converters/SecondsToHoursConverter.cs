﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace ServerManagerTool.Common.Converters
{
    public class SecondsToHoursConverter : IValueConverter
    {
        public const int MIN_VALUE = 0;
        public const int MAX_VALUE = int.MaxValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scaledValue = System.Convert.ToInt32(value);

            var sliderValue = (int)TimeSpan.FromSeconds(scaledValue).TotalHours;
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            sliderValue = Math.Min(MAX_VALUE, sliderValue);
            return sliderValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sliderValue = System.Convert.ToInt32(value);
            sliderValue = Math.Max(MIN_VALUE, sliderValue);
            sliderValue = Math.Min(MAX_VALUE, sliderValue);

            var scaledValue = (int)TimeSpan.FromHours(sliderValue).TotalSeconds;
            return scaledValue;
        }
    }
}
