/*
 * This file is auto-generated. 
*/

using System;
using System.Collections.Generic;
using OriginGPSUtil.NMEA;

namespace OriginGPSUtil.NMEA
{
    public enum Talker
    {
        /// <summary>
        /// GP
        /// </summary>
        GPS,

        /// <summary>
        /// GL
        /// </summary>
        GLONASS,

        /// <summary>
        /// GA
        /// </summary>
        Galileo,

        /// <summary>
        /// GN
        /// </summary>
        GNSS,

        /// <summary>
        /// BD
        /// </summary>
        BeiDou,

        /// <summary>
        /// GB
        /// </summary>
        BeiDou2,

        /// <summary>
        /// QZ
        /// </summary>
        QZSS,

        /// <summary>
        /// P
        /// </summary>
        Proprietary,

    }
    public enum FixQuality
    {
        /// <summary>
        /// 0
        /// </summary>
        NoFix,

        /// <summary>
        /// 1
        /// </summary>
        GPSSPS,

        /// <summary>
        /// 2
        /// </summary>
        DifferentialGPS,

        /// <summary>
        /// 6
        /// </summary>
        DeadReckoning,

    }

    public static class Resolver
    {
        public static class Dictionaries
        {
            public static Dictionary<string, Talker> TalkerResolverDictionary = new Dictionary<string, Talker>()
            {
                ["GP"] = Talker.GPS,
                ["GL"] = Talker.GLONASS,
                ["GA"] = Talker.Galileo,
                ["GN"] = Talker.GNSS,
                ["BD"] = Talker.BeiDou,
                ["GB"] = Talker.BeiDou2,
                ["QZ"] = Talker.QZSS,
                ["P"] = Talker.Proprietary,
            };
            public static Dictionary<string, FixQuality> FixQualityResolverDictionary = new Dictionary<string, FixQuality>()
            {
                ["0"] = FixQuality.NoFix,
                ["1"] = FixQuality.GPSSPS,
                ["2"] = FixQuality.DifferentialGPS,
                ["6"] = FixQuality.DeadReckoning,
            };

        }

        public static Talker ResolveTalkerType(string TalkerType) { try { return Dictionaries.TalkerResolverDictionary[TalkerType]; } catch (Exception ex) { throw ex; } }
        public static FixQuality ResolveFixQualityType(string FixQualityType) { try { return Dictionaries.FixQualityResolverDictionary[FixQualityType]; } catch (Exception ex) { throw ex; } }
    }

    public class Parsers
    {
        public static Dictionary<string, Func<string, object>> ParserDictionary = new Dictionary<string, Func<string, object>>()
        {
            ["int"] = s => int.Parse(s),
            ["string"] = s => s,
            ["char"] = s => char.Parse(s),
            ["float"] = s => float.Parse(s),
            ["DateTime"] = s => DateTime.ParseExact(s, new string[] { "HHmmss.fff", "HHmmss" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None),
            ["Talker"] = s => Resolver.ResolveTalkerType(s),
            ["FixQuality"] = s => Resolver.ResolveFixQualityType(s),
        };

        public static string[] SplitMessage(string message, out string Talker, out string Name)
        {
            var start = message.Substring(1, message.IndexOf(','));
            message = message.Substring(start.Length + 1);

            var split = message.Split(',');

            if (start[0] != 'P')
            {
                Talker = start.Substring(0, 2);
                Name = start.Substring(2, 3);
            }

            else
            {
                Talker = "P";
                Name = start.Substring(1);
            }

            var starLoc = split[split.Length - 1].IndexOf('*');

            split[split.Length - 1] = split[split.Length - 1].Substring(0, starLoc);                        // Mark - removing of checksum from the last item

            return split;
        }

        public static object Parse(string[] splitArray, int location, string type, bool nullable)
        {
            if (string.IsNullOrEmpty(splitArray[location]))
            {
                if (nullable)
                    return null;
                else
                    throw new Exception($"Error parsing sentence - variable {location + 1} is null, shouldn't be.");
            }

            try
            {
                return Parsers.ParserDictionary[type](splitArray[location]);
            }

            catch (Exception ex)
            {
                throw new Exception($"Error parsing sentence - variable {location + 1} failed parsing, reason {ex.Message}");
            }
        }
    }

    public class SatelliteInformation { public int PRN { get; set; } public int Elevation { get; set; } public int Azimuth { get; set; } public Nullable<int> CN0 { get; set; } }

    public class NMEASentence
    {
        public Talker Talker;

        protected string originalSentence;
        public string OriginalSentence
        {
            get
            {
                if (!DataValid)
                    throw new Exception("Data is not valid, message data and original sentence are out of sync.");

                return originalSentence;
            }

            set
            {
                originalSentence = value;
                Validate();
            }
        }

        protected bool dataValid;
        /// <summary>
        /// Valid data means the sentence data is correctly parsed according to OriginalSentence
        /// </summary>
        public bool DataValid
        {
            get
            {
                return dataValid;
            }

            protected set
            {
                dataValid = value;
            }
        }

        public void Invalidate()
        {
            DataValid = false;
        }

        /// <summary>
        /// Parses the data again accordig to info in OriginalSentence
        /// </summary>
        public virtual void Validate() { }

        /// <summary>
        /// Build OriginalSentence according to the current data
        /// </summary>
        public virtual void RebuildOriginalSentence() { }


        /// <summary>
        /// Parses a GGA NMEA message into a predefined NMEA Sentence class
        /// </summary>
        /// <param name="message">NMEA Message to parse</param>
        /// <returns>GGASentence class</returns>
        public static Sentences.GGASentence ParseGGA(string message)
        {
            Sentences.GGASentence sentence = new Sentences.GGASentence();
            string talker, name;
            var splitMessage = Parsers.SplitMessage(message, out talker, out name);

            if (name != "GGA")
                throw new Exception($"Sentence name {name} does not correspond to this GGA parser");

            sentence.Time = (DateTime)Parsers.Parse(splitMessage, 0, "DateTime", true);

            sentence.Latitude = (float)Parsers.Parse(splitMessage, 1, "float", false);

            sentence.LatitudeCardinal = (char)Parsers.Parse(splitMessage, 2, "char", false);

            sentence.Longitude = (float)Parsers.Parse(splitMessage, 3, "float", false);

            sentence.LongitudeCardinal = (char)Parsers.Parse(splitMessage, 4, "char", false);

            sentence.FixQuality = (FixQuality)Parsers.Parse(splitMessage, 5, "FixQuality", false);

            sentence.NumSatellites = (int)Parsers.Parse(splitMessage, 6, "int", false);

            sentence.HDOP = (float)Parsers.Parse(splitMessage, 7, "float", false);

            sentence.Altitude = (float)Parsers.Parse(splitMessage, 8, "float", false);

            sentence.AltitudeUnit = (char)Parsers.Parse(splitMessage, 9, "char", false);

            sentence.WGS84 = (float)Parsers.Parse(splitMessage, 10, "float", false);

            sentence.WGS84Unit = (char)Parsers.Parse(splitMessage, 11, "char", false);

            sentence.DGPSTime = (Nullable<int>)Parsers.Parse(splitMessage, 12, "int", true);

            sentence.DGPSRefID = (Nullable<int>)Parsers.Parse(splitMessage, 13, "int", true);

            sentence.dataValid = true;
            sentence.originalSentence = message;
            sentence.Talker = Resolver.ResolveTalkerType(talker);

            return sentence;
        }
        /// <summary>
        /// Parses a RMC NMEA message into a predefined NMEA Sentence class
        /// </summary>
        /// <param name="message">NMEA Message to parse</param>
        /// <returns>RMCSentence class</returns>
        public static Sentences.RMCSentence ParseRMC(string message)
        {
            Sentences.RMCSentence sentence = new Sentences.RMCSentence();
            string talker, name;
            var splitMessage = Parsers.SplitMessage(message, out talker, out name);

            if (name != "RMC")
                throw new Exception($"Sentence name {name} does not correspond to this RMC parser");

            sentence.TIme = (DateTime)Parsers.Parse(splitMessage, 0, "DateTime", false);

            sentence.Status = (char)Parsers.Parse(splitMessage, 1, "char", false);

            sentence.Latitude = (float)Parsers.Parse(splitMessage, 2, "float", false);

            sentence.LatitudeCardinal = (char)Parsers.Parse(splitMessage, 3, "char", false);

            sentence.Longitute = (float)Parsers.Parse(splitMessage, 4, "float", false);

            sentence.LongitudeCardinal = (char)Parsers.Parse(splitMessage, 5, "char", false);

            sentence.SpeedOverGround = (float)Parsers.Parse(splitMessage, 6, "float", false);

            sentence.CourseOverGround = (float)Parsers.Parse(splitMessage, 7, "float", false);

            sentence.Date = (DateTime)Parsers.Parse(splitMessage, 8, "DateTime", false);

            sentence.MagneticVariation = (Nullable<float>)Parsers.Parse(splitMessage, 9, "float", true);

            sentence.EastWestIndicator = (Nullable<char>)Parsers.Parse(splitMessage, 10, "char", true);

            sentence.Mode = (char)Parsers.Parse(splitMessage, 11, "char", false);

            sentence.dataValid = true;
            sentence.originalSentence = message;
            sentence.Talker = Resolver.ResolveTalkerType(talker);

            return sentence;
        }
        /// <summary>
        /// Parses a GSV NMEA message into a predefined NMEA Sentence class
        /// </summary>
        /// <param name="message">NMEA Message to parse</param>
        /// <returns>GSVSentence class</returns>
        public static Sentences.GSVSentence ParseGSV(string message)
        {
            Sentences.GSVSentence sentence = new Sentences.GSVSentence();
            string talker, name;
            var splitMessage = Parsers.SplitMessage(message, out talker, out name);

            if (name != "GSV")
                throw new Exception($"Sentence name {name} does not correspond to this GSV parser");

            sentence.NumMessages = (int)Parsers.Parse(splitMessage, 0, "int", false);

            sentence.MessageNumber = (int)Parsers.Parse(splitMessage, 1, "int", false);

            sentence.SattelitesInView = (int)Parsers.Parse(splitMessage, 2, "int", false);
            sentence.SatteliteInformation[0] = new SatelliteInformation();
            sentence.SatteliteInformation[0].PRN = (int)Parsers.Parse(splitMessage, 3, "int", false);

            sentence.SatteliteInformation[0].Elevation = (int)Parsers.Parse(splitMessage, 4, "int", false);

            sentence.SatteliteInformation[0].Azimuth = (int)Parsers.Parse(splitMessage, 5, "int", false);

            sentence.SatteliteInformation[0].CN0 = (Nullable<int>)Parsers.Parse(splitMessage, 6, "int", true);
            sentence.SatteliteInformation[1] = new SatelliteInformation();
            sentence.SatteliteInformation[1].PRN = (int)Parsers.Parse(splitMessage, 7, "int", false);

            sentence.SatteliteInformation[1].Elevation = (int)Parsers.Parse(splitMessage, 8, "int", false);

            sentence.SatteliteInformation[1].Azimuth = (int)Parsers.Parse(splitMessage, 9, "int", false);

            sentence.SatteliteInformation[1].CN0 = (Nullable<int>)Parsers.Parse(splitMessage, 10, "int", true);
            sentence.SatteliteInformation[2] = new SatelliteInformation();
            sentence.SatteliteInformation[2].PRN = (int)Parsers.Parse(splitMessage, 11, "int", false);
            sentence.SatteliteInformation[2].Elevation = (int)Parsers.Parse(splitMessage, 12, "int", false);
            sentence.SatteliteInformation[2].Azimuth = (int)Parsers.Parse(splitMessage, 13, "int", false);
            sentence.SatteliteInformation[2].CN0 = (Nullable<int>)Parsers.Parse(splitMessage, 14, "int", true);

            if (3 == sentence.SattelitesInView)
            {
                sentence.SatteliteInformation[3] = new SatelliteInformation();
                sentence.SatteliteInformation[3].CN0 = 0;
                return sentence;
            }

            sentence.SatteliteInformation[3] = new SatelliteInformation();
            sentence.SatteliteInformation[3].PRN = (int)Parsers.Parse(splitMessage, 15, "int", false);
            sentence.SatteliteInformation[3].Elevation = (int)Parsers.Parse(splitMessage, 16, "int", false);
            sentence.SatteliteInformation[3].Azimuth = (int)Parsers.Parse(splitMessage, 17, "int", false);
            sentence.SatteliteInformation[3].CN0 = (Nullable<int>)Parsers.Parse(splitMessage, 18, "int", true);

            sentence.dataValid = true;
            sentence.originalSentence = message;
            sentence.Talker = Resolver.ResolveTalkerType(talker);

            return sentence;
        }
    }

    namespace Sentences
    {
        public class GGASentence : NMEASentence { private DateTime _Time; public DateTime Time { get { return _Time; } set { _Time = value; DataValid = false; } } private float _Latitude; public float Latitude { get { return _Latitude; } set { _Latitude = value; DataValid = false; } } private char _LatitudeCardinal; public char LatitudeCardinal { get { return _LatitudeCardinal; } set { _LatitudeCardinal = value; DataValid = false; } } private float _Longitude; public float Longitude { get { return _Longitude; } set { _Longitude = value; DataValid = false; } } private char _LongitudeCardinal; public char LongitudeCardinal { get { return _LongitudeCardinal; } set { _LongitudeCardinal = value; DataValid = false; } } private FixQuality _FixQuality; public FixQuality FixQuality { get { return _FixQuality; } set { _FixQuality = value; DataValid = false; } } private int _NumSatellites; public int NumSatellites { get { return _NumSatellites; } set { _NumSatellites = value; DataValid = false; } } private float _HDOP; public float HDOP { get { return _HDOP; } set { _HDOP = value; DataValid = false; } } private float _Altitude; public float Altitude { get { return _Altitude; } set { _Altitude = value; DataValid = false; } } private char _AltitudeUnit; public char AltitudeUnit { get { return _AltitudeUnit; } set { _AltitudeUnit = value; DataValid = false; } } private float _WGS84; public float WGS84 { get { return _WGS84; } set { _WGS84 = value; DataValid = false; } } private char _WGS84Unit; public char WGS84Unit { get { return _WGS84Unit; } set { _WGS84Unit = value; DataValid = false; } } private Nullable<int> _DGPSTime; public Nullable<int> DGPSTime { get { return _DGPSTime; } set { _DGPSTime = value; DataValid = false; } } private Nullable<int> _DGPSRefID; public Nullable<int> DGPSRefID { get { return _DGPSRefID; } set { _DGPSRefID = value; DataValid = false; } } }
        public class RMCSentence : NMEASentence { private DateTime _TIme; public DateTime TIme { get { return _TIme; } set { _TIme = value; DataValid = false; } } private char _Status; public char Status { get { return _Status; } set { _Status = value; DataValid = false; } } private float _Latitude; public float Latitude { get { return _Latitude; } set { _Latitude = value; DataValid = false; } } private char _LatitudeCardinal; public char LatitudeCardinal { get { return _LatitudeCardinal; } set { _LatitudeCardinal = value; DataValid = false; } } private float _Longitute; public float Longitute { get { return _Longitute; } set { _Longitute = value; DataValid = false; } } private char _LongitudeCardinal; public char LongitudeCardinal { get { return _LongitudeCardinal; } set { _LongitudeCardinal = value; DataValid = false; } } private float _SpeedOverGround; public float SpeedOverGround { get { return _SpeedOverGround; } set { _SpeedOverGround = value; DataValid = false; } } private float _CourseOverGround; public float CourseOverGround { get { return _CourseOverGround; } set { _CourseOverGround = value; DataValid = false; } } private DateTime _Date; public DateTime Date { get { return _Date; } set { _Date = value; DataValid = false; } } private Nullable<float> _MagneticVariation; public Nullable<float> MagneticVariation { get { return _MagneticVariation; } set { _MagneticVariation = value; DataValid = false; } } private Nullable<char> _EastWestIndicator; public Nullable<char> EastWestIndicator { get { return _EastWestIndicator; } set { _EastWestIndicator = value; DataValid = false; } } private char _Mode; public char Mode { get { return _Mode; } set { _Mode = value; DataValid = false; } } }
        public class GSVSentence : NMEASentence { private int _NumMessages; public int NumMessages { get { return _NumMessages; } set { _NumMessages = value; DataValid = false; } } private int _MessageNumber; public int MessageNumber { get { return _MessageNumber; } set { _MessageNumber = value; DataValid = false; } } private int _SattelitesInView; public int SattelitesInView { get { return _SattelitesInView; } set { _SattelitesInView = value; DataValid = false; } } private SatelliteInformation[] _SatteliteInformation = new SatelliteInformation[4]; public SatelliteInformation[] SatteliteInformation { get { return _SatteliteInformation; } set { _SatteliteInformation = value; DataValid = false; } } }
    }
}
