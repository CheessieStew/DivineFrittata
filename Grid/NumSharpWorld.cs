using Microsoft.Toolkit.HighPerformance;
using NumSharp;
using NumSharp.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Grid
{
   public class NumSharpWorld
   {
        private WorldParams _worldParams;
             public const int MutatorsCount = 6;

        public int UpdateSize { get; set; } = 10;
        protected  readonly List<IMutator> Mutators; 
        private IMutator _mutator;
        private int _mutatorIdx;
        public   IEnumerable<string> MutatorsNames => Mutators.Select(m=>m.GetType().Name);

        private readonly NDArray<float> WaveFunction;
        private readonly NDArray ColorNeighbouringChances;
        private readonly NDArray  DirectionWeights;
        private float[] DirWghts = new[] { 1f, 0.70710678118f };
           
        private object lockObj = new object();
       public interface IMutator
        {
            float StayWeight { get; set; }
            float MutateWeight { get; set; }

            NDArray mutate (NDArray updateArea, NDArray neighbourChances);
        }

        private abstract class MutatorBase : IMutator 
        {
            private float _mutate;
            private float _stay;
            public float MutateWeight
            {
                get => _mutate;
                set
                {
                    if (_mutate != value)
                    {
                        _stay = 1 - value;
                        _mutate = value;
                    }
                }
            }
            public float StayWeight
            {
                get => _stay;
                set
                {
                    if (_stay != value)
                    {
                        _mutate = 1 - value;
                    _stay = value;
                    }
            }
        }
            public MutatorBase(float mutateWeight)
            {
                MutateWeight = mutateWeight;
            }

            public abstract NDArray mutate(NDArray updateArea, NDArray neighbourChances);
        }

        public WorldParams ParamsUpdate(WorldParams pars)
        {
         return    _worldParams = _worldParams.Update(pars);        }

        public NumSharpWorld(WorldParams worldParams  )
        {
            this._worldParams = worldParams;
           
             var init = new float[_worldParams.Rules.NumColors];
            init[0] = 0.4f;

            WaveFunction = new NDArray<float>(new Shape(_worldParams.Width, _worldParams.Height, _worldParams.Rules.NumColors));
            WaveFunction .SetData((NDArray)WaveFunction + new NDArray(init)+np.random.normal(0.4, 0.10, WaveFunction.shape));    
            ColorNeighbouringChances =   np.array<float>((_worldParams.Rules.NeighbourColors));
            ColorNeighbouringChances.SetData(ColorNeighbouringChances*  0.0001f * np.eye(ColorNeighbouringChances.shape[0]));

            for (int i = 0; i < _worldParams.Rules.NumColors; i++)
            { 
                var row = ColorNeighbouringChances[new Slice(i, _worldParams.Rules.NumColors), (object)i];
                var sum = row.sum();
                if (sum.GetData<float>()[0] > 0)
                {
                    row.SetData(row / sum);
                    if (i + 1 < _worldParams.Rules.NumColors)
                    { 
                        var col = ColorNeighbouringChances[(object)i, new Slice(i + 1, _worldParams.Rules.NumColors)];
                        col.SetData( col / sum ); 
                    }
                }
            }
            DirectionWeights = new NDArray<float>(new[] { 1f, 0.70710678118f, 1f, 0.70710678118f, 1f, 0.70710678118f, 1f, 0.70710678118f });

            Mutators  = new List<IMutator>()
            {
                new mutate34(_worldParams.MutateWeight ),
                new mutate3 (_worldParams.MutateWeight),
                new mutate5 (_worldParams.MutateWeight),
                new mutate4 (_worldParams.MutateWeight),
                new mutate2 (_worldParams.MutateWeight),
                new mutate1 (_worldParams.MutateWeight)
            };
            _mutator = Mutators[_mutatorIdx = _worldParams.FunctionIdx];
        }
        
        NDArray[] nbrs = new NDArray[8] ;
        public ((int, int), (int, int)) Szturch(int x, int y, byte color)
        {
            lock (lockObj)
            { 
                x %= _worldParams.Width;
                y %= _worldParams.Height;
                  WaveFunction[new Slice(x, x + 1), new Slice(y, y + 1), Slice.All].SetData(0.5f);
                WaveFunction[new Slice(x, x + 1), new Slice(y, y + 1), color].SetData(1f);

                return UpdateAround(x, y, 2);
            }
        }

        public ((int, int), (int, int)) UpdateAround(int x0, int y0, int? updateSize )
        {
            lock (lockObj)
            {
                LoadParams();
                x0 +=  _worldParams.Width;
                y0 += _worldParams.Height;
                updateSize ??= Math.Min(UpdateSize, Math.Min(_worldParams.Width / 2 - 1, _worldParams.Height / 2 - 1));
                Slice xIndexing1base, yIndexing1base, xIndexing2base, yIndexing2base,
                    xIndexing1, yIndexing1, xIndexing2, yIndexing2,
                    xIndexing1target, yIndexing1target, xIndexing2target, yIndexing2target;
                GetSlicings(x0, y0, updateSize.Value, out xIndexing1base, out yIndexing1base, out xIndexing2base, out yIndexing2base);
                GetTargetSlicings(xIndexing1base, yIndexing1base, xIndexing2base, yIndexing2base,
                    out xIndexing1target, out yIndexing1target, out xIndexing2target, out yIndexing2target);

                NDArray updateArea = (GetSlice(xIndexing1base, yIndexing1base, xIndexing2base, yIndexing2base));
                var zeros = np.zeros(updateArea.shape);

                var eeh = new NDArray[updateArea.shape[0]];
                for (int i = 0; i < 8; i++)
                {
                    GetSlicings(x0, y0, updateSize.Value, Directions[i], out xIndexing1, out yIndexing1, out xIndexing2, out yIndexing2);
                    var slice = GetSlice(xIndexing1, yIndexing1, xIndexing2, yIndexing2);
                    if (slice is not null)
                    {


                        for (int j = 0; j < updateArea.shape[0]; j++)
                        {
                            eeh[j] = (DirWghts[i % 2] * (slice[j].dot(ColorNeighbouringChances)));
                        }
                        // why can't I just do nbrs[i]  = slice.dot(ColorNeighbouringChances) * DirectionWeights?
                        // it returns zeros...
                        // doing it per-row it works as expected.
                        nbrs[i] = np.stack(eeh);

                    }
                    else nbrs[i] = (zeros);
                }

                 updateArea = normalize(_mutator.mutate(updateArea, np.stack(nbrs)), _worldParams.Normalize );

                SetSlice(xIndexing1base, yIndexing1base, xIndexing2base, yIndexing2base, updateArea,
                     xIndexing1target, yIndexing1target, xIndexing2target, yIndexing2target);
            }
            return ((x0 - updateSize.Value, y0 - updateSize.Value), (x0 + updateSize.Value+1, y0 + updateSize.Value + 1));
        }

        private void LoadParams()
        {
            if (_worldParams.FunctionIdx != _mutatorIdx)
            {
                _mutator = Mutators[_mutatorIdx = _worldParams.FunctionIdx];
            }

            _mutator.MutateWeight = _worldParams.MutateWeight;
        }

        private static NDArray normalize(NDArray nDArray, bool normalize)
        {
            if (!normalize)
                return nDArray;
 
            return nDArray/ np.power(np.power(nDArray, 2f).astype(NPTypeCode.Single).sum(0), 0.5f);
            
        }
        private class mutate34 : MutatorBase 
        {
            public mutate34(float mutateWeight) : base(mutateWeight)
            {
            }

            public override NDArray mutate (NDArray updateArea, NDArray neighbourChances)
            {
                //  NDArray mutate = np.power(neighbourChances.sum(0), 1f / stayWeight) * np.power(updateArea, 1f / mutateWeight);
                NDArray mutateWeights = np.abs(np.random.normal(0, 1, updateArea.shape)) * MutateWeight;
                NDArray stayWeights = 1f - mutateWeights;
                NDArray mutate = (np.power( neighbourChances.prod(0), 0.125f) - updateArea * stayWeights) ;
                // NDArray baze = np.min(mutate, 2, true);
                //  mutate = mutate - baze;
                //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
                NDArray sigmoidMutate = np.power(1 + np.exp((-1) * mutate), -1);
                //updateArea = stayWeight * updateArea + mutateWeight * sigmoidMutate;
                return stayWeights * updateArea + mutateWeights * sigmoidMutate;
            }
        }
        private class mutate3 : MutatorBase 
        {
            public mutate3(float mutateWeight) : base(mutateWeight)
            {
            }

            public override NDArray mutate (NDArray updateArea, NDArray neighbourChances)
            {
                //  NDArray mutate = np.power(neighbourChances.sum(0), 1f / stayWeight) * np.power(updateArea, 1f / mutateWeight);
                NDArray mutateWeights = np.abs(np.random.normal(0, 1, updateArea.shape)) * MutateWeight;
                NDArray stayWeights = 1f - mutateWeights;
                NDArray mutate = (neighbourChances.sum(0) - updateArea *stayWeights) * 0.125f;
                // NDArray baze = np.min(mutate, 2, true);
                //  mutate = mutate - baze;
                //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
                NDArray sigmoidMutate = np.power(1 + np.exp((-1) * mutate), -1);
                //updateArea = stayWeight * updateArea + mutateWeight * sigmoidMutate;
                return stayWeights * updateArea + mutateWeights * sigmoidMutate;
            }
        }
        private class mutate5 : MutatorBase 
        {
            public mutate5(float mutateWeight) : base(mutateWeight)
            {
            }

            public override NDArray mutate(NDArray updateArea, NDArray neighbourChances)
            {
                //  NDArray mutate = np.power(neighbourChances.sum(0), 1f / stayWeight) * np.power(updateArea, 1f / mutateWeight);
                NDArray mutateWeights = np.abs( np.random.normal(0, 1, updateArea.shape)) * MutateWeight;
                NDArray stayWeights = 1f - mutateWeights;
                 NDArray mutate = (np.exp(neighbourChances.prod(0) / mutateWeights - updateArea / (stayWeights / MutateWeight)) * 0.125f);

                // NDArray baze = np.min(mutate, 2, true);
                //  mutate = mutate - baze;
                //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
                NDArray sigmoidMutate = np.power(1 + np.exp((-1) * mutate), -1);
                //updateArea = stayWeight * updateArea + mutateWeight * sigmoidMutate;
                return stayWeights * updateArea + mutateWeights * sigmoidMutate;
            }

        }
        private class mutate4 : MutatorBase 
        {
            public mutate4(float mutateWeight) : base(mutateWeight)
            {
            }

            public override NDArray mutate(NDArray updateArea, NDArray neighbourChances)
        {
            //  NDArray mutate = np.power(neighbourChances.sum(0), 1f / stayWeight) * np.power(updateArea, 1f / mutateWeight);
            NDArray mutateWeights = np.abs(np.random.normal(0, 1, updateArea.shape)) * MutateWeight;
            NDArray stayWeights = 1f - mutateWeights;
              NDArray mutate =  np.exp(neighbourChances.sum(0) / mutateWeights )- MutateWeight * updateArea /  stayWeights    ;

            // NDArray baze = np.min(mutate, 2, true);
            //  mutate = mutate - baze;
            //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
            NDArray sigmoidMutate = np.power(1 + np.exp((-1) * mutate), -1);
            //updateArea = stayWeight * updateArea + mutateWeight * sigmoidMutate;
            return stayWeights * updateArea + mutateWeights * sigmoidMutate;
        }
        }
        private class mutate2 : MutatorBase  
        {
            public mutate2(float mutateWeight) : base(mutateWeight)
            {
            }

            public override NDArray mutate(NDArray updateArea, NDArray neighbourChances)
        {
          //  NDArray mutate = np.power(neighbourChances.sum(0), 1f / stayWeight) * np.power(updateArea, 1f / mutateWeight);

            NDArray mutate =   (neighbourChances.sum(0) - updateArea  )* 0.125f;
           // NDArray baze = np.min(mutate, 2, true);
          //  mutate = mutate - baze;
            //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
            NDArray sigmoidMutate = np.power(1 + np.exp((-1) * mutate), -1);
            //updateArea = stayWeight * updateArea + mutateWeight * sigmoidMutate;
            return StayWeight * updateArea + MutateWeight * sigmoidMutate;
        }
        }
        private class mutate1 : MutatorBase
        {
            public mutate1(float mutateWeight) : base(mutateWeight)
            {
            }

            public override  NDArray mutate(NDArray updateArea, NDArray neighbourChances)
            {
                var lol = np.array<float>(np.array<float>(np.sum(0)).sum(0));
                // lol = lol / lol.sum(0);
                NDArray mutate = (neighbourChances.sum(0) - updateArea) * np.exp((-1) * lol) /  StayWeight;
                //   NDArray baze = np.min(mutate, 2, true);
                //    mutate = mutate - baze;
                //         tmp = tmp / np.exp(updateArea*mutateWeight) ;
                var sigmoidMutateInv = 1 + np.exp((-1) * mutate);
                return  StayWeight * updateArea +  MutateWeight / sigmoidMutateInv;
            }
        }
        public void CopyTo(  int x1, int y1 ,  int x2, int y2 , byte[,] target)
        {
             GetSlicings(x1, y1, x2+1, y2+1, out var xs1, out var ys1, out var xs2, out var ys2); 
            var argmax = np.argmax(WaveFunction, 2); 
            GetSlices(argmax, xs1, ys1, xs2, ys2, out var s1, out var s2, out var s3, out var s4);
            var ttarget = np.array(target, copy: false);
            if(s1 is not null)
                ttarget[xs1 ,ys1].SetData(s1) ;
            if (s2 is not null)
                ttarget[xs2, ys1].SetData(s2);
            if (s3 is not null)
                ttarget[xs1, ys2].SetData(s3);
            if (s4 is not null)
                ttarget[xs2, ys2].SetData(s4); 
             
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
       
        private NDArray  GetSlice(  Slice xIndexing1, Slice yIndexing1, Slice xIndexing2, Slice yIndexing2)
        {
            NDArray  s11, s21, s12, s22;
            GetSlices(WaveFunction, xIndexing1, yIndexing1, xIndexing2, yIndexing2, out s11, out s21, out s12, out s22);

            NDArray hst1 = s11;
            if (s21 is not null)
                if (s11 is not null)
                    hst1 = np.vstack(s11, s21);
            if (hst1 is null)
                hst1 = s21;
            NDArray hst2 = s12;
            if (s22 is not null)
                if (s12 is not null)
                    hst2 = np.vstack(s12, s22);
            if (hst2 is null)
                hst2 = s22;
            if (hst2 is not null)
                if (hst1 is not null)
                    hst1 = np.hstack(hst1, hst2);
            if (hst1 is null)
                hst1 = hst2;

            return hst1;

        }

        private void GetSlices(NDArray waveFunction, Slice xIndexing1, Slice yIndexing1, Slice xIndexing2, Slice yIndexing2, out NDArray  s11, out NDArray   s21, out NDArray  s12, out NDArray s22)
        {
            s11 = GetWaveFunction(waveFunction, xIndexing1, yIndexing1);
            s21 = GetWaveFunction(waveFunction, xIndexing2, yIndexing1);
            s12 = GetWaveFunction(waveFunction, xIndexing1, yIndexing2);
            s22 = GetWaveFunction(waveFunction, xIndexing2, yIndexing2);
        }

        private NDArray  GetWaveFunction(NDArray waveFunction, Slice x, Slice y)
        {
            if(x.Length>0 && y.Length>0)
                return waveFunction[x, y ];
            return default;
        }

        private void GetSlicings(int x0, int y0, int updateSize, Direction d, out Slice xIndexing1, out Slice yIndexing1, out Slice xIndexing2, out Slice yIndexing2)
             => GetSlicings(x0 + GetX(d), y0 + GetY(d), updateSize, out xIndexing1, out yIndexing1, out xIndexing2, out yIndexing2);

        private void GetSlicings(int x0, int y0, int updateSize, out Slice xIndexing1, out Slice yIndexing1, out Slice xIndexing2, out Slice yIndexing2)
            => GetSlicings(x0 - updateSize, y0 - updateSize, x0 + updateSize+1, y0 + updateSize+1 , out xIndexing1, out yIndexing1, out xIndexing2, out yIndexing2);

        private void GetSlicings(int x1, int y1, int x2, int y2, out Slice xIndexing1, out Slice yIndexing1, out Slice xIndexing2, out Slice yIndexing2)
        {
            x1 %= _worldParams.Width; 
            x2 %= _worldParams.Width;
            y1 %= _worldParams.Height ;
            y2 %= _worldParams.Height ;
            xIndexing1 = new Slice(x1, x2);
            yIndexing1 = new Slice(y1, y2); 
            xIndexing2 = new Slice(x1, x2);
            yIndexing2 = new Slice(y1, y2);
            if (xIndexing1.Start > xIndexing1.Stop)
            {
               xIndexing1.Stop = _worldParams.Width;
               xIndexing2.Start = 0;
            }
            else
            {
                xIndexing2.Start = xIndexing2.Stop = 0;
            }
            if (yIndexing1.Start > yIndexing1.Stop)
            {
                yIndexing1.Stop = _worldParams.Height;
                yIndexing2.Start = 0;
            }
            else
            {
                yIndexing2.Start = yIndexing2.Stop = 0;
            }
        }
        private void GetTargetSlicings(  Slice xIndexing1, Slice yIndexing1, Slice xIndexing2, Slice yIndexing2,
            out Slice xIndexing1target, out Slice yIndexing1target, out Slice xIndexing2target, out Slice yIndexing2target)
        {
             xIndexing1target = new Slice(0,
                                 xIndexing1.Length.Value  );
            yIndexing1target = new Slice(0,
                                  yIndexing1.Length.Value  );

            xIndexing2target = new Slice(xIndexing1.Length.Value  ,
                                    xIndexing1.Length.Value + xIndexing2.Length.Value  );

            yIndexing2target = new Slice(yIndexing1.Length.Value ,
                                   yIndexing1.Length.Value + yIndexing2.Length.Value ) ;
        }
   
        private void SetSlice(Slice xIndexing1, Slice yIndexing1, Slice xIndexing2, Slice yIndexing2, NDArray s,
            Slice xIndexing1target, Slice yIndexing1target, Slice xIndexing2target, Slice yIndexing2target)
        {
            var targetSlice  = GetWaveFunction(WaveFunction, xIndexing1, yIndexing1 );
            if (targetSlice is not null )
            {
                 NDArray ss = s[xIndexing1target, yIndexing1target, Slice.All];
                TrySetData(targetSlice, ss); 
             }

            targetSlice  = GetWaveFunction(WaveFunction, xIndexing2, yIndexing1 );
            if (targetSlice is not null)
            {
                 NDArray ss = s[xIndexing2target, yIndexing1target, Slice.All];
                TrySetData( targetSlice, ss);
             }

            targetSlice  = GetWaveFunction(WaveFunction,xIndexing1, yIndexing2 );
            if (targetSlice is not null)
            {
                 NDArray ss = s[xIndexing1target, yIndexing2target, Slice.All];
                TrySetData(targetSlice, ss);
             }

            targetSlice  = GetWaveFunction(WaveFunction, xIndexing2, yIndexing2 );
            if (targetSlice is not null)
            {
                 NDArray ss = s[xIndexing2target, yIndexing2target, Slice.All];
                TrySetData(targetSlice, ss);
             }
        }

        private static void TrySetData(NDArray targetSlice, NDArray ss)
        {
            if(targetSlice.Shape == ss.Shape)
            {
                targetSlice.SetData(ss);

            }
            else 
            {  Debug.WriteLine    ("dude why");
            }
        }
    }

 
}
 