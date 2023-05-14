using System;

namespace NowPlaying.Core
{
    public class RollingAvgLong : RollingAverage<long>
    {
        public RollingAvgLong(int depth, long initValue) : base(depth, initValue, (a, b) => a + b, (n, d) => n / d) {}
    }

    public class RollingAverage<T>
    {
        public int Depth { get; private set; }

        public delegate T Add(T a, T b);
        public delegate T DivByInt(T num, int den);

        private readonly Add add;
        private readonly DivByInt divByInt;

        private T[] values;
        private int index;

        public RollingAverage(int depth, T initValue, Add add, DivByInt divByInt)
        {
            this.add = add;
            this.divByInt = divByInt;
            this.Depth = depth;

            if (depth < 1)
            {
                throw new ArgumentException("Depth must be >= 1", "depth");
            }

            this.index = 0;
            this.values = new T[depth];
            for (int i=0; i < depth; i++) { this.values[i] = initValue; }
        }

        public void Push(T value)
        {
            values[index++] = value;
            if (index == Depth) 
            { 
                index = 0; 
            }
        }

        public T GetAverage()
        {
            T sum = values[0];
            T avg;
            for(int i=1; i < Depth; i++)
            {
                sum = add(sum, values[i]);
            }
            avg = divByInt( sum, Depth );
            return avg;
        }
    }
}
