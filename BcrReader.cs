using System;
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
        public DateTime CreateDate { get; private set; }
        public DateTime ModDate { get; private set; }
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
