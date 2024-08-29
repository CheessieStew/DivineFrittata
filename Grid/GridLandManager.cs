using GalaSoft.MvvmLight.CommandWpf;
using System.ComponentModel;
using System.Data;
using System.Windows.Input;

namespace Grid
{
    public class GridLandManager : INotifyPropertyChanged
    {
        public virtual GridLand GridLand { get; }
        public event PropertyChangedEventHandler PropertyChanged;
        public ICommand ResetParams { get; }
        public ICommand SetParams { get; }
        public ICommand RestartWorld { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float MutateWeight { get; set; }
        public bool Normalize { get; set; }
        public int MaxFunctionIdx { get; }
        public int FunctionIdx { get; set; }
        public float Speed { get; set; }
        public float UpdateFreq { get; set; }
        public DataView NeighbourMatrix => NeighbourMatrixTable?.DefaultView;
        private DataTable NeighbourMatrixTable;
        protected WorldParams LastParams { get; private set; }

        public GridLandManager()
        {
            GridLand = new GridLand();
            MaxFunctionIdx = GridLand.WorldParams.MaxFunctionIdx;

            ResetParams = new RelayCommand(() => UpdateFrom(LastParams));
            SetParams = new RelayCommand(OnSetParams);
            RestartWorld = new RelayCommand(GridLand.Reset);

            UpdateFrom(GridLand.WorldParams);
        }


        protected void OnSetParams() => UpdateFrom(GridLand.SetParams(GetParams()));

        protected WorldParams GetParams()
        {
            return new WorldParams(GetRules(), MutateWeight, Width, Height, Normalize, MaxFunctionIdx, FunctionIdx, Speed, UpdateFreq);
        }

        private WorldRules GetRules()
        {
            return new WorldRules(NeighbourMatrixTable);
        }

        protected void UpdateFrom(WorldParams wp)
        {
            LastParams = wp;
            Width = wp.Width;
            Height = wp.Height;
            MutateWeight = wp.MutateWeight;
            Normalize = wp.Normalize;
            FunctionIdx = wp.FunctionIdx;
            Speed = wp.Speed;
            UpdateFreq = wp.UpdateFreq;
            UpdateRules(wp.Rules);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MutateWeight)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Normalize)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionIdx)));
            // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof()));

        }

        private void UpdateRules(WorldRules rules)
        {
            NeighbourMatrixTable = new DataTable();


            for (int i = 0; i < rules.NumColors; i++)
            {
                NeighbourMatrixTable.Columns.Add(i.ToString(), typeof(float));
            }

            for (int row = 0; row < rules.NumColors; row++)
            {
                DataRow dr = NeighbourMatrixTable.NewRow();
                for (int col = 0; col < rules.NumColors; col++)
                {
                    dr[col] = rules.NeighbourColors[row, col];
                }
                NeighbourMatrixTable.Rows.Add(dr);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NeighbourMatrix)));

        }
    }
}
