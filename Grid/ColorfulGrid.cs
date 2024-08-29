using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Grid
{
    public class ColorfulGrid : FrameworkElement
    { 
      
        Rect[,] Rects;
        double renderWidth, renderHeight;
        int gridWidth, gridHeight;
        DateTime lastRender = DateTime.Now;
        private bool updatePending;
        const int MinMsPerFrame = 50;
        List<Pen> Pens;
        private byte[,] Colors;
        List<SolidColorBrush> Brushes;  
         DrawingGroup backingStore;
        [Bindable(true)]
        public IGridDataSource Source
        {
            get => (IGridDataSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        private IGridDataSource _src;
        public static readonly DependencyProperty SourceProperty =
     DependencyProperty.Register(
      name: nameof(Source),
      propertyType: typeof(IGridDataSource),
      ownerType: typeof(ColorfulGrid),
      typeMetadata: new FrameworkPropertyMetadata( 
         new PropertyChangedCallback(OnSourceChanged))
    );

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is ColorfulGrid grid)
            {
                 if (e.OldValue is IGridDataSource oldSource) 
                {
                    oldSource.UpdateRange -= grid._source_UpdateRange;
                }
                if (e.NewValue  is IGridDataSource newSource)
 
                {
                    newSource.UpdateRange += grid._source_UpdateRange;
                  grid.  _src = newSource;
                }
                grid.Init();
            } 
        }

        public Rect FullRect1 { get; private set; }
        public Rect FullRect2 { get; private set; }
         private void _source_UpdateRange(((int x1, int y1), (int x2, int y2)) r)
        {
            _src.Copy(Colors, r.Item1.x1, r.Item1.y1, r.Item2.x2, r.Item2.y2);
            updatePending = true;
         }
 

        public ColorfulGrid()
        {
            // this helps make thin lines a little clearer
            SnapsToDevicePixels = true;
           
             backingStore = new DrawingGroup();
            Render();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void Init( )
        {
            if (_src?.Valid != true) 
                return;
            Colors = new byte[_src.Width, _src.Height];
            Brushes = new List<SolidColorBrush>();
            Pens = new List<Pen>();
            for (int i = 0; i <Source. Colors.Count; i++)
            {
                 var c = Source.Colors[i];
                SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)(255* c.X), (byte)(255 * c.Y), (byte)(255 * c.Z)));
                var paleC = brush.Color;
                paleC *= 0.8f;
                paleC.A = 255;
                 SolidColorBrush brush2 = new SolidColorBrush(paleC);
              Pen pen = new Pen(brush, 1.0f);
                pen.Freeze();
                Pens.Add(pen); 

                  brush.Freeze();
                Brushes.Add(brush2);

            }
            foreach (var pen in Pens)
            {
                pen.Freeze();
            }
            updatePending = true;
        }

        private void AudioLevelsUIElement_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (RenderSize.Width != 0)
            {

                lock (_renderLock)                 
                    InitRects();
                updatePending = true;
            }
        } 

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            this.Render();
        }
         
        protected override void OnRender(DrawingContext drawingContext)
        {
            Render();
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(backingStore);
        }
         
        private void Render()
        {
            if (_src?.Valid != true)
                return;
            if ((DateTime.Now - lastRender).TotalMilliseconds > MinMsPerFrame)
            {

                lock (_renderLock)
                {
                    InitRects();
                    if (!updatePending)
                        return;
                    var drawingContext = backingStore.Open();
                    Render(drawingContext);
                    drawingContext.Close();
                    updatePending = false;
                }
            }
        }

        private void InitRects()
        {
            if (Rects != null
                && this.RenderSize.Width == renderWidth && this.RenderSize.Height == renderHeight
                && gridHeight == Source.Height && gridWidth == Source.Height)
                return;
            if (_src?.Valid!=true)
                return;
            renderWidth = this.RenderSize.Width;
            renderHeight = this.RenderSize.Height;
             var squareWidth = Math.Min(renderHeight, renderWidth);
            Rects = new Rect[Source.Width, Source.Height];
            double x, y;
            double xS = squareWidth / (gridWidth = Source.Width);
            double yS = squareWidth / (gridHeight = Source.Height);
            double xDiff, yDiff;  
            if(renderWidth > renderHeight)
            {
                xDiff = (renderWidth - renderHeight) / 2;
                yDiff = 0;
                FullRect1 = new Rect(0, 0, xDiff, renderHeight);
                FullRect2 = new Rect(renderWidth - xDiff, 0 , xDiff, renderHeight);
            }
            else
            {
                xDiff = 0;
                yDiff= (renderHeight - renderWidth) / 2 ;

                FullRect1 = new Rect(0, 0, renderWidth, yDiff);
                FullRect2 = new Rect(0, renderHeight - yDiff, renderWidth, yDiff);
            }
         for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    x = xS * i + xDiff;
                    y = yS * j + yDiff;  

                    Rects[i, j] = new Rect(x, y, x + xS, y + yS);
                }
            }
                updatePending = true;
        }
        private object _renderLock = new object();
        private void Render(DrawingContext drawingContext)
        {

             
                for (int i = 0; i < gridWidth; i++)
                {
                    for (int j = 0; j < gridHeight; j++)
                    {
                        byte  r = Colors[i,j];
                        drawingContext.DrawRectangle(                       
                        Brushes[r], Pens[r],
                        Rects[i,j]);
                    }
                }
                drawingContext.DrawRectangle(Brushes[0], Pens[0], FullRect1);
                drawingContext.DrawRectangle(Brushes[0], Pens[0], FullRect2);

                lastRender = DateTime.Now;
            }
         
    }
}
