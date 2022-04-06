using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JVermeulen.Spatial.Types
{
    public readonly struct Polygon
    {
        public double[][][] Coordinates { get; }
        //internal string Text => string.Join(", ", Polylines.Select(p => p.Text));

        public Polyline[] Polylines => Coordinates.Select(c => new Polyline(c)).ToArray();

        public Polygon(double[][][] coordinates)
        {
            Coordinates = coordinates;
        }

        public Polygon(Polyline[] polylines)
        {
            Coordinates = polylines.Select(p => p.Coordinates).ToArray();
        }

        public IEnumerable<Polyline> GetPolyline()
        {
            for (int i = 0; i < Coordinates.Length; i++)
            {
                yield return new Polyline(Coordinates[i]);
            }
        }

        public override string ToString()
        {
            var points = Coordinates.Select(c => c.Select(d => $"{d[0]} {d[1]}"));
            var polylines = points.Select(c => $"({string.Join(", ", c)})");
            var polygons = polylines.Select(c => $"({string.Join(", ", c)})");

            return $"POLYGON ({string.Join(", ", polylines)})";
        }
    }
}
