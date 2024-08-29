using GalaSoft.MvvmLight.Command;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Grid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public  MainWindow()
        {
              InitializeComponent();
 
 
         } 
    }
    public class DesignGridLandManager 
    {
        public IGridDataSource GridLand { get; } = new DesignGrid();

        public class DesignGrid : IGridDataSource
        {
            public int Height => 5;

            public int Width =>5;

            public bool Valid => true;

            public IReadOnlyList<Vector3> Colors => new[] { Vector3.One, Vector3.Zero };

            public int NumColors => 2;

            public event Action<((int, int), (int, int))> UpdateRange;

            public void Copy(byte[,] colors, int x1, int y1, int x2, int y2)
            {
                for(int i = 0; i<  Width; i++)
                {
                    for(int j=0; j < Height; j++)
                    {
                        colors[i, j] = (byte)((i%2 +  (j+1) % 2)%2);
                    }
                }
             }
        }
    }
}
