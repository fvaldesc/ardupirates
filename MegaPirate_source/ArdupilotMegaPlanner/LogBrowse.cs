﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ZedGraph; // Graphs

namespace ArdupilotMega
{
    public partial class LogBrowse : Form
    {
        int m_iColumnCount = 0;
        DataTable m_dtCSV = new DataTable();

        PointPairList list1 = new PointPairList();
        PointPairList list2 = new PointPairList();
        PointPairList list3 = new PointPairList();
        PointPairList list4 = new PointPairList();
        PointPairList list5 = new PointPairList();

        int graphs = 0;

        public LogBrowse()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Log Files|*.log";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "logs";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                    PopulateDataTableFromUploadedFile(stream);
                    stream.Close();

                    dataGridView1.DataSource = m_dtCSV;

                }
                catch (Exception ex) { MessageBox.Show("Failed to read File: "+ex.ToString()); }

                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                CreateChart(zg1);

                int a = 1;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    //Commands.Rows[a].HeaderCell.Value
                    row.HeaderCell.Value = a.ToString();
                    a++;
                }
            }
            else
            {
                return;
            }
        }

        private void PopulateDataTableFromUploadedFile(System.IO.Stream strm)
        {
            System.IO.StreamReader srdr = new System.IO.StreamReader(strm);
            String strLine = String.Empty;
            Int32 iLineCount = 0;
            do
            {
                strLine = srdr.ReadLine();
                if (strLine == null)
                {
                    break;
                }
                if (0 == iLineCount++)
                {
                    m_dtCSV = new DataTable("CSVTable");
                    //m_dtCSV = this.CreateDataTableForCSVData(strLine);
                }
                this.AddDataRowToTable(strLine, m_dtCSV);
            } while (true);
        }

        private DataTable CreateDataTableForCSVData(String strLine)
        {
            DataTable dt = new DataTable("CSVTable");
            String[] strVals = strLine.Split(new char[] { ',',':' });
            m_iColumnCount = strVals.Length;
            int idx = 0;
            foreach (String strVal in strVals)
            {
                String strColumnName = String.Format("{0}", idx++);
                dt.Columns.Add(strColumnName, Type.GetType("System.String"));
            }
            return dt;
        }

        private DataRow AddDataRowToTable(String strCSVLine, DataTable dt)
        {
            String[] strVals = strCSVLine.Split(new char[] { ',',':' });
            Int32 iTotalNumberOfValues = strVals.Length;
            // If number of values in this line are more than the columns
            // currently in table, then we need to add more columns to table.
            if (iTotalNumberOfValues > m_iColumnCount)
            {
                Int32 iDiff = iTotalNumberOfValues - m_iColumnCount;
                for (Int32 i = 0; i < iDiff; i++)
                {
                    String strColumnName = String.Format("{0}", (m_iColumnCount + i));
                        dt.Columns.Add(strColumnName, Type.GetType("System.String"));
                }
                m_iColumnCount = iTotalNumberOfValues;
            }
            int idx = 0;
            DataRow drow = dt.NewRow();
            foreach (String strVal in strVals)
            {
                String strColumnName = String.Format("{0}", idx++);
                    drow[strColumnName] = strVal.Trim();
            }
            dt.Rows.Add(drow);
            return drow;
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    //    col.HeaderText = "";
                }
            }
            catch { }
            try
            {
                string option = dataGridView1[0, e.RowIndex].EditedFormattedValue.ToString();

                switch (option)
                {
                    case "GPS":
                        dataGridView1.Columns[1].HeaderText = "Time";
                        dataGridView1.Columns[2].HeaderText = "Fix";
                        dataGridView1.Columns[3].HeaderText = "Sats";
                        dataGridView1.Columns[4].HeaderText = "Lat";
                        dataGridView1.Columns[5].HeaderText = "Long";
                        dataGridView1.Columns[6].HeaderText = "Mix Alt";
                        dataGridView1.Columns[7].HeaderText = "GPSAlt";
                        dataGridView1.Columns[8].HeaderText = "GR Speed";
                        dataGridView1.Columns[9].HeaderText = "CRS";
                        break;
                    case "ATT":
                        dataGridView1.Columns[1].HeaderText = "Roll";
                        dataGridView1.Columns[2].HeaderText = "Pitch";
                        dataGridView1.Columns[3].HeaderText = "Yaw";
                        dataGridView1.Columns[4].HeaderText = "";
                        dataGridView1.Columns[5].HeaderText = "";
                        dataGridView1.Columns[6].HeaderText = "";
                        dataGridView1.Columns[7].HeaderText = "";
                        dataGridView1.Columns[8].HeaderText = "";
                        dataGridView1.Columns[9].HeaderText = "";
                        break;
                    case "NTUN":
                        if (MainV2.APMFirmware == MainV2.Firmwares.ArduPilotMega)
                        {
                            dataGridView1.Columns[1].HeaderText = "Yaw";
                            dataGridView1.Columns[2].HeaderText = "WP dist";
                            dataGridView1.Columns[3].HeaderText = "Target Bear";
                            dataGridView1.Columns[4].HeaderText = "Nav Bear";
                            dataGridView1.Columns[5].HeaderText = "Alt Err";
                            dataGridView1.Columns[6].HeaderText = "AS";
                            dataGridView1.Columns[7].HeaderText = "NavGScaler";
                            dataGridView1.Columns[8].HeaderText = "";
                            dataGridView1.Columns[9].HeaderText = "";
                        }
                        else
                        {
                            dataGridView1.Columns[1].HeaderText = "WP dist";
                            dataGridView1.Columns[2].HeaderText = "WP Verify";
                            dataGridView1.Columns[3].HeaderText = "Target Bear";
                            dataGridView1.Columns[4].HeaderText = "Nav Bear";
                            dataGridView1.Columns[5].HeaderText = "Long error";
                            dataGridView1.Columns[6].HeaderText = "Lat error";
                            dataGridView1.Columns[7].HeaderText = "Nav Lon";
                            dataGridView1.Columns[8].HeaderText = "Nav Lat";
                            dataGridView1.Columns[9].HeaderText = "";
                        }
                        break;
                    case "CTUN":
                        if (MainV2.APMFirmware == MainV2.Firmwares.ArduPilotMega)
                        {
                            dataGridView1.Columns[1].HeaderText = "Servo Roll";
                            dataGridView1.Columns[2].HeaderText = "nav_roll";
                            dataGridView1.Columns[3].HeaderText = "roll_sensor";
                            dataGridView1.Columns[4].HeaderText = "Servo Pitch";
                            dataGridView1.Columns[5].HeaderText = "nav_pitch";
                            dataGridView1.Columns[6].HeaderText = "pitch_sensor";
                            dataGridView1.Columns[7].HeaderText = "Servo Throttle";
                            dataGridView1.Columns[8].HeaderText = "Servo Rudder";
                            dataGridView1.Columns[9].HeaderText = "AN 4";
                        }
                        else
                        {
                            dataGridView1.Columns[1].HeaderText = "Rudder IN";
                            dataGridView1.Columns[2].HeaderText = "Rudder Out";
                            dataGridView1.Columns[3].HeaderText = "Yaw Debug";
                            dataGridView1.Columns[4].HeaderText = "Yaw";
                            dataGridView1.Columns[5].HeaderText = "Nav yaw";
                            dataGridView1.Columns[6].HeaderText = "Yaw Error";
                            dataGridView1.Columns[7].HeaderText = "Omega Z";
                            dataGridView1.Columns[8].HeaderText = "Throttle Out";
                            dataGridView1.Columns[9].HeaderText = "Sonar Alt";
                            dataGridView1.Columns[10].HeaderText = "Baro_Alt";
                            dataGridView1.Columns[11].HeaderText = "NextWP Alt";
                            dataGridView1.Columns[12].HeaderText = "Alt Error";
                            dataGridView1.Columns[13].HeaderText = "Sonar I";
                        }
                        break;
                    case "PM":
                        dataGridView1.Columns[1].HeaderText = "loop time";
                        dataGridView1.Columns[2].HeaderText = "Main count";
                        dataGridView1.Columns[3].HeaderText = "G_Dt_max";
                        dataGridView1.Columns[4].HeaderText = "Gyro Sat";
                        dataGridView1.Columns[5].HeaderText = "adc constr";
                        dataGridView1.Columns[6].HeaderText = "renorm_sqrt";
                        dataGridView1.Columns[7].HeaderText = "renorm_blowup";
                        dataGridView1.Columns[8].HeaderText = "gps_fix count";
                        dataGridView1.Columns[9].HeaderText = "imu_health";
                        break;
                    case "RAW":
                        dataGridView1.Columns[1].HeaderText = "Gyro X";
                        dataGridView1.Columns[2].HeaderText = "Gyro Y";
                        dataGridView1.Columns[3].HeaderText = "Gyro Z";
                        dataGridView1.Columns[4].HeaderText = "Accel X";
                        dataGridView1.Columns[5].HeaderText = "Accel Y";
                        dataGridView1.Columns[6].HeaderText = "Accel Z";
                        dataGridView1.Columns[7].HeaderText = "";
                        dataGridView1.Columns[8].HeaderText = "";
                        dataGridView1.Columns[9].HeaderText = "";
                        break;
                    default:
                        dataGridView1.Columns[1].HeaderText = "1";
                        dataGridView1.Columns[2].HeaderText = "2";
                        dataGridView1.Columns[3].HeaderText = "3";
                        dataGridView1.Columns[4].HeaderText = "4";
                        dataGridView1.Columns[5].HeaderText = "5";
                        dataGridView1.Columns[6].HeaderText = "6";
                        dataGridView1.Columns[7].HeaderText = "7";
                        dataGridView1.Columns[8].HeaderText = "8";
                        dataGridView1.Columns[9].HeaderText = "9";
                        dataGridView1.Columns[10].HeaderText = "10";
                        dataGridView1.Columns[11].HeaderText = "11";
                        dataGridView1.Columns[12].HeaderText = "12";
                        dataGridView1.Columns[13].HeaderText = "13";
                        break;
                }
            }
            catch { Console.WriteLine("DGV logbrowse error"); }
        }

        public void CreateChart(ZedGraphControl zgc)
        {
            GraphPane myPane = zgc.GraphPane;

            // Set the titles and axis labels
            myPane.Title.Text = "Value Graph";
            myPane.XAxis.Title.Text = "Line Number";
            myPane.YAxis.Title.Text = "Output";

            LineItem myCurve;

            myCurve = myPane.AddCurve("Value", list1, Color.Red, SymbolType.None);
            myCurve = myPane.AddCurve("Value", list2, Color.Green, SymbolType.None);
            myCurve = myPane.AddCurve("Value", list3, Color.Blue, SymbolType.None);
            myCurve = myPane.AddCurve("Value", list4, Color.Pink, SymbolType.None);
            myCurve = myPane.AddCurve("Value", list5, Color.Yellow, SymbolType.None);                           

            // Show the x axis grid
            myPane.XAxis.MajorGrid.IsVisible = true;

            myPane.XAxis.Scale.Min = 0;
            //myPane.XAxis.Scale.Max = -1;

            // Make the Y axis scale red
            myPane.YAxis.Scale.FontSpec.FontColor = Color.Red;
            myPane.YAxis.Title.FontSpec.FontColor = Color.Red;
            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;
            // Don't display the Y zero line
            myPane.YAxis.MajorGrid.IsZeroLine = true;
            // Align the Y axis labels so they are flush to the axis
            myPane.YAxis.Scale.Align = AlignP.Inside;
            // Manually set the axis range
            //myPane.YAxis.Scale.Min = -1;
            //myPane.YAxis.Scale.Max = 1;

            // Fill the axis background with a gradient
            //myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);


            // Calculate the Axis Scale Ranges
            try
            {
                zg1.AxisChange();
            }
            catch { }



        }

        private void Graphit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.RowCount == 0 || dataGridView1.ColumnCount == 0)
            {
                MessageBox.Show("Please load a valid file");
                return;
            }

            int col = dataGridView1.CurrentCell.ColumnIndex;
            int row = dataGridView1.CurrentCell.RowIndex;
            string type = dataGridView1[0,row].Value.ToString();
            double a = 0; // row counter

            if (col == 0)
            {
                MessageBox.Show("Please pick another column, Highlight the cell you wish to graph");
                return;
            }

            int error = 0;

            foreach (DataGridViewRow datarow in dataGridView1.Rows)
            {
                if (datarow.Cells[0].Value.ToString() == type)
                {
                    try
                    {
                        double value = double.Parse(datarow.Cells[col].Value.ToString(),new System.Globalization.CultureInfo("en-US"));
                        if (graphs == 0)
                        {
                            zg1.GraphPane.CurveList[0].Label.Text = dataGridView1.Columns[col].HeaderText;
                            list1.Add(a, value);
                        }
                        else if (graphs == 1)
                        {
                            zg1.GraphPane.CurveList[1].Label.Text = dataGridView1.Columns[col].HeaderText;
                            list2.Add(a, value);
                        }
                        else if (graphs == 2)
                        {
                            zg1.GraphPane.CurveList[2].Label.Text = dataGridView1.Columns[col].HeaderText;
                            list3.Add(a, value);
                        }
                        else if (graphs == 3)
                        {
                            zg1.GraphPane.CurveList[3].Label.Text = dataGridView1.Columns[col].HeaderText;
                            list4.Add(a, value);
                        }
                        else if (graphs == 4)
                        {
                            zg1.GraphPane.CurveList[4].Label.Text = dataGridView1.Columns[col].HeaderText;
                            list5.Add(a, value);
                        }
                        else
                        {
                            MessageBox.Show("Max of 5");
                            break;
                        }
                    }
                    catch { error++; Console.WriteLine("Bad Data : " + type + " " + col + " " + a); if (error >= 500) { MessageBox.Show("There is to much bad data - failing"); break; } }
                }
                a++;
            }

            // Make sure the Y axis is rescaled to accommodate actual data
            try
            {
                zg1.AxisChange();
            }
            catch { }
            // Zoom all
            zg1.ZoomOutAll(zg1.GraphPane);
            // Force a redraw
            zg1.Invalidate();

            graphs++;
        }

        private void BUT_cleargraph_Click(object sender, EventArgs e)
        {
            graphs = 0;
            foreach (LineItem line in zg1.GraphPane.CurveList) {
                line.Clear();
                line.Label.Text = "Value";
            }
            zg1.Invalidate();
        }

        private void BUT_loadlog_Click(object sender, EventArgs e)
        {
            // reset column count
            m_iColumnCount = 0;
            // clear existing lists
            zg1.GraphPane.CurveList.Clear();
            // reload
            Form1_Load(sender, e);
        }
    }
}
