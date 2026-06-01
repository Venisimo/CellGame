using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMP_RGZ
{
    public class Vector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Length => Math.Sqrt(X * X + Y * Y);
        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }
        public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
        public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);
        public static Vector operator *(Vector v, double scalar) => new Vector(v.X * scalar, v.Y * scalar);
        public static Vector operator *(double scalar, Vector v) => new Vector(v.X * scalar, v.Y * scalar);

        public Vector Normalize()
        {
            double len = Length;
            if (len == 0) return new Vector(0, 0);
            return new Vector(X / len, Y / len);
        }
        public double DistanceTo(Vector other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
