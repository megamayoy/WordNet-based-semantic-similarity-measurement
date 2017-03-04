using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WordNetEngine;
using WordNet_Noun_Semantics.Properties;

namespace WordNet_Noun_Semantics
{
    public partial class MainForm : Form
    {
        internal string SynsetFile { get; set; }
        internal string HypernymFile { get; set; }
        internal string CasesFile { get; set; }
        public string RqOutputFile { get; set; }
        public string RqCases { get; set; }
        public string OutCastOutputFile { get; set; }
        public string OutCastCases { get; set; }

        private WordNet _wordNet;
        private BackgroundWorker _bgWorker;
        private Stopwatch _workWatch;
        private string _status;
        private int _prog = 0;
        private List<long> _outCastAvgList = new List<long>();
        private List<long> _rqAvgList = new List<long>();
        private string _latestState = string.Empty;
        public MainForm()
        {
            InitializeComponent();

            splitContainer1.Panel1Collapsed = false;
            splitContainer1.Panel2Collapsed = true;
            _status = "Ready";
        }
        private void timeTimer_Tick(object sender, EventArgs e)
        {
            processStatusLbl.Text = _status;
            dtimeLbl.Text = DateTime.Now.ToShortTimeString();
        }
        
        #region Processing Graph Input
        private void SelectGraphFilesBtnClick(object sender, EventArgs e)
        {
            openFiledg.RestoreDirectory = true;
            openFiledg.Title = "Select Synsets File";
            var dgResult = openFiledg.ShowDialog();
            if (dgResult == DialogResult.OK)
            {
                SynsetFile = openFiledg.FileName;

                filePathTxtBox.Text = openFiledg.SafeFileName;
                openFiledg.Title = "Select Hypernym File";

                dgResult = openFiledg.ShowDialog();

                HypernymFile = openFiledg.FileName;
                if (dgResult == DialogResult.OK)
                {
                    hypTxt.Text = openFiledg.SafeFileName;
                    processInputBtn.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Please specify input files");
                    hypTxt.Text = string.Empty;
                    filePathTxtBox.Text = string.Empty;
                    casesFileTxtBox.Text = string.Empty;
                    SynsetFile = string.Empty;
                    HypernymFile = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("Please specify input files");
                hypTxt.Text = string.Empty;
                filePathTxtBox.Text = string.Empty;
                casesFileTxtBox.Text = string.Empty;
                SynsetFile = string.Empty;
                HypernymFile = string.Empty;
            }
        }
        private void ProcessInput(object sender, EventArgs e)
        {
            outputConsole.Clear();
            outputConsole.AppendText($"Please wait processing input... Start Time {DateTime.Now.ToShortTimeString()}");
            try
            {
                ProcessGraphInputFiles();
            }
            catch (Exception ex)
            {
                LogEvent("Error Occurred");
                LogEvent($"{ex.Message}");
            }
        }
        private void ProcessGraphInputFiles()
        {
            _bgWorker = new BackgroundWorker {WorkerReportsProgress = true};
            _bgWorker.DoWork += ProcessGraphInputFilesBgWorkerWork;
            _bgWorker.RunWorkerCompleted += ProcessGraphInputFilesBgWorkerCompleted;
            _bgWorker.ProgressChanged += ProcessGraphInputFilesBgWorkerProgressReport;
            proceedBtn.Enabled = false;
            selectFileBtn.Enabled = false;
            processInputBtn.Enabled = false;
            validateGraphCheckBox.Enabled = false;
            resetBtn.Enabled = false;
            statusProgressIcon.Image = Resources.RedCirlce;
            _bgWorker.RunWorkerAsync();
        }
        private void ProcessGraphInputFilesBgWorkerProgressReport(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            toolStripProgressBar1.Value = progressChangedEventArgs.ProgressPercentage;
            var stateObject = (object[]) progressChangedEventArgs.UserState;
            _status = $"Processing Input {(int) stateObject[0]} / {(int) stateObject[1]}";
        }
        private void ProcessGraphInputFilesBgWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            LogEvent($"Graph Process time:{_workWatch.ElapsedMilliseconds} MilliSeconds.");
            _status = "Ready";
            toolStripProgressBar1.Value = 0;
            if (validateGraphCheckBox.Checked)
            {
                LogEvent($"Validating Graph is Rooted.");
                var worker = new BackgroundWorker();
                worker.DoWork += (o, args) =>
                {
                    try
                    {
                        _status = "Validating Input";
                        _wordNet.InitWordNet();
                    }
                    catch (Exception ex)
                    {
                        LogEvent("Error Ocurred:\n" + ex.Message);
                    }
                };
                worker.RunWorkerCompleted += (o, args) =>
                {
                    LogEvent("Input Validation Succeed.");
                    _status = "Ready";
                    proceedBtn.Enabled = true;
                    inputSizeLabel.Text = $"{_wordNet.GetInputSize().ToString()} Noun";
                    speedInputLabel.Text = $"{_workWatch.ElapsedMilliseconds/1000} S";
                    statusProgressIcon.Image = Resources.GreenCircle;
                };
                statusProgressIcon.Image = Resources.RedCirlce;
                worker.RunWorkerAsync();
            }
            else
            {
                proceedBtn.Enabled = true;
                _wordNet.InitWordNet(false);
                inputSizeLabel.Text = $"{_wordNet.GetInputSize()} N";
                speedInputLabel.Text = $"{_workWatch.ElapsedMilliseconds} S";
                _status = "Ready";
                LogEvent("=======");
                LogEvent("Ready.");
                statusProgressIcon.Image = Resources.GreenCircle;
            }
        }
        private void ProcessGraphInputFilesBgWorkerWork(object sender, DoWorkEventArgs e)
        {
            _workWatch = Stopwatch.StartNew();
            _wordNet = new WordNet(HypernymFile, SynsetFile, ProcessGraphInputFilesWordNetOnProgressGraphReport);
            _workWatch.Stop();
        }
        private void ProcessGraphInputFilesWordNetOnProgressGraphReport(int arg1, int arg2)
        {
            _prog = arg1*100/(arg2 - 1);
            _bgWorker.ReportProgress(_prog, new object[] {arg1, arg2});
        }
        private void ResetInputBtnClick(object sender, EventArgs e)
        {
            HypernymFile = SynsetFile = string.Empty;
            selectFileBtn.Enabled = true;
            processInputBtn.Enabled = false;
        }

        #endregion

        #region Utils
        private void PlotAvg(List<long> avgList )
        {
            SuspendLayout();

            var dChart = dataChart.Series.FindByName("Avg");

            if (dChart == null)
            {
                dataChart.Series.Add("Avg");
                dataChart.Series["Avg"].ChartType = SeriesChartType.FastLine;
            }

            var avg = avgList.Sum() / avgList.Count;
            dataChart.Series["Avg"].Points.Clear();
            for (int i = 0; i < avgList.Count; i++)
            {
                dataChart.Series["Avg"].Points.AddXY(i, avg);
            }
            ResumeLayout();

            dataChart.Refresh();
        }
        private void GraphDraw(Chart graph, int xVal, long yVal, string name, SeriesChartType chartType)
        {
            try
            {
                graph.Series[name].Points.AddXY(xVal, yVal);
            }
            catch
            {
                graph.Series.Add(name);
                graph.Series[name].Points.AddXY(xVal, yVal);
                graph.Series[name].ChartType = chartType;
            }
        }
        private void LogEvent(string s)
        {
            outputConsole.AppendText("\n" + s);
        }

        #endregion

        private void ProceedToTestBtnClick(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
            splitContainer1.Panel2Collapsed = false;
        }

        private void NewInputMenuItemClick(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = false;
            splitContainer1.Panel2Collapsed = true;
            proceedBtn.Enabled = false;
            selectFileBtn.Enabled = true;
            validateGraphCheckBox.Enabled = true;
        }

        #region Relational Queries
        private void SelectRqInputFileCasesBtnClick(object sender, EventArgs e)
        {
            openFiledg.RestoreDirectory = true;
            openFiledg.Title = "Select RQ Cases";
            if (openFiledg.ShowDialog() == DialogResult.OK)
            {
                RqCases = openFiledg.FileName;
                saveFileDialog1.Title = "Output File";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    RqOutputFile = saveFileDialog1.FileName;
                    rqCasesTxtBox.Text = openFiledg.SafeFileName;
                    rqTestRunBtn.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Please specify output file");
                    RqOutputFile = string.Empty;
                    RqCases = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("Please specify input file");
                RqOutputFile = string.Empty;
                RqCases = string.Empty;
            }
        }

        private void RunRelationQueryTestBtnClick(object sender, EventArgs e)
        {
           ProcessRelationalQueries();
        }

        private void ProcessRelationalQueries()
        {
            _bgWorker = new BackgroundWorker {WorkerReportsProgress = true};
            _bgWorker.DoWork += RelationalQueriesBgWorkerWork;
            _bgWorker.ProgressChanged += RelationalQueriesBgWorkerWorkProgress;
            _bgWorker.RunWorkerCompleted += RelationalQueriesBgWorkerWorkCompleted;
            statusProgressIcon.Image = Resources.RedCirlce;
            dataChart.Series.Clear();
            _bgWorker.RunWorkerAsync();
        }

        private void RelationalQueriesBgWorkerWorkCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            _status = "Ready";
            toolStripProgressBar1.Value = 0;
            _latestState = "rq";
            if (MessageBox.Show("Processed Succesfully, Open File For Comparison", "Done", MessageBoxButtons.YesNo) ==
                DialogResult.Yes)
            {
                Process.Start("notepad++", RqOutputFile);
            }
            statusProgressIcon.Image = Resources.GreenCircle;
        }

        private void RelationalQueriesBgWorkerWorkProgress(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            toolStripProgressBar1.Value = progressChangedEventArgs.ProgressPercentage;

            var stateObject = (object[]) progressChangedEventArgs.UserState;

            GraphDraw(dataChart, Convert.ToInt32(stateObject[0]), (long) stateObject[1], "SCA", SeriesChartType.Area);

            _status = (string) stateObject[2];

            LogEvent($"Processed Input {Convert.ToInt32(stateObject[0])} at {(long) stateObject[1]}");
        }

        private void RelationalQueriesBgWorkerWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            using (var writer = new StreamWriter(RqOutputFile))
            {
                using (var reader = new StreamReader(RqCases))
                {
                    var inputLines = reader.ReadToEnd();
                    var data = Regex.Split(inputLines, "\r\n|\r|\n");
                    var tct = data.Length;
                    int ct = 0;
                    for (int i = 0; i < data.Length; i++)
                    {
                        ct++;
                        var queries = data[i].Split(',');
                        var watch = Stopwatch.StartNew();
                        HashSet<string> scaSet = new HashSet<string>();
                        var length = _wordNet.GetSca(queries[0], queries[1], out scaSet);
                        watch.Stop();

                        {
                            var writeLine = $"{length},";
                            foreach (var sc in scaSet)
                            {
                                writeLine += $"{sc} ";
                            }
                            writeLine = writeLine.Remove(writeLine.LastIndexOf(" ", StringComparison.Ordinal), 1);
                            writer.WriteLine(writeLine);
                        }
                        _rqAvgList.Add(watch.ElapsedMilliseconds);
                        _bgWorker.ReportProgress(ct*100/(tct),
                            new object[] {ct, watch.ElapsedMilliseconds, $"Working..{ct} / {tct}"});
                    }
                }
                writer.Flush();
            }
        }
        private void RelationalQueryUseFileInputCheckChange(object sender, EventArgs e)
        {
            rqCasesTxtBox.Enabled = useRqCasesFileInputCheckBox.Checked;
            rqFileSelectBtn.Enabled = useRqCasesFileInputCheckBox.Checked;
            rqInputTxtBox.Enabled = !useRqCasesFileInputCheckBox.Checked;
        }

        private void scaInputTxtBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                ProcessScaLine();
            }
        }

        private void ProcessScaLine()
        {
            var inputLine = rqInputTxtBox.Lines[rqInputTxtBox.Lines.Length - 1];
            var query = inputLine?.Split(' ');
            var p = 0;
            var sca = _wordNet.GetSca(int.Parse(query[0]), int.Parse(query[1]), out p);
            LogEvent($"SCA -> {sca}, at path length = {p}.");
        }

        #endregion

        #region OutCasting Noun

        private void SelectOutCastCasesFileInputBtnClick(object sender, EventArgs e)
        {
            openFiledg.RestoreDirectory = true;
            openFiledg.Title = "Select OutCast Cases";
            if (openFiledg.ShowDialog() == DialogResult.OK)
            {
                OutCastCases = openFiledg.FileName;
                saveFileDialog1.Title = "Output File";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    OutCastOutputFile = saveFileDialog1.FileName;
                    casesFileTxtBox.Text = openFiledg.SafeFileName;
                    runOutCastBtn.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Please specify output file");
                    OutCastOutputFile = string.Empty;
                    OutCastCases = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("Please specify input file");
                OutCastOutputFile = string.Empty;
                OutCastCases = string.Empty;
            }
        }
        private void ProcessOutCastFromFileInput(object sender, EventArgs e)
        {
            _bgWorker = new BackgroundWorker {WorkerReportsProgress = true};
            _bgWorker.DoWork += OutCastQueriesBgWorkerWork;
            _bgWorker.ProgressChanged += OutCastQueriesBgWorkerProgress;
            _bgWorker.RunWorkerCompleted += OutCastQueriesBgWorkerWorkCompleted;
            statusProgressIcon.Image = Resources.RedCirlce;
            dataChart.Series.Clear();
            _bgWorker.RunWorkerAsync();
        }
        private void OutCastQueriesBgWorkerWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Value = 0;
            if (MessageBox.Show("Processed Succesfully, Open File For Comparison", "Done", MessageBoxButtons.YesNo) ==
                DialogResult.Yes)
            {
                Process.Start("notepad++", OutCastOutputFile);
            }
            statusProgressIcon.Image = Resources.GreenCircle;
            _status = "Ready";
            _latestState = "ocq";
            //PlotAvg(_outCastAvgList);
        }
        private void OutCastQueriesBgWorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
            var stateObject = (object[]) e.UserState;
            GraphDraw(dataChart, Convert.ToInt32(stateObject[0]), (long) stateObject[1], "OutCast", SeriesChartType.Area);
            LogEvent($"OutCast Result {Convert.ToString(stateObject[3])}, Time Elapsed:{(long) stateObject[1]} Ms.");
            _status = (string) stateObject[2];
        }
        private void OutCastQueriesBgWorkerWork(object sender, DoWorkEventArgs e)
        {
            _outCastAvgList.Clear();
            using (var writer = new StreamWriter(OutCastOutputFile))
            {
                using (var reader = new StreamReader(OutCastCases))
                {
                    var ct = 0;
                    var input = reader.ReadToEnd();
                    var inputLines = Regex.Split(input, "\r\n|\r|\n");
                    var totalInput = inputLines.Length;
                    for (int i = 1; i < totalInput; i++)
                    {
                        var nouns = inputLines[i].Split(',');
                        ct++;
                        try
                        {
                            var watch = Stopwatch.StartNew();
                            var outCastedNoun = _wordNet.OutCastNoun(nouns.ToList(), NounCastedProgressReport,
                                "OutCast-Noun");
                            watch.Stop();
                            _bgWorker.ReportProgress(ct*100/(totalInput - 1),
                                new object[]
                                {ct, watch.ElapsedMilliseconds, $"Working..{ct} / {totalInput}", outCastedNoun});

                            _outCastAvgList.Add(watch.ElapsedMilliseconds);

                            writer.WriteLine(outCastedNoun);
                        }
                        catch (Exception ex)
                        {
                            LogEvent($"Error ocurred\n{ex.Message},\n{ex.StackTrace}.");
                        }
                    }
                }
            }
        }
        private void NounCastedProgressReport(string arg1, int arg2, long arg3)
        {
        }

        private void OutCastNounUseFileInputCheckChange(object sender, EventArgs e)
        {
            outCastTxtBox.Enabled = !useOutCastFileCheckBox.Checked;
            casesSelectFileBtn.Enabled = useOutCastFileCheckBox.Checked;
            casesFileTxtBox.Enabled = useOutCastFileCheckBox.Checked;
        }

        private void outCastTxtBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    var input = outCastTxtBox.Lines[outCastTxtBox.Lines.Length - 1].Split(' ');
                    var watch = Stopwatch.StartNew();
                    var outCastedNoun = _wordNet.OutCastNoun(input.ToList());
                    watch.Stop();
                    LogEvent($"Noun Outcasted result: {outCastedNoun},Time: {watch.ElapsedMilliseconds}.");
                }
                catch (Exception ex)
                {
                    LogEvent($"Error ocurred\n{ex.Message},\n{ex.StackTrace}.");
                }
            }
        }
        #endregion
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            outputConsole.Clear();
            outputConsole.AppendText("Console cleared");
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (_latestState == "rq")
            {
                PlotAvg(_rqAvgList);
            }
            else if (_latestState == "ocq")
            {
                PlotAvg(_outCastAvgList);
            }
        }

        private void rqInputTxtBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
