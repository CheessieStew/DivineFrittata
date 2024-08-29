using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Grid
{
    public class GridLand : IGridDataSource, IDisposable
    {
        Random _random = new();

        private byte[,] _grid;
        public int Height => WorldParams.Height;
        public int Width => WorldParams.Width;
        public bool Valid { get; private set; }
        public Task Alive { get; private set;  }
        private CancellationTokenSource _cancellation;
        public IReadOnlyList<Vector3> Colors => _colors;
        private Vector3[] _colors;
        public WorldParams WorldParams { get; private set; }
        public NumSharpWorld World { get; private set; }
        public int NumColors => WorldParams.Rules.NumColors;

         public event Action<((int, int), (int, int))> UpdateRange;

        public GridLand(WorldParams? wp = null )
        {
            Reset( WorldParams = wp?? new WorldParams(WorldRules.GetRandom(_random, 5, 12), 0.12f, 50,50, true, NumSharpWorld.MutatorsCount, _random.Next(0, NumColors) , 10, 5));
         }
        public void  Reset( )
        {
              Reset(WorldParams);
        }

         public WorldParams SetParams(WorldParams pars)
        {
            if (pars.Width == Width && pars.Height == Height)
            {
                WorldParams= World.ParamsUpdate(pars);
            }
            else
            {
                Reset(pars);          
            }
            return WorldParams;
        }
    

        private async void Reset(WorldParams pars)
        {
            Valid = false;
            _cancellation?.Cancel();
            WorldParams = pars;

             World = new NumSharpWorld             (WorldParams);
            _colors = GenerateColors(WorldParams.Rules.NumColors);
            _grid = new byte[Width, Height];
            Valid = true;
            if(Alive!=null)
                await Alive;
            Alive = Task.Run(Live, (_cancellation = new CancellationTokenSource()).Token);
        }

        private async Task Live()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                int x = _random.Next(0, Width) + Width,
                    y = _random.Next(0, Height) + Height;
                UpdateGrid(World.UpdateAround(x, y, _random.Next(0, 8)));
               await Task.Delay((int)(1000/ WorldParams.Speed));
            }
        } 

        public void Copy(byte[,] target,   int x1, int y1 ,  int x2, int y2 )
        {
            World.CopyTo(x1,y1,x2,y2, target); 
        }
         
        private Vector3[] GenerateColors(int colors)
        {
            var res = new Vector3[colors];
            for (int i = 0; i < colors; i++)
            {
                res[i] = RandomColor(_random);
            }
            for (int t = 0; t < colors; t++)
            {
                var v = _random.Next(0, colors);
                for (int tt = 0; tt < colors/3; tt++)
                {

                    var nb = _random.Next(0, colors);

              
                    if (_random.Next() % 3 > 1)
                        (res[v], res[nb]) = ((res[v] * 5 + res[nb] * 2) / 7, (res[v] * 2 + res[nb] * 5) / 7);
                }
                 
            }
            return res;  
             static Vector3 RandomColor(Random random)
            {
                const float  r = 0.5f;
                var u = random.NextDouble();
                var v = random.NextDouble();
                var theta = 2 * Math.PI * u;
                var phi = Math.Acos(2 * v - 1);
                return new Vector3((float)( r + (r * Math.Sin(phi) * Math.Cos(theta))),
                   (float)(r + (r * Math.Sin(phi) * Math.Sin(theta))),
                   (float)(r + (r * Math.Cos(phi))));
            }
        }
        private void UpdateGrid(((int x1, int y1), (int x2, int y2)) r)
        {

            UpdateRange?.Invoke(r);

        }

        public void Dispose()
        {
            _cancellation.Cancel();
            Alive.Dispose();
        }
    }
}
