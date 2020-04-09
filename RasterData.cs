namespace Bev.IO.BcrReader
{
    public class RasterData
    {
        
        public RasterData(int numPoints, int numProfiles)
        {
            NumberOfPointsPerProfile = numPoints;
            NumberOfProfiles = numProfiles;
            zValues = new double[NumberOfPointsPerProfile, NumberOfProfiles];
            XScale = 1.0;
            YScale = 1.0;
        }

        public int NumberOfPointsPerProfile { get; private set; } // NumPoints
        public int NumberOfProfiles { get; private set; } // NumProfiles
        public double XScale { get; set; }
        public double YScale { get; set; }

        public double[] GetProfileFor(int profileIndex)
        {
            double[] profile = new double[NumberOfPointsPerProfile];
            for (int i = 0; i < NumberOfPointsPerProfile; i++)
            {
                profile[i] = GetValueFor(i, profileIndex);
            }
            return profile;
        }

        public double GetValueFor(int pointIndex, int profileIndex)
        {
            if (pointIndex < 0) return double.NaN;
            if (profileIndex < 0) return double.NaN;
            if (pointIndex >= NumberOfPointsPerProfile) return double.NaN;
            if (profileIndex >= NumberOfProfiles) return double.NaN;
            return zValues[pointIndex, profileIndex];
        }

        public Point3D GetPointFor(int pointIndex, int profileIndex)
        {
            double xCoordinate = pointIndex * XScale;
            double yCoordinate = profileIndex * YScale;
            if (pointIndex < 0) xCoordinate = double.NaN;
            if (profileIndex < 0) yCoordinate = double.NaN;
            if (pointIndex >= NumberOfPointsPerProfile) xCoordinate = double.NaN;
            if (profileIndex >= NumberOfProfiles) yCoordinate = double.NaN;
            return new Point3D(xCoordinate, yCoordinate, GetValueFor(pointIndex, profileIndex));
        }

        private double[,] zValues;

    }
}
