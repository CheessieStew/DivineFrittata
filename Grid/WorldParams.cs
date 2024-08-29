namespace Grid
{
    public struct WorldParams
    {
        public WorldRules Rules { get; }

        private float _mutate;
         public float MutateWeight
        {
            get => _mutate; 
            set
            {
                 _mutate = value;
            }
        } 
 
        public  int Width { get; }
        public int Height { get; }
        public bool  Normalize { get; set; }

        public int  MaxFunctionIdx { get;  }
        public int FunctionIdx { get; set; }


        public float  Speed { get; set; }
        public float UpdateFreq { get; set; }

        public WorldParams( WorldRules rules, float mutateWeight,  int width, int height, bool normalize, int maxFunctionIdx, int functionIdx, float speed, float updateFreq)
        {
            _mutate = mutateWeight;
             Width = width;
            Height = height;
            Normalize = normalize;
            Rules = rules;
            MaxFunctionIdx = maxFunctionIdx;
            FunctionIdx = functionIdx;
            Speed = speed;
            UpdateFreq = updateFreq;
        }

        public WorldParams Update(WorldParams pars)
        {
            
            Normalize = pars.Normalize;
            FunctionIdx = pars.FunctionIdx;
            Speed = pars.Speed;
            UpdateFreq = pars.UpdateFreq;
            MutateWeight = pars.MutateWeight;
            return this;
        }
    }

 
}
 