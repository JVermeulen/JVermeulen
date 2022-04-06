using System;

namespace JVermeulen.Spatial.Types
{
    public readonly struct Point
    {
        public double[] Coordinates { get; }

        public double X
        {
            get { return Coordinates[0]; }
            set { Coordinates[0] = value; }
        }

        public double Y
        {
            get { return Coordinates[1]; }
            set { Coordinates[1] = value; }
        }

        public Point(double[] coordinates)
        {
            Coordinates = coordinates;
        }

        public Point(double x, double y)
        {
            Coordinates = new double[] { x, y };
        }

        public override string ToString()
        {
            return $"POINT ({X} {Y})";
        }
    }
}
