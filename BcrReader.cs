//*******************************************************************************************
//
// Library for reading GPS data files according to ISO 25178-7, ISO 25178-71 and EUNA 15178. 
//
// Usage:
//   1) instantiate class with filename as parameter;
//   2) get profiles or single points of the raster data;
//
// Known problems and restrictions:
//   The legacy parameters "Compression" and "CheckType" are not evaluated. The ISO standards
//   do not recommend them and they were propably never used. 
//   Trailer formatted according to ISO 25178-71 can not be interpreted (yet).
//
//*******************************************************************************************


using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bev.SurfaceRasterData;

namespace Bev.IO.BcrReader
{
    public class BcrReader
    {

        private string[] sections;

        #region Ctor
        public BcrReader(string fileName)
        {
            Status = ErrorCode.OK;
            LoadFile(fileName);
            MetaData = new Dictionary<string, string>();
            ParseHeaderSection();
            ParseMainSection();
            ParseTrailerSection();
            UpdateSurfaceDataProperties();
            CheckIfDataIsComplete();
        }
        #endregion

        #region Properties
        public ErrorCode Status { get; private set; }
        public SurfaceData RasterData { get; private set; }
        // BCR header parameters
        public string VersionField { get; private set; }
        public DateTime? CreateDate { get; private set; }
        public DateTime? ModDate { get; private set; }
        public string ManufacID { get; private set; }
        public int NumPoints { get; private set; }
        public int NumProfiles { get; private set; }
        public double XScale { get; private set; }
        public double YScale { get; private set; }
        public double ZScale { get; private set; }
        // usefull parameters
        public double XOffset { get; private set; } = 0.0;
        public double YOffset { get; private set; } = 0.0;
        public double ZOffset { get; private set; } = 0.0;
        public double SampleTemperature { get; private set; } = 20.0;
        // BCR trailer information
        public Dictionary<string, string> MetaData {get; private set;}
        #endregion

        #region Methods

        public void SetXOffset(double value)
        {
            XOffset = value;
            if (RasterData == null) return;
            RasterData.XOffset = XOffset;
        }

        public void SetYOffset(double value)
        {
            YOffset = value;
            if (RasterData == null) return;
            RasterData.YOffset = YOffset;
        }

        public void SetZOffset(double value)
        {
            ZOffset = value;
            if (RasterData == null) return;
            RasterData.ZOffset = ZOffset;
        }

        public double[] GetProfileFor(int profileIndex)
        {
            if (Status == ErrorCode.OK || Status == ErrorCode.IncompleteData)
                return RasterData.GetProfileFor(profileIndex);
            return null;
        }

        public Point3D[] GetPointsProfileFor(int profileIndex)
        {
            if (Status == ErrorCode.OK || Status == ErrorCode.IncompleteData)
                return RasterData.GetPointsProfileFor(profileIndex);
            return null;
        }

        public double GetValueFor(int pointIndex, int profileIndex)
        {
            if (Status == ErrorCode.OK || Status == ErrorCode.IncompleteData)
                return RasterData.GetValueFor(pointIndex, profileIndex);
            return double.NaN;
        }

        public Point3D GetPointFor(int pointIndex, int profileIndex)
        {
            if (Status == ErrorCode.OK || Status == ErrorCode.IncompleteData)
                return RasterData.GetPointFor(pointIndex, profileIndex);
            return null;
        }

        #endregion

        #region Private stuff

        private void LoadFile(string fileName)
        {
            try
            {
                string allText = File.ReadAllText(fileName);
                if (string.IsNullOrWhiteSpace(allText))
                {
                    Status = ErrorCode.NoData;
                    return;
                }
                sections = allText.Split(new[]{'*'}, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception)
            {
                Status = ErrorCode.NoFile;
                return;
            }
            if (sections.Length < 3 || sections.Length > 4) // there must be a CR LF after each, including the final, *
                Status = ErrorCode.InvalidSectionNumber;
        }
        
        private void ParseHeaderSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            // split to lines
            string[] headerLines = PrepareSections(sections[0]);
            if (headerLines.Length < 13)
            {
                Status = ErrorCode.BadHeaderSection;
                return;
            }
            VersionField = headerLines[0];
            string firstChar = VersionField.Substring(0, 1).ToLowerInvariant();
            if (firstChar != "a")
            {
                Status = ErrorCode.InvalidVersionField;
                return;
            }
            for (int i = 1; i < headerLines.Length; i++)
            {
                var kv = SplitToKeyValue(headerLines[i]);
                string key = kv.Item1;
                string value = kv.Item2;
                MetaData.Add(key, value);
                switch (key.ToUpper())
                {
                    case "MANUFACID":
                        ManufacID = value;
                        break;
                    case "CREATEDATE":
                        CreateDate = ParseToDateTime(value);
                        break;
                    case "MODDATE":
                        ModDate = ParseToDateTime(value);
                        break;
                    case "NUMPOINTS":
                        NumPoints = ParseToInt(value);
                        break;
                    case "NUMPROFILES":
                        NumProfiles = ParseToInt(value);
                        break;
                    case "XSCALE":
                        XScale = ParseToDouble(value);
                        break;
                    case "YSCALE":
                        YScale = ParseToDouble(value);
                        break;
                    case "ZSCALE":
                        ZScale = ParseToDouble(value);
                        break;
                    case "ZRESOLUTION":
                        // not used
                        break;
                    case "COMPRESSION":
                        // not used
                        break;
                    case "DATATYPE":
                        //not used
                        break;
                    case "CHECKTYPE":
                        // not used
                        break;
                    default:
                        break;
                }
            }
            AnalyzeHeaderParseError();
        }

        private void ParseMainSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            RasterData = new SurfaceData(NumPoints, NumProfiles);
            string[] mainLines = PrepareSections(sections[1]);
            foreach (var line in mainLines)
            {
                double[] values = ExtractValuesFromDataLine(line);
                foreach (var value in values)
                {
                    RasterData.FillUpData(value * ZScale);
                }
            }
        }

        private void ParseTrailerSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            if (sections.Length != 4)
            {
                // even for missing trailer section add a line of information
                MetaData.Add("MetaDataInFile", "none");
                return;
            }
            string[] trailerLines = PrepareSections(sections[2]);
            foreach (var line in trailerLines)
            {
                var kv = SplitToKeyValue(line);
                MetaData.Add(kv.Item1, kv.Item2);
            }
            ParseMetadataForParameters();
        }

        // This is very implementation specific!
        // The trailer section of the BCR file is scanned for case sensitive keys
        // moreover the numerical value must be given in defined units (m and °C) 
        public void ParseMetadataForParameters()
        {
            if (MetaData.ContainsKey("ScanFieldOriginX"))
                XOffset = ParseFirstTokenToDouble(MetaData["ScanFieldOriginX"]);
            if (MetaData.ContainsKey("ScanFieldOriginY"))
                YOffset = ParseFirstTokenToDouble(MetaData["ScanFieldOriginY"]);
            if (MetaData.ContainsKey("ScanFieldOriginZ"))
                ZOffset = ParseFirstTokenToDouble(MetaData["ScanFieldOriginZ"]);
            if (MetaData.ContainsKey("SampleTemperature"))
                SampleTemperature = ParseFirstTokenToDouble(MetaData["SampleTemperature"]);
        }

        // from here on we have some handy helper methods

        private void CheckIfDataIsComplete()
        {
            if (Status != ErrorCode.OK) return;
            if (RasterData == null) return;
            if (RasterData.IsDataComplete) return;
            Status = ErrorCode.IncompleteData;
        }

        private void UpdateSurfaceDataProperties()
        {
            if (RasterData == null) return;
            RasterData.XScale = XScale;
            RasterData.YScale = YScale;
            RasterData.ZScale = ZScale;
            SetXOffset(XOffset);
            SetYOffset(YOffset);
            SetZOffset(ZOffset);
            RasterData.VersionField = VersionField;
            RasterData.Manufacturer = ManufacID;
            RasterData.ModifyDate = ModDate;
            RasterData.CreateDate = CreateDate;
            RasterData.IsMetricUnit = true;
            RasterData.XUnit = "m";
            RasterData.YUnit = "m";
            RasterData.ZUnit = "m";
            RasterData.Description = "Created by BcrReader";
            RasterData.MetaData = MetaData;
        }

        private string[] PrepareSections(string rawText)
        {
            string[] textLines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // now remove any comments
            for (int i = 0; i < textLines.Length; i++)
            {
                textLines[i] = RemoveComment(textLines[i]);
            }
            return textLines;
        }

        private string RemoveComment(string rawLine)
        {
            if (rawLine.Contains(";"))
            {
                int index = rawLine.IndexOf(';');
                return rawLine.Substring(0, index).Trim();
            }
            return rawLine.Trim();
        }

        private (string, string) SplitToKeyValue(string line)
        {
            string[] pair = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (pair.Length != 2)
            {
                return (" ", " ");
            }
            return (pair[0].Trim(), pair[1].Trim());
        }

        private double[] ExtractValuesFromDataLine(string dataLine)
        {
            string[] tokens = dataLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double[] results = new double[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                results[i] = ParseToDouble(tokens[i]);
            }
            return results;
        }

        private DateTime? ParseToDateTime(string value)
        {
            try
            {
                DateTime result = DateTime.ParseExact(value, "ddMMyyyyHHmm", CultureInfo.InvariantCulture);
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private double ParseFirstTokenToDouble(string value)
        {
            string[] tokens = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return double.NaN;
            foreach (var token in tokens)
            {
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    return result;
            }
            return double.NaN;
        }

        private double ParseToDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;
            return double.NaN;
        }

        private int ParseToInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return -1;
        }

        private void AnalyzeHeaderParseError()
        {
            if (NumPoints <= 0)
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            if (NumProfiles <= 0)
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            if (double.IsNaN(XScale))
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            if (double.IsNaN(YScale))
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            if (double.IsNaN(ZScale))
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            // The following parameters are not needed actually
            // checks could be discarded
            if (CreateDate == null)
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
            if (ModDate == null)
            {
                Status = ErrorCode.HeaderParseError;
                return;
            }
        }

        #endregion
    }

    public enum ErrorCode
    {
        OK,
        IncompleteData,
        Unknown,
        NoFile,
        NoData,
        InvalidSectionNumber,
        BadHeaderSection,
        InvalidVersionField,
        HeaderParseError
    }
}
