using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using System.Windows.Input;
using Microsoft.Win32;
using OriginGPSUtil.NMEA;
using OriginGPSUtil.NMEA.Sentences;
using System.IO;
using System.Diagnostics;
using OxyPlot;
//using OxyPlot.Wpf;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Reflection;
using System.Deployment.Application;
using System.Windows;
//using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Printing;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;



namespace NMEAGraphs
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //public string Title { get { return $"OriginGPS NMEA Log Analyzer {GetVersion()}"; } }
        // currently give up the version function and update version manually   

        public string Title { get { return $"OriginGPS NMEA Log Analyzer  2.2.  R&D tool for internal use only"; } }

        public MainViewModel()
        {
            BrowseCommand = CreateBrowseCommand();
            PlotCommand = CreatePlotCommand();
            SaveGraphCommand = CreateSaveGraphCommand();
        }
        
        private PlotModel _graph;
        public PlotModel Graph
        {
            get { return _graph; }
            set
            {
                _graph = value;
                OnPropertyChanged();
            }
        }

        private static readonly char LogSourceSplitter = ';';
        private string _logSources;
        public string LogSources
        {
            get { return _logSources; }
            set
            {
                _logSources = value;
                OnPropertyChanged();
            }
        }

        private bool _showGPS = true;
        public bool ShowGPS
        {
            get { return _showGPS; }
            set
            {
                _showGPS = value;
                OnPropertyChanged();
            }
        }

        private bool _showGLONASS = true;
        public bool ShowGLONASS
        {
            get { return _showGLONASS; }
            set
            {
                _showGLONASS = value;
                OnPropertyChanged();
            }
        }

        private bool _showGalileo;
        public bool ShowGalileo
        {
            get { return _showGalileo; }
            set
            {
                _showGalileo = value;
                OnPropertyChanged();
            }
        }

        private bool _showBeidou;
        public bool ShowBeidou
        {
            get { return _showBeidou; }
            set
            {
                _showBeidou = value;
                OnPropertyChanged();
            }
        }

        private bool _showQZSS;
        public bool ShowQZSS
        {
            get { return _showQZSS; }
            set
            {
                _showQZSS = value;
                OnPropertyChanged();
            }
        }

        private bool _showGNSS;
        public bool ShowGNSS
        {
            get { return _showGNSS; }
            set
            {
                _showGNSS = value;
                OnPropertyChanged();
            }
        }

        private string _stats;
        public string Stats
        {
            get { return _stats; }
            set { _stats = value; OnPropertyChanged(); }
        }

        private string _events;
        public string Events
        {
            get { return _events; }
            set { _events = value;  OnPropertyChanged(); }
        }

        public ICommand BrowseCommand { get; }
        public ICommand PlotCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveGraphCommand { get; }


        // This object holds the data we're supposed to display in the graph 
        // with all the relevant metadata, so if we want to just show different plots
        // for the same log files, we won't have to parse them all over again.
        private ValuesArraysObject _data { get; set; } = new ValuesArraysObject();

        int _missedSentences;

        private void ResetPlotModel()
        {
            Graph = new PlotModel
            {
                Title = "Module Performance Analysis",
                Subtitle = "Mean values of 4 best satellites per module per constellation\n"
            };

            // Create X Axis
            Graph.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Sample Time",
                StringFormat = "HH:mm:ss"
            });

            // Create Y Axis
            Graph.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Mean C/N0 Value (dB-Hz)"
            });
        }


        private ICommand CreateBrowseCommand()
        {

            return new RelayCommand<object>((o) =>
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All Files (*.*)|*.*";
                dlg.Multiselect = true;
                dlg.Title = "Select NMEA logs for display";

                CreateEventsList();

                if ((bool)dlg.ShowDialog())
                {
                    LogSources = string.Join(LogSourceSplitter.ToString(), dlg.FileNames);
                }
            });
        }


        /* this is ThemeDictionaryExtension button which is saving the graph to .jpg file   */
        private ICommand CreateSaveGraphCommand()
        {
           
            return new RelayCommand<object>((o) =>
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = "pdf";
                dlg.Filter = "PDF Files|*.pdf";
                Nullable<bool> res = dlg.ShowDialog();
                if (res == true)
                {

                    PdfDocument pdf = new PdfDocument();
                    pdf.Info.Title = "NMEA Analyzer Graph";
                    PdfPage pdfPage1 = pdf.AddPage();
                    PdfPage pdfPage2 = pdf.AddPage();
                    pdfPage1.Orientation = PdfSharp.PageOrientation.Landscape;
                    XGraphics page1 = XGraphics.FromPdfPage(pdfPage1);

                    XFont main_font = new XFont("Verdana", 30, XFontStyle.Bold);
                    XFont secondary_font = new XFont("Verdana", 9);
               //     XFont stats_font = new XFont("Verdana", 6);             // for the statistics 
                    XFont watermark_font = new XFont("Verdana", 40);

                    page1.DrawString("\nNMEA Log Analyzer", main_font, XBrushes.Blue, new XRect(0, 25, pdfPage1.Width.Point, pdfPage1.Height.Point), XStringFormats.TopCenter);
                    page1.DrawString("\nOriginGPS Proprietry tool, for internal use only\n", secondary_font, XBrushes.Blue, new XRect(0, 5, pdfPage1.Width.Point, pdfPage1.Height.Point), XStringFormats.TopCenter);

                    using (var stream = new MemoryStream())
                    {
                        Graph.LegendFontSize = 10;
                        Graph.LegendPlacement = OxyPlot.LegendPlacement.Outside;
                        Graph.LegendPosition = OxyPlot.LegendPosition.TopRight;
                        OxyPlot.Wpf.PngExporter.Export(Graph, stream, 1500, 940, OxyPlot.OxyColor.FromRgb(255,255, 255), 120);
                        XImage graph_image = Image.FromStream(stream);

                        page1.DrawImage(graph_image, 5, 90, 750, 470);
                     
                    }
                    page1.RotateAtTransform(45, new XPoint(pdfPage1.Width.Point / 2, 200));
                    page1.DrawString("OriginGPS Proprietry tool", watermark_font, XBrushes.LightGray, new XRect(70, 200, pdfPage1.Width.Point, 400), XStringFormats.Center);
                    XGraphics page2 = XGraphics.FromPdfPage(pdfPage2);

                    page2.DrawString("\nNMEA Log Analyzer", main_font, XBrushes.Blue, new XRect(0, 25, pdfPage2.Width.Point, pdfPage2.Height.Point), XStringFormats.TopCenter);
                    page2.DrawString("\nOriginGPS Proprietry tool, for internal use only\n", secondary_font, XBrushes.Blue, new XRect(0, 5, pdfPage2.Width.Point, pdfPage2.Height.Point), XStringFormats.TopCenter);
                    page2.DrawString("Statistics", main_font, XBrushes.Black, new XRect(40, 90, pdfPage2.Width.Point, pdfPage2.Height.Point), XStringFormats.TopLeft);

                    PdfSharp.Drawing.Layout.XTextFormatter tf = new PdfSharp.Drawing.Layout.XTextFormatter(page2);
                    tf.DrawString(Stats, secondary_font, XBrushes.Black, new XRect(40, (double)140, pdfPage2.Width.Point, pdfPage2.Height.Point), XStringFormats.TopLeft);

                    
                    string pdfFilename = dlg.FileName;
                    pdf.Save(pdfFilename);
                    Process.Start(pdfFilename);
                    Graph.LegendFontSize = 12;
                    Graph.LegendPlacement = OxyPlot.LegendPlacement.Inside;


                }
            });
        }


        private IEnumerable<Talker> GetSelectedTalkers()
        {
            if (ShowGPS) yield return Talker.GPS;
            if (ShowGLONASS) yield return Talker.GLONASS;
            if (ShowGalileo) yield return Talker.Galileo;
            if (ShowBeidou) yield return Talker.BeiDou;
            if (ShowBeidou) yield return Talker.BeiDou2;
            if (ShowQZSS) yield return Talker.QZSS;
            if (ShowGNSS) yield return Talker.GNSS;
        }

        private void CreateValueArrays()
        {
            //Cursors
            //  (System.Windows.Input.CursorType.Wait;
            Mouse.OverrideCursor = Cursors.Wait;

                        if (null == _data)
            {
                /* in case there was a previous attempt of making plot with a non-text file */
            }
            // Skip reading the log files if we have already read those ones.
            else if (_data.LogSources == LogSources)
                return;

            var sw = new Stopwatch();

            string[] logFiles = LogSources.Split(LogSourceSplitter);
            var sentencesArrays = new GSVWithTimeAndHDOP[logFiles.Length][];

            sw.Start();
            // Parse each line of each log file and add it to the appropriate list 
            // of GSV Sentences if we're supposed to show it.

            /* a check if all files are text files  */
            for (int indx = 0; indx < logFiles.Length; ++indx)
            {
                System.IO.StreamReader objReader;
                objReader = new System.IO.StreamReader(logFiles[indx]);
                int temp_ch = 'a'; 
                for (int counter = 0; counter < 1000; ++counter)
                {
                    if (objReader.Read() == -1)
                    {
                        break; 
                    }
                    temp_ch = objReader.Read();
               //     MessageBox.Show(temp_ch.ToString());
                      if  (temp_ch > 126)
                       {
                           MessageBox.Show("Error - you entered not a text file.\nPlease Insert a valid NMEA log.");
                         _data = null;
                           return;
                       } 
                   }
            }



            Parallel.For(0, logFiles.Length, (i) =>
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(logFiles[i]);
                }
                catch
                {
                    MessageBox.Show("Couldn't read file " + logFiles[i]);
                    throw new Exception("Couldn't read file " + logFiles[i]);
                }

                sentencesArrays[i] = ParseFile(lines, i).ToArray();
            });
            sw.Stop();
            Debug.WriteLine($"Reading the files and parsing them took {sw.Elapsed.TotalSeconds} seconds.");

            // Check if there are any empty files.
            for(int i = 0; i < logFiles.Length; i++)
            {
                if(sentencesArrays[i] == null || sentencesArrays[i].Length < 8 )
                {
                    MessageBox.Show($"Couldn't find enough NMEA sentences in {logFiles[i]}. Please try again without this file.", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    _data = null;
                    return;
                }
            }

            sw.Restart();
            // Go over all the timestamps in the logs to figure out when to start and when to end.
            DateTime startTime = DateTime.MinValue, endTime = DateTime.MaxValue;
            foreach (var sentenceArray in sentencesArrays)
            {
                startTime = MaxDateTime(startTime, sentenceArray[0].Time);
                endTime = MinDateTime(endTime, sentenceArray[sentenceArray.Length - 1].Time);
            }
            sw.Stop();
            Debug.WriteLine($"Figuring out the start and end times took {sw.Elapsed.TotalSeconds} seconds.");

            // Todo: Add check to see if any of the files have no samples in range. 

            sw.Restart();
            // Assemble arrays of mean C/N0 values
            var valueArrays = new Dictionary<string, MeanTalkerHDOPInViewTime[]>();
            Parallel.For(0, logFiles.Length, (i) =>
            {
                var array = GetMeanTalkerTimesForSentences(sentencesArrays[i], startTime, endTime).ToArray();
                valueArrays.Add(Path.GetFileNameWithoutExtension(logFiles[i]), array);
            });
            sw.Stop();
            Debug.WriteLine($"Assembling the mean values took {sw.Elapsed.TotalSeconds} seconds.");

            _data = new ValuesArraysObject
            {
                LogSources = LogSources,
                ValueArrays = valueArrays,
                StartTime = startTime,
                EndTime = endTime
            };

            sw.Restart();
            CleanupValues(_data.ValueArrays);
            sw.Stop();
            Debug.WriteLine($"Cleanup took {sw.Elapsed.TotalSeconds} seconds.");
        }

        // This method removes unwanted values from the general ValueArrays
        private void CleanupValues(Dictionary<string, MeanTalkerHDOPInViewTime[]> valueArrays)
        {
            // Attempt to remove dips in the graph
        }

        private ICommand CreatePlotCommand()
        {            
            return new RelayCommand<object>((o) =>
            {
                var sw = new Stopwatch();

                sw.Start();
                // Parse LogSources and create arrays of Mean Values
                CreateValueArrays();
                sw.Stop();
                Debug.WriteLine($"Parsing the files and building the initial dataset took {sw.Elapsed.TotalSeconds} seconds.");

                // The data couldn't be loaded, no graph can be drawn.
                if (_data == null)
                {
                    ResetPlotModel();

                    return;
                }
                sw.Restart();
                // This resets the PlotModel but doesn't change assign the max number 
                // of samples shown, so we have to set that after we figure it out.
                ResetPlotModel();

                // Go over all the arrays of values and create a LineSeries
                // for each talker for each constellation
                int minSampleCount = int.MaxValue;
                foreach (var arrayNamePair in _data.ValueArrays)
                {
                    foreach (var talker in GetSelectedTalkers())
                    {
                        var line = new LineSeries { Title = $"{arrayNamePair.Key} - {Enum.GetName(typeof(Talker), talker)}", Smooth = false };

                        // This fixes the "Sample Time: 0.#####" when you click on the graph.
                        line.TrackerFormatString = line.TrackerFormatString.Replace("{2:0.###}", "{2:hh:mm:ss}");

                        int sampleCount = 0;
                        foreach (var mtt in arrayNamePair.Value.Where(mtt => mtt.Talker == talker && mtt.MeanValue != -1))
                        {
                            line.Points.Add(new DataPoint(DateTimeAxis.ToDouble(mtt.Time), mtt.MeanValue));
                        }
                        minSampleCount = Math.Min(sampleCount, minSampleCount);

                        Graph.Series.Add(line);
                    }
                }

                /*// Now once we have the minimal sample count, we'll set it as the 
                // maximum value of the X axis:
                Graph.Axes.Where(x => x.Position == AxisPosition.Bottom).Single()
                    .Maximum = minSampleCount + 5; // Show +5 samples*/

                // Change the min max values of the Y axis to be +-3 of the min max values.
                // This should be a part of ResetPlotModel and not here. 

                int newMax = 0;
                int newMin = 0;
                try
                {
                    newMax = (int)_data.ValueArrays.Max(array => array.Value.Max(data => data.MeanValue)) + 3;
                    newMin = (int)_data.ValueArrays.Min(array => array.Value.Min(data => data.MeanValue)) - 3;
                }
                catch (InvalidOperationException e)
                {
                    newMax = 50;
                    newMin = 0;
                }

                var axis = Graph.Axes.Where(x => x.Position == AxisPosition.Left).Single();
                axis.Maximum = newMax;
                axis.Minimum = newMin;

                // Notify the graph that we have new data to dislpay.
                Graph.InvalidatePlot(true);

                // Recreate the stats
                CalculateStats();


                sw.Stop();
                Debug.WriteLine($"Building the graph took {sw.Elapsed.TotalSeconds} seconds.");

                Mouse.OverrideCursor = Cursors.Arrow;

            }, (o) =>
            {
                return !string.IsNullOrWhiteSpace(LogSources);
            });
        }

        /*private void CreateSaveCommand()
        {
            //return !string.IsNullOrWhiteSpace(LogSources);
        }
        */

        private double GetSecondTimeDifference(DateTime start, DateTime end)
        {
            return (end - start).TotalSeconds;
        }

        private void CalculateStats()
        {
            StringBuilder sb = new StringBuilder();

            // Time frame
            sb.Append($"Displayed Time Frame: {_data.StartTime} - {_data.EndTime}.").Append(Environment.NewLine);
            sb.Append($"Total Duration: {_data.EndTime - _data.StartTime} hours.").Append(Environment.NewLine);
            sb.Append(Environment.NewLine);

            // C/N0 Average
            sb.Append("Average of 4 Best SVs: ").Append(Environment.NewLine);
            var talkers = GetSelectedTalkers();
            int longestTalker = talkers.Max(talker => talker.ToString().Length);
            int longestLog = _data.ValueArrays.Keys.Max(log => log.Length);
            foreach (var talker in talkers)
            {
                foreach (var log in _data.ValueArrays.Keys)
                {
                    var values = _data.ValueArrays[log].Where(data => data.Talker == talker);
                    if (values.Count() < 1)
                        continue;

                    var cn0 = values.Average(data => data.MeanValue);
                    var inView = values.Average(data => data.InView);

                    //sb.Append(string.Format($"{{0,{longestTalker - talker.ToString().Length}}}: {{1,{longestLog - log.Length}}}: {cn0:0.00} dB, Satellites In View: {inView:0.00}", talker, log))
                    sb.Append($"{talker}: {log}: C/N0 {cn0:0.00} dB, Average number of Satellites In View: {inView:0.00}")
                        .Append(Environment.NewLine);
                }
            }
            sb.Append(Environment.NewLine);

            // HDOP Average - Todo: do better.
            sb.Append("Average HDOP Values: ")
                .Append(Environment.NewLine);
            foreach (var log in _data.ValueArrays.Keys)
            {
                try
                {
                    var average = _data.ValueArrays[log].Average(data => data.HDOP);
                    sb.Append($"{log}: {average}").Append(Environment.NewLine);
                }
                catch (InvalidOperationException e)
                {
                    continue;
                }
            }
            sb.Append(Environment.NewLine);

            //sb.Append($"Missing: {_missedSentences}");canx 

            Stats = sb.ToString();
        }

        private void CreateEventsList()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Events During The Test:\n (Time of events has been taken from terminal time and not from the module. Might be differences between the two.)\n\n");

            OnPropertyChanged();
            Events = sb.ToString();
        }

        private IEnumerable<MeanTalkerHDOPInViewTime> GetMeanTalkerTimesForSentences(GSVWithTimeAndHDOP[] sentences, DateTime startTime, DateTime endTime)
        {
            var times = sentences.Select(s => s.Time).Distinct().Where(t => IsDateTimeInRange(t, startTime, endTime));

            foreach (var time in times)
            {
                // We're looping over all supported talkers to have them loaded just in case
                // we'll want to display other tlakers in the same log files later. (when we're
                // asked to display plots for the same log files as previously, we won't reload them.)
                var talkers = new Talker[] { Talker.GPS, Talker.GLONASS, Talker.Galileo, Talker.BeiDou, Talker.BeiDou2, Talker.QZSS, Talker.GNSS };
                foreach (var talker in talkers)
                {
                    var sentencesWithTimeAndTalker = from sentence in sentences
                                                     where sentence.Time == time
                                                     where sentence.Sentence.Talker == talker
                                                     select sentence;
                    if (sentencesWithTimeAndTalker.Count() < 1)
                        continue;


                    /*          var values = from sentence in sentencesWithTimeAndTalker                                      Original code
                                           from info in sentence.Sentence.SatteliteInformation
                                           where info.CN0 != null
                                           select info.CN0.Value;
                                           */

                    List<int> values = new List<int>(0);

                    foreach (var sentence in sentencesWithTimeAndTalker)
                        foreach (var info in sentence.Sentence.SatteliteInformation)
                            if (info.CN0 == null)
                                values.Add(0);
                            else
                                values.Add((int)info.CN0);
              
                    // We have multiple GSV sentences in every output (GGA,GSV,GSV,GSV,RMC), so we have 
                    // multiple GSVs but they all have the same HDOP, so it doesn't matter what we take.
                    float hdop = sentencesWithTimeAndTalker.First().HDOP;

                    // Same goes for number of satellites in view
                    int inView = sentencesWithTimeAndTalker.First().Sentence.SattelitesInView;

                    yield return new MeanTalkerHDOPInViewTime
                    {
                        MeanValue = CalculateMeanValue(values),
                        Talker = talker,
                        Time = time,
                        HDOP = hdop,
                        InView = inView
                    };
                }
            }
        }

        private double CalculateMeanValue(IEnumerable<int> values)
        {
            int elements = values.Count();
            if (elements == 0)
                return -1;
            if (elements < 4)
            {
                return values .Average();
            }
            return values.OrderByDescending(n => n)
                .Take(Math.Min(elements, 4))
                .Average();
        }

        private bool IsDateTimeInRange(DateTime value, DateTime start, DateTime end)
        {
            return value >= start && value <= end;
        }

        private DateTime MinDateTime(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        private DateTime MaxDateTime(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        private IEnumerable<GSVWithTimeAndHDOP> ParseFile(string[] lines, int i)            // i is the .txt file index. 
        {
            int cold_start_counter = 0;
            int hot_start_counter = 0;
            int warm_start_counter = 0;
            int hardware_reset_counter = 0;
            bool soft_start_flag = false;            // distinguish between hard reset and soft reset 
            
            _missedSentences = 0;
            DateTime time = DateTime.MaxValue;
            /**/    DateTime last_time = DateTime.MinValue;
            float hdop = float.PositiveInfinity;
            bool GGA_Valid = true;                      /* flag that indicates if there is a fix, from GGA message  */
            FixQuality fix_quality = 0;
            int down_counter = 0;
            int talker_counter = 0;
            bool glonass = false;
            GSVSentence falseGSVsentenceGP = NMEASentence.ParseGSV("$GPGSV,1,1,4,28,83,205,00,30,56,024,00,05,39,262,00,07,34,059,00*7E");
            GSVSentence falseGSVsentenceGL = NMEASentence.ParseGSV("$GLGSV,1,1,4,28,83,205,00,30,56,024,00,05,39,262,00,07,34,059,00*7E");
            string[] logFiles = LogSources.Split(LogSourceSplitter);

            foreach (var line in lines)
            {
                if (line.Contains("GGA"))
                {
                    try
                    {
                        var sentence = NMEASentence.ParseGGA(line);
                        time = sentence.Time;
                        fix_quality = sentence.FixQuality;
                        GGA_Valid = true;
                        last_time = time;
                        hdop = sentence.HDOP;
                    }

                    catch (Exception ex)
                    {
                        GGA_Valid = false;
                        ++down_counter;

                        if (last_time != DateTime.MinValue)
                        {
                            last_time = last_time.AddSeconds(1);
                            time = last_time;
                        }   
                        
                        _missedSentences++;
                        Debug.WriteLine("Failed parsing line " + line + " because " + ex.Message);
                    }
                }
                else if (line.Contains("GSV"))
                {
                    if (!glonass && (line.Contains("GLGSV")))
                        glonass = true;

                    if ((time == DateTime.MaxValue) || (false == GGA_Valid)) // We don't have a time for that sentence, might as well skip it
                        continue;                                             // or there is no fix

                    GSVSentence sentence;
                    try
                    {
                        sentence = NMEASentence.ParseGSV(line);
                    }
                    catch (Exception ex)
                    {
                        _missedSentences++;
                       // MessageBox.Show(ex.Message);
                        Debug.WriteLine("Failed parsing line " + line + " because " + ex.Message);
                        continue;
                    }
                    
                        yield return new GSVWithTimeAndHDOP { Sentence = sentence, Time = time, HDOP = hdop };
                    
                    
                }


                /* This section deals with a case of no fix. If there is no fix - the SNR of the graph is 0. 
                    This sections handles GPS and GLONASS only.
                    */
              
                if ((line.Contains("GGA")) && (false == GGA_Valid ) &&  (last_time != DateTime.MinValue))
                {
                    /* MessageBox.Show(time.ToString());
                     MessageBox.Show(last_time.ToString());*/
                    ++talker_counter;
                    if ((talker_counter % 2) == 1)
                    {
                        yield return new GSVWithTimeAndHDOP { Sentence = falseGSVsentenceGP, Time = time, HDOP = 1 };
                    }
                    if (glonass && (talker_counter % 2) == 0)
                    {
                        time = time.AddSeconds(-1);
                        yield return new GSVWithTimeAndHDOP { Sentence = falseGSVsentenceGL, Time = time, HDOP = 1 };
                    }

                }

                if (line.Contains("PSRF"))
                {

                    int indx = logFiles[i].ToString().LastIndexOf('\\') + 1;
                    int lngt = logFiles[i].ToString().LastIndexOf('.');

                    Regex rgx = new Regex(@"\d{2}:\d{2}:\d{2}");
                    if (line.Contains("PSRF101"))
                    {
                        Match mat = rgx.Match(line);
                        soft_start_flag = true;

                        /* cold start   */
                        if (line.Contains(",4*"))
                        {
                            ++cold_start_counter;

                            Events = Events + logFiles[i].ToString().Substring(indx,lngt - indx)  + ":  " + cold_start_counter.ToString() + ".   " + mat.ToString() + " Cold Start\n";
                        }

                        /* hot start    */
                        if (line.Contains(",1*"))
                        {
                            ++hot_start_counter;
                            Events = Events + logFiles[i].ToString().Substring(indx, lngt - indx) + ":  " +  hot_start_counter.ToString() + ".   " + mat.ToString() + " Hot Start\n";
                        }

                        /* warm start   */
                        if (line.Contains(",2*") || line.Contains(",3*"))
                        {
                            ++warm_start_counter;
                            Events = Events + logFiles[i].ToString().Substring(indx, lngt - indx) + ":  " + warm_start_counter.ToString() + ".   " + mat.ToString() + " Warm Start\n";
                        }
                    }

                    /* hard reset   */
                    if (line.Contains("PSRF150,1"))
                    {
                        if (soft_start_flag == false)
                        {
                            ++hardware_reset_counter;
                            Events = Events + logFiles[i].ToString().Substring(indx, lngt - indx) + ":  " + hardware_reset_counter.ToString()+ ".   "+ time.ToString("HH:mm:ss") +  ".  " + " Reset\n";
                        }
                        soft_start_flag = false;
                    }
                }
            }
        }
    }




    class ValuesArraysObject
    {
        public Dictionary<string, MeanTalkerHDOPInViewTime[]> ValueArrays;
        public string LogSources;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    class MeanTalkerHDOPInViewTime
    {
        public int InView;
        public float HDOP;
        public double MeanValue;
        public Talker Talker;
        public DateTime Time;
    }

    class GSVWithTimeAndHDOP
    {
        public GSVSentence Sentence;
        public DateTime Time;
        public float HDOP;
    }
}
