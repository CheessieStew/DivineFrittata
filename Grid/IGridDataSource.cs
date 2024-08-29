using System;
using System.Collections.Generic;
using System.Numerics;

namespace Grid
{
    public interface IGridDataSource
    {
        int Height { get; }
        int Width { get; }
         event Action<((int, int),(int,int))> UpdateRange;
        bool Valid { get; }
        IReadOnlyList<Vector3> Colors { get; }
        int NumColors { get; }

        void Copy(byte[,] colors,  int x1, int y1 ,int x2, int y2   );
    }
}
