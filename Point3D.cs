using System;
using System.Globalization;

namespace Bev.IO.BcrReader
{
    public class Point3D : IComparable
    {
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public int CompareTo(object obj)
        {
            Point3D objc = obj as Point3D;
            if (X < objc.X) return -1;
            if (X > objc.X) return 1;
            return 0;
        }

        public override string ToString()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            return $"[Point3D - X:{X.ToString()} Y:{Y.ToString()} Z:{Z.ToString()}]";
        }
    }
}
