using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Bev.IO.BcrReader
{
    public class BcrReader
    {

        private string[] sections;
        private RasterData rasterData;

        #region Ctor
        public BcrReader(string fileName)
        {
            Status = ErrorCode.OK;
            LoadDataFromFile(fileName);
            ParseHeaderSection();
            ParseMainSection();
            ParseTrailerSection();
        }
        #endregion

        #region Properties
        public ErrorCode Status { get; private set; }
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
        // BCR trailer information
        public Dictionary<string, string> MetaData {get; private set;}
        #endregion

        #region Methods

        public double[] GetProfileFor(int profileIndex)
        {
            if (Status == ErrorCode.OK)
                return rasterData.GetProfileFor(profileIndex);
            return null;
        }

        public double GetValueFor(int pointIndex, int profileIndex)
        {
            if (Status == ErrorCode.OK)
                return rasterData.GetValueFor(pointIndex, profileIndex);
            return double.NaN;
        }

        public Point3D GetPointFor(int pointIndex, int profileIndex)
        {
            if (Status == ErrorCode.OK)
                return rasterData.GetPointFor(pointIndex, profileIndex);
            return null;
        }

        #endregion

        #region Private stuff

        private void LoadDataFromFile(string fileName)
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
            if (sections.Length < 2 || sections.Length > 3)
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
            // obsolete parameters are not parsed
            for (int i = 1; i < headerLines.Length; i++)
            {
                var kv = SplitToKeyValue(headerLines[i]);
                string key = kv.Item1;
                string value = kv.Item2;
                switch (key)
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
            rasterData = new RasterData(NumPoints, NumProfiles);
            rasterData.XScale = XScale;
            rasterData.YScale = YScale;
            string[] mainLines = PrepareSections(sections[1]);
            foreach (var line in mainLines)
            {
                double[] values = ExtractValuesFromDataLine(line);
                foreach (var value in values)
                {
                    rasterData.FillUpData(value * ZScale);
                }
            }
        }

        private void ParseTrailerSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            if (sections.Length != 3)
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
        }

        // from here on we have some handy helper methods

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
            return (pair[0].Trim().ToUpper(), pair[1].Trim());
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

        private double ParseToDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out double result))
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
        Unknown,
        NoFile,
        NoData,
        InvalidSectionNumber,
        BadHeaderSection,
        InvalidVersionField,
        HeaderParseError
    }
}
