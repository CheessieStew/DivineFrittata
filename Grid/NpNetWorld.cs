
using Numpy;
using Numpy.Models;
using Python.Runtime;
using System;
using System.Linq;

namespace Grid
{
    /*
    public class World
    {
        private readonly WorldRules WorldRules;
        private readonly int Width;
        private readonly int Height;
        public const double mutateWeight = 0.8f;
        public const double stayWeight = 1f - mutateWeight;
        public int UpdateSize { get; set; } = 10;
         private readonly NDarray<double> WaveFunction;
        private readonly NDarray<double> ColorNeighbouringChances;
        private readonly NDarray<double> DirectionWeights;
        private readonly NDarray<double> MutateWeightScalar;
        private NDarray  neighbourChances ;
        Slice xIndexing;
        Slice yIndexing;
          
        public World(WorldRules worldRules, int width, int height )
        {
            this.WorldRules = worldRules;
            this.Width = width;
            this.Height = height;
            MutateWeightScalar = np.array(mutateWeight);
            var init = new double[worldRules.NumColors];
            init[0] = 1f;           
             WaveFunction = new NDarray<double>(np.resize(np.array(init), new Shape(Width, Height, worldRules.NumColors)) );

            var colorNeighbouringChances = new double[worldRules.NumColors, worldRules.NumColors];
            for (byte i = 0; i < worldRules.NumColors; i++)
            {
                for (byte j = 0; j < worldRules.NumColors; j++)
                {
                    colorNeighbouringChances[i, j] = worldRules.NeighbourColors[i].Contains(j) ? 1 : 0;
                }
            }
            ColorNeighbouringChances = np.array<double>(colorNeighbouringChances);
            for (int i = 0; i < worldRules.NumColors; i++)
            {            
                 var slice = ColorNeighbouringChances[(object)i, Slice.All()];
                var sum = slice.sum();
                if( sum.asscalar<double>() > 0)
                    ColorNeighbouringChances[(object)i, Slice.All()] =  (slice / slice.sum());
            }
            xIndexing = new Slice();
            yIndexing = new Slice();

            DirectionWeights = new NDarray<double>( np.resize
                (np.array<double>(new[] { 1, 0.70710678118d })  // 1 for hori/veri, 1/sqrt(2) for diago                 
                    * 0.125f,  // 1/8
                 new Shape(8)));
        }

        public ((int, int), (int, int)) UpdateAround(int x0, int y0, int? updateSize = null)
        {
            updateSize ??= Math.Min( UpdateSize, Math.Min(Width/2-1, Height/2-1));
            NDarray s=   (stayWeight * GetSlice(x0, y0, updateSize.Value)) ;
            neighbourChances = np.empty(8, Width, Height, WorldRules.NumColors);
            for (int i = 0; i < 8; i++)
            {
                neighbourChances[i] = np.dot(GetSlice(x0, y0, updateSize.Value, Directions[i]) ,  ColorNeighbouringChances);
            }
            // s  =  (s +  mutateWeight 
            //     + mutateWeight * np.exp(- np.multiply(np.stack(neighbourChances), DirectionWeights).sum()));

             NDarray tmp = np.multiply(DirectionWeights, neighbourChances);
            tmp = tmp.sum(new Axis(0));
            tmp = (-1) * tmp;
            tmp = np.exp(tmp);
            tmp = mutateWeight * tmp;
            s = s.add(tmp);
            s = s.add(MutateWeightScalar);
            SetSlice(x0, 0, updateSize.Value, s); 
            return ((x0 - updateSize.Value, y0 - updateSize.Value), (x0 + updateSize.Value, y0 + updateSize.Value));
        }

        public byte[]   Get(((int x1, int y1), (int x2, int y2)) r)
        {
            return    np.argmax (WaveFunction[new Slice(r.Item1.x1, r.Item2.x2), new Slice(r.Item1.y1, r.Item2.y2), Slice.All()], 2).GetData<byte>();
        }

        [Flags]
        public enum Direction { N = 1, NE = N | E, E = 2, SE = S | E, S = 4, SW = S | W, W = 8, NW = N | W }
        public readonly Direction[] Directions = new[] {
            Direction.N,
            Direction.NE,
            Direction.E,
            Direction.SE,
            Direction.S,
            Direction.SW,
            Direction.W,
            Direction.NW };
         
        static int GetX(Direction d) => d.HasFlag(Direction.N) ? -1 : 1;
        static int GetY(Direction d) => d.HasFlag(Direction.W) ? -1 : 1;
        private NDarray  GetSlice(int x, int y, int updateSize, Direction d) =>
            GetSlice(x + GetX(d), y + GetY(d), updateSize);
        private NDarray  GetSlice(int x0, int y0, int updateSize)
        {
            xIndexing.Start = (x0 - updateSize) % Width;
            xIndexing.Stop = (x0 + updateSize) % Width;
            yIndexing.Start = (y0 - updateSize) % Height;
            yIndexing.Stop = ( y0 + updateSize) % Height;
            if (xIndexing.Start >  xIndexing.Stop)
                xIndexing.Start = -Width + xIndexing.Start % Width;  

            if (yIndexing.Start > yIndexing.Stop)
                yIndexing.Start = -Height  + yIndexing.Start % Height ; 
             
            return WaveFunction[xIndexing, yIndexing, Slice.All()];
        }
        private void SetSlice(int x0, int y0, int updateSize, NDarray  s)
        {
            xIndexing.Start = (x0 - updateSize);
            xIndexing.Stop = (x0 + updateSize);
            yIndexing.Start = (y0 - updateSize);
            yIndexing.Stop = (y0 + updateSize);
            if (xIndexing.Start > xIndexing.Stop)
                xIndexing.Start = -Width + xIndexing.Start % Width;
            else xIndexing.Start %= Width;
            if (yIndexing.Start > yIndexing.Stop)
                yIndexing.Start = -Height + xIndexing.Start % Height;
            else yIndexing.Start %= Height;
            xIndexing.Stop %= Width;
            yIndexing.Stop %= Height;


            WaveFunction[xIndexing, yIndexing, Slice.All()] = s;
        }



    }
*/}