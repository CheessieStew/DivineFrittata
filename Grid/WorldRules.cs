using System;
using System.Collections.Generic;
using System.Linq;

namespace Grid
{
    public struct  WorldRules
    {
        public float[,] NeighbourColors;
        public int NumColors { get; }
        public WorldRules(int? colors, Random random )
        {
            colors ??= random.Next(5, 12);
            NumColors = colors.Value;  
            NeighbourColors = new float [colors.Value ,colors.Value];
            var allColors = Enumerable.Range(0, colors.Value).Select(c=>(byte)c).ToList ();
            double density = 1;
            List<byte> curNbrs = new List<byte>();
            for (byte color1 = 0; color1< colors; color1++)
            {
                curNbrs.Clear();
                int nbc = 1;
                if (NumColors * 2 - (int)density > 1 && color1 + 1 < NumColors)
                {

                    nbc = random.Next(0, NumColors * 2 - (int)density);
                    nbc += (int)(2 / density);
                }

                if(nbc>0)
                    density += Math.Log(1+nbc);
                if(color1+1<NumColors)
                for(int nb = 0; nb < nbc; nb++)
                {
                    curNbrs.Add((byte)random.Next(color1+1, NumColors));
                }
                 


                double selfdensity = 2f * (density+ Math.Log(1+nbc));
                int self =
                    (NumColors * 2 - (int)selfdensity > 0) ? random.Next(1, NumColors * 2 - (int)selfdensity) : 1;
                foreach (var nb in curNbrs.GroupBy(x => x).OrderBy(x=>x.Key))
                {
                    int count = nb.Count();
               
                     
                    {
                    NeighbourColors[color1, nb.Key] = count;
                        NeighbourColors[ nb.Key, color1] = count;
                    }
                }
                NeighbourColors[color1, color1] = self; 
            } 
            
        }

        public WorldRules(System.Data.DataTable neighbourMatrixTable) : this()
        {
            NumColors = neighbourMatrixTable.Columns.Count;
            NeighbourColors = new float[NumColors, NumColors];
            for (int i = 0; i < neighbourMatrixTable.Rows.Count; i++)
            {
                for (int j = i; j < neighbourMatrixTable.Columns .Count; j++)
                {
                    NeighbourColors[i, j] = NeighbourColors[j, i] = (float)neighbourMatrixTable.Rows[i][j];
                        
                }
            }
        }

        internal static WorldRules GetRandom(Random random, int v1, int v2)
        {
            return new WorldRules(random.Next(v1, v2), random);
        }
    }
}
