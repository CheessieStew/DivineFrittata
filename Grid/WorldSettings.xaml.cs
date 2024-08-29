using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace Grid
{
    /// <summary>
    /// Interaction logic for WorldSettings.xaml
    /// </summary>
    public partial class WorldSettings : UserControl, INotifyPropertyChanged
    {
        [Bindable(true)]
        public IReadOnlyList<Vector3> Colors
        {
            get => (IReadOnlyList<Vector3>)GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }
        public static readonly DependencyProperty ColorsProperty =
        DependencyProperty.Register(
         name: nameof(Colors),
         propertyType: typeof(IReadOnlyList<Vector3>),
         ownerType: typeof(WorldSettings),
         typeMetadata: new FrameworkPropertyMetadata(new PropertyChangedCallback(OnColorsChanged))
       );
        public IReadOnlyList<Brush> Brushes => _brushes;
        public Brush Brushhh { get; private set; }
 
        private List<Brush> _brushes;

        public event PropertyChangedEventHandler PropertyChanged;

        public WorldSettings()
        {
             InitializeComponent();
        }
         
        private static void OnColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WorldSettings s)
            {          
                if (e.NewValue is IReadOnlyList<Vector3> newSource)
                {                    
                    s. _brushes = new List<Brush >();
                    for (int i = 0; i< newSource.Count; i++)
                    {
                        var c = newSource[i];
                        SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(255 * c.X), (byte)(255 * c.Y), (byte)(255 * c.Z)));
  
                        
                        brush.Freeze();
                        s._brushes.Add(brush );
                    }
                    s.Brushhh = s._brushes.FirstOrDefault();
                    s.PropertyChanged?.Invoke(s, new PropertyChangedEventArgs(nameof(s.Brushhh)));
                    s.PropertyChanged?.Invoke(s, new PropertyChangedEventArgs(nameof(s._brushes)));
                }            
            
            }

        }


       
    }
    public class RowNrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FrameworkElement e)
             return    DataGridRow.GetRowContainingElement(e)?.GetIndex() ?? DependencyProperty.UnsetValue;
            return   DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ColorArrayAccessConverter : ArrayAccessConverter
    {
        public override  object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var res = base.Convert(values, targetType, parameter, culture);
            if (res is Vector3 c)
                return Color.FromRgb((byte)(255 * c.X), (byte)(255 * c.Y), (byte)(255 * c.Z));
            return DependencyProperty.UnsetValue;
        }
         
       
    }
    public class ArrayAccessConverter : IMultiValueConverter
    { 
        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                  if (values[1] is IList  os)
                {
                    return values[0] switch
                    {
                        DataGridCell dgc 
                            => GetForCell(os, dgc),
                        DataGridColumnHeader ch when int.TryParse(ch.Content?.ToString(), out var idx) 
                            => Get(os,idx),
                        DataGridRowHeader rh  when DataGridRow.GetRowContainingElement(rh).GetIndex()  is int idx
                            => Get(os, idx),
                        _ => DependencyProperty.UnsetValue
                    };

                }
                else
                {

                }
            }
            catch { }
            return DependencyProperty.UnsetValue;
         }

        private static   object Get(IList os, int d)
          => os.Count > 0 ? os[Math.Clamp(d, 0, os.Count - 1)] : null; 

        private static object GetForCell(IList brushes, DataGridCell dgc)
        {

            // System.Data.DataRowView rowView = (System.Data.DataRowView)dgc.DataContext;
            var c = dgc.Column.DisplayIndex;
            int r = DataGridRow.GetRowContainingElement(dgc).GetIndex();

            return r >= c ? Get( brushes, c) : Brushes.Black;
        }

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PowerScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!double.TryParse(parameter?.ToString(), out var p)) 
                p = 2;
              return Math.Clamp( Math.Pow ((float )value, 1d/ p), 0, 1);
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!double.TryParse(parameter?.ToString(), out var p))
                p = 2;
            return Math.Clamp(Math.Pow((double )value, p), 0, 1);
             
        }
    }
}
