namespace Bev.IO.BcrReader
{
    public class RasterData
    {

        private readonly double[,] zValues;
        private int runningIndex;

        #region Ctor
        public RasterData(int numPoints, int numProfiles)
        {
            NumberOfPointsPerProfile = numPoints;
            NumberOfProfiles = numProfiles;
            zValues = new double[NumberOfPointsPerProfile, NumberOfProfiles];
            XScale = 1.0;
            YScale = 1.0;
            ResetRunningIndex();
        }
        #endregion

        #region Properties
        public int NumberOfPointsPerProfile { get; private set; } // NumPoints
        public int NumberOfProfiles { get; private set; } // NumProfiles
        public double XScale { get; set; }
        public double YScale { get; set; }
        #endregion

        #region Methods

        // the hight data is filled up pointwise in the raster data array
        // this is a slow process but compatible with the file parsing technique
        public void FillUpData(double value)
        {
            //int pointsIndex = runningIndex - profileIndex * NumberOfPointsPerProfile;
            int pointsIndex = runningIndex % NumberOfPointsPerProfile;
            if (pointsIndex >= NumberOfPointsPerProfile)
                return;
            int profileIndex = runningIndex / NumberOfPointsPerProfile;
            if (profileIndex >= NumberOfProfiles)
                return;
            zValues[pointsIndex, profileIndex] = value;
            runningIndex++;
        }

        public double[] GetProfileFor(int profileIndex)
        {
            double[] profile = new double[NumberOfPointsPerProfile];
            for (int i = 0; i < NumberOfPointsPerProfile; i++)
            {
                // this assures that NaN array is returned for a profile index outside the range
                profile[i] = GetValueFor(i, profileIndex);
            }
            return profile;
        }

        public Point3D[] GetPointsProfileFor(int profileIndex)
        {
            Point3D[] profile = new Point3D[NumberOfPointsPerProfile];
            for (int i = 0; i < NumberOfPointsPerProfile; i++)
            {
                // this assures that NaN array is returned for a profile index outside the range
                profile[i] = GetPointFor(i, profileIndex);
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

        #endregion

        #region Private stuff

        private void ResetRunningIndex()
        {
            runningIndex = 0;
        }

        #endregion
    }
}
