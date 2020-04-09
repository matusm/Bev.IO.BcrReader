using System;
using System.Globalization;
using System.IO;

namespace Bev.IO.BcrReader
{
    public class BcrReader
    {

        private string[] sections;

        public BcrReader(string fileName)
        {
            Status = ErrorCode.OK;
            LoadDataFromFile(fileName);
            ParseHeaderSection();
            ParseDataSection();
            ParseTrailerSection();
        }

 
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



        private void ParseHeaderSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            // split to lines
            string[] headerLines = sections[0].Split(new[]{'\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if(headerLines.Length<13)
            {
                Status = ErrorCode.BadHeaderSection;
                return;
            }
            // now remove any comments
            for (int i = 0; i < headerLines.Length; i++)
            {
                headerLines[i] = RemoveComment(headerLines[i]);
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


        private (string, string) SplitToKeyValue(string line)
        {
            string[] pair = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if(pair.Length!=2)
            {
                return (" ", " ");
            }
            return (pair[0].Trim().ToUpper(), pair[1].Trim());
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

        private void ParseDataSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            throw new NotImplementedException();
        }

        private void ParseTrailerSection()
        {
            if (Status != ErrorCode.OK)
            {
                return;
            }
            if (sections.Length != 3)
            {
                return;
            }
            
            throw new NotImplementedException();
        }

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
                char[] sectionDelimiter = { '*' };
                sections = allText.Split(sectionDelimiter, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception)
            {
                Status = ErrorCode.NoFile;
                return;
            }
            if (sections.Length < 2 || sections.Length > 3)
                Status = ErrorCode.InvalidSectionNumber;
        }
    }

    public enum ErrorCode
    {
        OK,
        Unknown,
        NoFile,
        NoData,
        InvalidSectionNumber,
        BadHeaderSection,
        InvalidVersionField


    }
}
