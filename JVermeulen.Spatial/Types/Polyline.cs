using System;
using System.Collections.Generic;
using System.Linq;

namespace JVermeulen.Spatial.Types
{
    public readonly struct Polyline
    {
        public double[][] Coordinates { get; }

        public Point[] Points => Coordinates.Select(c => new Point(c)).ToArray();

        public Polyline(double[][] coordinates)
        {
            Coordinates = coordinates;
        }

        public Polyline(Point[] points)
        {
            Coordinates = points.Select(p => p.Coordinates).ToArray();
        }

        public IEnumerable<Point> GetPoints()
        {
            for (int i = 0; i < Coordinates.Length; i++)
            {
                yield return new Point(Coordinates[i]);
            }
        }

        public override string ToString()
        {
            var points = Coordinates.Select(c => $"{c[0]} {c[1]}");

            return $"LINESTRING ({string.Join(", ", points)})";
        }
    }
}
