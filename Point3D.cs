using System.Globalization;

namespace Bev.IO.BcrReader
{
    public class Point3D
    {
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public override string ToString()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            return $"[Point3D - X:{X.ToString()} Y:{Y.ToString()} Z:{Z.ToString()}]";
        }
    }
}
