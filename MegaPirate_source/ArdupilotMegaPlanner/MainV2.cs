﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Xml;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Diagnostics;
using System.Runtime.InteropServices; 
using System.Speech.Synthesis;
using System.Globalization;
using System.Threading;
using System.Net.Sockets;

namespace ArdupilotMega
{
    public partial class MainV2 : Form
    {
        [DllImport("user32.dll")]
        public static extern int FindWindow(string szClass, string szTitle);
        [DllImport("user32.dll")]
        public static extern int ShowWindow(int Handle, int showState);

        const int SW_SHOWNORMAL = 1;
        const int SW_HIDE = 0;

        public static MAVLink comPort = new MAVLink();
        public static string comportname = "";
        public static Hashtable config = new Hashtable();
        public static bool givecomport = false;
        public static Firmwares APMFirmware = Firmwares.ArduPilotMega;
        public static bool MAC = false;

        public static bool speechenable = false;
        public static SpeechSynthesizer talk = new SpeechSynthesizer();

        public static Joystick joystick = null;
        DateTime lastjoystick = DateTime.Now;

        public static WebCamService.Capture cam = null;

        public static CurrentState cs = new CurrentState();

        bool serialthread = false;

        TcpListener listener; 

        DateTime heatbeatsend = DateTime.Now;

        public static List<System.Threading.Thread> threads = new List<System.Threading.Thread>();
        public static MainV2 instance = null;

        public enum Firmwares
        {
            ArduPilotMega,
            ArduCopter2,
            MegaPirate,
        }

        GCSViews.FlightData FlightData;
        GCSViews.FlightPlanner FlightPlanner;
        //GCSViews.Configuration Configuration;
        GCSViews.Simulation Simulation;
        GCSViews.Firmware Firmware;
        GCSViews.FirmwareMP FirmwareMP;
        //GCSViews.Terminal Terminal;

        public MainV2()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var t = Type.GetType("Mono.Runtime");
            MAC = (t != null);

            Form splash = new Splash();
            splash.Show();

            string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            splash.Text = "APM Planner " + Application.ProductVersion + " Build " + strVersion + " By Michael Oborne";

            splash.Refresh();

            //talk.SpeakAsync("Welcome to APM Planner");

            InitializeComponent();

            MyRenderer.currentpressed = MenuFlightData;

            MainMenu.Renderer = new MyRenderer();

            List<object> list = new List<object>();
            foreach (object obj in Enum.GetValues(typeof(Firmwares)))
            {
                TOOL_APMFirmware.Items.Add(obj);
            }

            if (TOOL_APMFirmware.Items.Count > 0)
                TOOL_APMFirmware.SelectedIndex = 0;

            this.Text = splash.Text;

            comPort.BaudRate = 115200;

            CMB_serialport.Items.AddRange(SerialPort.GetPortNames());
            if (CMB_serialport.Items.Count > 0)
            {
                CMB_serialport.SelectedIndex = 0;
                CMB_baudrate.SelectedIndex = 7;
            }

            splash.Refresh();

            xmlconfig(false);

            if (config.ContainsKey("language") && !string.IsNullOrEmpty((string)config["language"]))
                changelanguage(getcultureinfo((string)config["language"]));

            if (!MAC) // windows only
            {
                if (MainV2.config["showconsole"] != null && MainV2.config["showconsole"].ToString() == "True")
                {
                }
                else
                {
                    int win = FindWindow("ConsoleWindowClass", null);
                    ShowWindow(win, SW_HIDE); // hide window
                }
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, 56781);
                System.Threading.Thread t12 = new System.Threading.Thread(delegate() { try { listernforclients(); } catch (Exception) { } });
                t12.IsBackground = true;
                // wait for tcp connections               
                t12.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            try
            {
                FlightData = new GCSViews.FlightData();
                FlightPlanner = new GCSViews.FlightPlanner();
                //Configuration = new GCSViews.Configuration();
                Simulation = new GCSViews.Simulation();
                Firmware = new GCSViews.Firmware();
                //Terminal = new GCSViews.Terminal();
            }
            catch (Exception e) { MessageBox.Show("A Major error has occured : " + e.ToString()); this.Close(); }

            changeunits();

            System.Threading.Thread.Sleep(2000);

            try
            {
                if (config["MainHeight"] != null)
                    this.Height = int.Parse(config["MainHeight"].ToString());
                if (config["MainWidth"] != null)
                    this.Width = int.Parse(config["MainWidth"].ToString());
                if (config["MainMaximised"] != null)
                    this.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState), config["MainMaximised"].ToString());


                if (config["CMB_rateattitude"] != null)
                    MainV2.cs.rateattitude = byte.Parse(config["CMB_rateattitude"].ToString());
                if (config["CMB_rateattitude"] != null)
                    MainV2.cs.rateposition = byte.Parse(config["CMB_rateposition"].ToString());
                if (config["CMB_rateattitude"] != null)
                    MainV2.cs.ratestatus = byte.Parse(config["CMB_ratestatus"].ToString());
                if (config["CMB_rateattitude"] != null)
                    MainV2.cs.raterc = byte.Parse(config["CMB_raterc"].ToString());
                                
                if (config["speechenable"] != null)
                    MainV2.speechenable = bool.Parse(config["speechenable"].ToString());

            }
            catch { } 
            

            instance = this;
            splash.Close();
        }

        private void CMB_serialport_Click(object sender, EventArgs e)
        {
            CMB_serialport.Items.Clear();
            CMB_serialport.Items.AddRange(SerialPort.GetPortNames());
        }

        public static void fixtheme(Control temp)
        {
            fixtheme(temp, 0);
        }

        public static void fixtheme(Control temp, int level)
        {
            if (level == 0)
            {
                temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                temp.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
            }
            //Console.WriteLine(temp.GetType());

            foreach (Control ctl in temp.Controls)
            {
                if (((Type)ctl.GetType()) == typeof(System.Windows.Forms.Button))
                {
                    ctl.ForeColor = Color.Black;
                    System.Windows.Forms.Button but = (System.Windows.Forms.Button)ctl;
                }
                else if (((Type)ctl.GetType()) == typeof(TextBox))
                {
                    ctl.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
                    TextBox txt = (TextBox)ctl;
                    txt.BorderStyle = BorderStyle.None;
                }
                else if (((Type)ctl.GetType()) == typeof(DomainUpDown))
                {
                    ctl.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
                    DomainUpDown txt = (DomainUpDown)ctl;
                    txt.BorderStyle = BorderStyle.None;
                }
                else if (((Type)ctl.GetType()) == typeof(GroupBox))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    ctl.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
                }
                else if (((Type)ctl.GetType()) == typeof(ZedGraph.ZedGraphControl))
                {
                    ZedGraph.ZedGraphControl zg1 = (ZedGraph.ZedGraphControl)ctl;
                    zg1.GraphPane.Chart.Fill = new ZedGraph.Fill(Color.FromArgb(0x1f, 0x1f, 0x20));
                    zg1.GraphPane.Fill = new ZedGraph.Fill(Color.FromArgb(0x37, 0x37, 0x38));

                    foreach (ZedGraph.LineItem li in zg1.GraphPane.CurveList)
                    {
                        li.Line.Width = 4;
                    }

                    zg1.GraphPane.Title.FontSpec.FontColor = Color.White;

                    zg1.GraphPane.XAxis.MajorTic.Color = Color.White;
                    zg1.GraphPane.XAxis.MinorTic.Color = Color.White;
                    zg1.GraphPane.YAxis.MajorTic.Color = Color.White;
                    zg1.GraphPane.YAxis.MinorTic.Color = Color.White;

                    zg1.GraphPane.XAxis.MajorGrid.Color = Color.White;
                    zg1.GraphPane.YAxis.MajorGrid.Color = Color.White;

                    zg1.GraphPane.YAxis.Scale.FontSpec.FontColor = Color.White;
                    zg1.GraphPane.YAxis.Title.FontSpec.FontColor = Color.White;

                    zg1.GraphPane.XAxis.Scale.FontSpec.FontColor = Color.White;
                    zg1.GraphPane.XAxis.Title.FontSpec.FontColor = Color.White;

                    zg1.GraphPane.Legend.Fill = new ZedGraph.Fill(Color.FromArgb(0x85, 0x84, 0x83));
                    zg1.GraphPane.Legend.FontSpec.FontColor = Color.White;
                }
                else if (((Type)ctl.GetType()) == typeof(BSE.Windows.Forms.Panel) || ((Type)ctl.GetType()) == typeof(System.Windows.Forms.SplitterPanel))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    ctl.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
                }
                else if (((Type)ctl.GetType()) == typeof(Form))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    ctl.ForeColor = Color.White;// Color.FromArgb(0xe6, 0xe8, 0xea);
                }
                else if (((Type)ctl.GetType()) == typeof(RichTextBox))
                {
                    ctl.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;
                    RichTextBox txtr = (RichTextBox)ctl;
                    txtr.BorderStyle = BorderStyle.None;
                }
                else if (((Type)ctl.GetType()) == typeof(TabPage))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);  //Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;
                    TabPage txtr = (TabPage)ctl;
                    txtr.BorderStyle = BorderStyle.None;
                }
                else if (((Type)ctl.GetType()) == typeof(TabControl))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);  //Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;
                    TabControl txtr = (TabControl)ctl;

                }
                else if (((Type)ctl.GetType()) == typeof(DataGridView))
                {
                    ctl.ForeColor = Color.White;
                    DataGridView dgv = (DataGridView)ctl;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.BorderStyle = BorderStyle.None;
                    dgv.BackgroundColor = Color.FromArgb(0x26, 0x27, 0x28);
                    DataGridViewCellStyle rs = new DataGridViewCellStyle();
                    rs.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    rs.ForeColor = Color.White;
                    dgv.RowsDefaultCellStyle = rs;

                    DataGridViewCellStyle hs = new DataGridViewCellStyle(dgv.ColumnHeadersDefaultCellStyle);
                    hs.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    hs.ForeColor = Color.White;

                    dgv.ColumnHeadersDefaultCellStyle = hs;

                    dgv.RowHeadersDefaultCellStyle = hs;
                }
                else if (((Type)ctl.GetType()) == typeof(ComboBox))
                {
                    ctl.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;
                    ComboBox CMB = (ComboBox)ctl;
                    CMB.FlatStyle = FlatStyle.Flat;
                }
                else if (((Type)ctl.GetType()) == typeof(NumericUpDown))
                {
                    ctl.BackColor = Color.FromArgb(0x43, 0x44, 0x45);
                    ctl.ForeColor = Color.White;
                }
                else if (((Type)ctl.GetType()) == typeof(TrackBar))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    ctl.ForeColor = Color.White;
                }
                else if (((Type)ctl.GetType()) == typeof(LinkLabel))
                {
                    ctl.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
                    ctl.ForeColor = Color.White;
                    LinkLabel LNK = (LinkLabel)ctl;
                    LNK.ActiveLinkColor = Color.White;
                    LNK.LinkColor = Color.White;
                    LNK.VisitedLinkColor = Color.White;
                
                }

                if (ctl.Controls.Count > 0)
                    fixtheme(ctl, 1);
            }
        }

        private void MenuFlightData_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();

            GCSViews.Terminal.threadrun = false;

            UserControl temp = FlightData;

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Location = new Point(0, MainMenu.Height);

            temp.Dock = DockStyle.Fill;

            //temp.ForeColor = Color.White;

            //temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);

            MyView.Controls.Add(temp);

            if (MainV2.config["FlightSplitter"] != null)
                ((GCSViews.FlightData)temp).MainHcopy.SplitterDistance = int.Parse(MainV2.config["FlightSplitter"].ToString());
        }

        private void MenuFlightPlanner_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();

            GCSViews.Terminal.threadrun = false;

            UserControl temp = FlightPlanner;

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Location = new Point(0, MainMenu.Height);

            temp.Dock = DockStyle.Fill;

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);

            MyView.Controls.Add(temp);
        }

        private void MenuConfiguration_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();

            GCSViews.Terminal.threadrun = false;

            UserControl temp = new GCSViews.Configuration();

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Location = new Point(0, MainMenu.Height);

            temp.Dock = DockStyle.Fill;

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);

            MyView.Controls.Add(temp);
        }

        private void MenuSimulation_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();

            GCSViews.Terminal.threadrun = false;

            UserControl temp = Simulation;

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Location = new Point(0, MainMenu.Height);

            temp.Dock = DockStyle.Fill;

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);

            MyView.Controls.Add(temp);
        }

        private void MenuFirmware_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();
            GCSViews.Terminal.threadrun = false;
            Boolean _is_ffimu = true;

            if (MainV2.cs.firmware == MainV2.Firmwares.MegaPirate)
            {
                if (MessageBox.Show(this, "Do you have a FFIMU?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                    _is_ffimu = false;
            }
 
            UserControl temp = null;
            if (MainV2.cs.firmware == MainV2.Firmwares.MegaPirate)
            {
                if (MainV2.cs.firmware == MainV2.Firmwares.MegaPirate && FirmwareMP == null)
                    FirmwareMP = new GCSViews.FirmwareMP();

                FirmwareMP.Is_FFIMU = _is_ffimu;
                temp = FirmwareMP;
            }
            else
            {
                temp = Firmware;
                fixtheme(temp);
            }
            if (MainV2.cs.firmware != MainV2.Firmwares.MegaPirate)
            {
                temp.ForeColor = Color.White;
                temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
            }
            else
            {
                temp.ForeColor = Color.Black;
                temp.BackColor = System.Drawing.SystemColors.Control;
            }


            /*
            MyView.Controls.Clear();

            GCSViews.Terminal.threadrun = false;

            UserControl temp = Firmware;

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Dock = DockStyle.Fill;

            MyView.Controls.Add(temp);

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
             */
        }

        private void MenuTerminal_Click(object sender, EventArgs e)
        {
            if (comPort.IsOpen)
            {
                MenuConnect_Click(sender, e);
            }

            givecomport = true;

            MyView.Controls.Clear();

            this.MenuConnect.BackgroundImage = global::ArdupilotMega.Properties.Resources.disconnect;

            UserControl temp = new GCSViews.Terminal();

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Dock = DockStyle.Fill;

            MyView.Controls.Add(temp);

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);

        }

        private void MenuConnect_Click(object sender, EventArgs e)
        {
            givecomport = false;

            if (comPort.IsOpen)
            {
                try
                {
                    if (talk != null) // cancel all pending speech
                        talk.SpeakAsyncCancelAll();

                    comPort.Close();
                }
                catch { }

                this.MenuConnect.BackgroundImage = global::ArdupilotMega.Properties.Resources.connect;
            }
            else
            {
                try
                {
                    comPort.BaudRate = int.Parse(CMB_baudrate.Text);
                }
                catch { }
                comPort.DataBits = 8;
                comPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "1");
                comPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None");

                comPort.DtrEnable = false;

                if (config["CHK_resetapmonconnect"] != null && bool.Parse(config["CHK_resetapmonconnect"].ToString()) == true)
                    comPort.DtrEnable = true;

                if (DialogResult.OK != Common.MessageShowAgain("Mavlink Connect","Make sure your APM slider switch is in Flight Mode (away from RC pins)")) {
                    return;
                }

                try
                {
                    comPort.PortName = CMB_serialport.Text;
                    comPort.Open(true);

                    if (comPort.param["SYSID_SW_TYPE"] != null) { 
                        if (float.Parse(comPort.param["SYSID_SW_TYPE"].ToString()) == 10) {
                            TOOL_APMFirmware.SelectedIndex = TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduCopter2);
                        } else if (float.Parse(comPort.param["SYSID_SW_TYPE"].ToString()) == 0) {
                            TOOL_APMFirmware.SelectedIndex = TOOL_APMFirmware.Items.IndexOf(Firmwares.ArduPilotMega);
                        }
                    }

                    cs.firmware = APMFirmware;

                    this.MenuConnect.BackgroundImage = global::ArdupilotMega.Properties.Resources.disconnect;
                }
                catch (Exception ex) { try { comPort.Close(); } catch { } MessageBox.Show("Is your CLI switch in Flight position?\n(this is required for MAVlink comms)\n\n" + ex.ToString()); return; }
            }
        }

        private void CMB_serialport_SelectedIndexChanged(object sender, EventArgs e)
        {
            comportname = CMB_serialport.Text;
            comPort.PortName = CMB_serialport.Text;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            //Form temp = new Main();
            //temp.Show();
        }

        private void MainV2_FormClosed(object sender, FormClosedEventArgs e)
        {
            GCSViews.FlightData.threadrun = 0;
            GCSViews.Simulation.threadrun = 0;

            serialthread = false;

            try
            {
                if (comPort.IsOpen)
                    comPort.Close();
            }
            catch { } // i get alot of these errors, the port is still open, but not valid - user has unpluged usb
            try
            {
                FlightData.Dispose();
            }
            catch { }
            try
            {
                FlightPlanner.Dispose();
            }
            catch { }
            try
            {
                Simulation.Dispose();
            }
            catch { }

            xmlconfig(true);
        }

        
        private void xmlconfig(bool write)
        {
            if (write || !File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml"))
            {
                try
                {
                    XmlTextWriter xmlwriter = new XmlTextWriter(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml", Encoding.ASCII);
                    xmlwriter.WriteStartDocument();

                    xmlwriter.WriteStartElement("Config");

                    //xmlwriter.WriteElementString("APMlocation", defineslocation);

                    xmlwriter.WriteElementString("comport", comportname);

                    xmlwriter.WriteElementString("baudrate", CMB_baudrate.Text);

                    xmlwriter.WriteElementString("APMFirmware", APMFirmware.ToString());

                    foreach (string key in config.Keys)
                    {
                        xmlwriter.WriteElementString(key, config[key].ToString());
                    }

                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteEndDocument();
                    xmlwriter.Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            }
            else
            {
                try
                {
                    using (XmlTextReader xmlreader = new XmlTextReader(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml"))
                    {
                        while (xmlreader.Read())
                        {
                            xmlreader.MoveToElement();
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "comport":
                                        string temp = xmlreader.ReadString();

                                        CMB_serialport.SelectedIndex = CMB_serialport.FindString(temp);
                                        if (CMB_serialport.SelectedIndex == -1)
                                        {
                                            CMB_serialport.Text = temp; // allows ports that dont exist - yet
                                        }
                                        comPort.PortName = temp;
                                        comportname = temp;
                                        break;
                                    case "baudrate":
                                        string temp2 = xmlreader.ReadString();

                                        CMB_baudrate.SelectedIndex = CMB_baudrate.FindString(temp2);
                                        if (CMB_baudrate.SelectedIndex == -1)
                                        {
                                            CMB_baudrate.SelectedIndex = CMB_baudrate.FindString("57600"); ; // must exist
                                        }
                                        //bau = int.Parse(CMB_baudrate.Text);
                                        break;
                                    case "APMFirmware":
                                        string temp3 = xmlreader.ReadString();
                                        TOOL_APMFirmware.SelectedIndex = TOOL_APMFirmware.FindStringExact(temp3);
                                        if (TOOL_APMFirmware.SelectedIndex == -1)
                                            TOOL_APMFirmware.SelectedIndex = 0;
                                        APMFirmware = (MainV2.Firmwares)Enum.Parse(typeof(MainV2.Firmwares), TOOL_APMFirmware.Text);
                                        break;
                                    case "Config":
                                        break;
                                    case "xml":
                                        break;
                                    default:
                                        config.Add(xmlreader.Name, xmlreader.ReadString());
                                        break;
                                }
                            }
                            catch { } // silent fail on bad entry
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("Bad Config File: "+ex.ToString()); } // bad config file
            }
        }

        private void CMB_baudrate_SelectedIndexChanged(object sender, EventArgs e)
        {
            comPort.BaudRate = int.Parse(CMB_baudrate.Text);
        }

        private void SerialReader()
        {
            if (serialthread == true)
                return;
            serialthread = true;

            DateTime menuupdate = DateTime.Now;

            DateTime speechcustomtime = DateTime.Now;

            while (serialthread)
            {
                try
                {
                    System.Threading.Thread.Sleep(10);
                    if ((DateTime.Now - menuupdate).Milliseconds > 500)
                    {
//                        Console.WriteLine(DateTime.Now.Millisecond);
                        if (comPort.IsOpen)
                        {
                            if ((string)this.MenuConnect.BackgroundImage.Tag != "Disconnect")
                            {
                                MenuConnect.BackgroundImage = global::ArdupilotMega.Properties.Resources.disconnect;
                                this.MenuConnect.BackgroundImage.Tag = "Disconnect";
                            }
                        }
                        else
                        {
                            if ((string)this.MenuConnect.BackgroundImage.Tag != "Connect")
                            {
                                this.MenuConnect.BackgroundImage = global::ArdupilotMega.Properties.Resources.connect;
                                this.MenuConnect.BackgroundImage.Tag = "Connect";
                            }
                        }
                        menuupdate = DateTime.Now;
                    }

                    if (!MAC)
                    {
                        //joystick stuff

                        if (joystick != null && joystick.enabled)
                        {
                            MAVLink.__mavlink_rc_channels_override_t rc = new MAVLink.__mavlink_rc_channels_override_t();

                            rc.target_component = comPort.compid;
                            rc.target_system = comPort.sysid;

                            rc.chan1_raw = cs.rcoverridech1;//(ushort)(((int)state.Rz / 65.535) + 1000);
                            rc.chan2_raw = cs.rcoverridech2;//(ushort)(((int)state.Y / 65.535) + 1000);
                            rc.chan3_raw = cs.rcoverridech3;//(ushort)(1000 - ((int)slider[0] / 65.535 ) + 1000);
                            rc.chan4_raw = cs.rcoverridech4;//(ushort)(((int)state.X / 65.535) + 1000);

                            if (lastjoystick.AddMilliseconds(100) < DateTime.Now)
                            {
                                //Console.WriteLine("{0} {1} {2} {3} ", rc.chan1_raw, rc.chan2_raw, rc.chan3_raw, rc.chan4_raw);
                                //                            Console.WriteLine(DateTime.Now.Millisecond);
                                comPort.generatePacket(MAVLink.MAVLINK_MSG_ID_RC_CHANNELS_OVERRIDE, rc);
                                lastjoystick = DateTime.Now;
                            }
                        }
                    }

                    if (speechenable && talk != null && (DateTime.Now - speechcustomtime).TotalSeconds > 30 && MainV2.cs.lat != 0 && (MainV2.comPort.logreadmode || comPort.IsOpen))
                    {
                        //speechbatteryvolt
                        float warnvolt = 0;
                        float.TryParse(MainV2.getConfig("speechbatteryvolt"),out warnvolt);

                        if (MainV2.getConfig("speechbatteryenabled") == "True" && MainV2.cs.battery_voltage <= warnvolt)
                        {
                            MainV2.talk.SpeakAsync(Common.speechConversion(MainV2.getConfig("speechbattery")));
                        }

                        if (MainV2.getConfig("speechcustomenabled") == "True") {
                            MainV2.talk.SpeakAsync(Common.speechConversion(MainV2.getConfig("speechcustom")));
                        }

                        speechcustomtime = DateTime.Now;
                    }

                    if (!comPort.IsOpen || givecomport == true)
                        continue;

                    if (heatbeatsend.Second != DateTime.Now.Second)
                    {
                        Console.WriteLine("remote lost {0}", cs.packetdrop);

                        MAVLink.__mavlink_heartbeat_t htb = new MAVLink.__mavlink_heartbeat_t();

                        htb.type = (byte)MAVLink.MAV_TYPE.MAV_GENERIC;
                        htb.autopilot = (byte)MAVLink.MAV_AUTOPILOT_TYPE.MAV_AUTOPILOT_ARDUPILOTMEGA;
                        htb.mavlink_version = 1;

                        comPort.generatePacket((byte)MAVLink.MAVLINK_MSG_ID_HEARTBEAT, htb);
                        heatbeatsend = DateTime.Now;
                    }

                    // data loss warning
                    if (speechenable && talk != null && (DateTime.Now - comPort.lastvalidpacket).TotalSeconds > 10)
                    {
                        if (MainV2.talk.State == SynthesizerState.Ready)
                            MainV2.talk.SpeakAsync("WARNING No Data for " + (int)(DateTime.Now - comPort.lastvalidpacket).TotalSeconds + " Seconds");
                    }

                    while (comPort.BytesToRead > 10 && givecomport == false)
                        comPort.readPacket();
                }
                catch (Exception e) { Console.WriteLine("Serial Reader fail :" + e.Message); }
            }
        }

        private class MyRenderer : ToolStripProfessionalRenderer
        {
            public static ToolStripItem currentpressed;
            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                //BackgroundImage
                if (e.Item.BackgroundImage == null) base.OnRenderButtonBackground(e);
                else
                {
                    Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                    e.Graphics.DrawImage(e.Item.BackgroundImage, bounds);
                    if (e.Item.Pressed || e.Item == currentpressed)
                    {
                        SolidBrush brush = new SolidBrush(Color.FromArgb(73, 0x2b, 0x3a, 0x03));
                        e.Graphics.FillRectangle(brush, bounds);
                        if (e.Item.Name != "MenuConnect")
                        {
                            //Console.WriteLine("new " + e.Item.Name + " old " + currentpressed.Name );
                            //e.Item.GetCurrentParent().Invalidate();
                            if (currentpressed != e.Item)
                                currentpressed.Invalidate();
                            currentpressed = e.Item;
                        }
                        
                        // Something...
                    }
                    else if (e.Item.Selected) // mouse over
                    {
                        SolidBrush brush = new SolidBrush(Color.FromArgb(73, 0x2b, 0x3a, 0x03));
                        e.Graphics.FillRectangle(brush, bounds);
                        // Something...
                    }
                    using (Pen pen = new Pen(Color.Black))
                    {
                        //e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                    }
                }
            }

            protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
            {
                //base.OnRenderItemImage(e);
            }
        }

        private void MainV2_Load(object sender, EventArgs e)
        {
            if (!MAC)
            {
                /*
                DeviceList joysticklist = Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly);

                foreach (DeviceInstance device in joysticklist)
                {
                    //if (device.InstanceName == name)
                    {
                        Console.WriteLine("Using {0}",device.ProductName);
                        joystick = new Device(device.InstanceGuid);
                        break;
                    }
                }

                joystick.SetDataFormat(DeviceDataFormat.Joystick);

                joystick.Acquire();
                 */
            }

            MenuFlightData_Click(sender, e);

            System.Threading.Thread t11 = new System.Threading.Thread(delegate() { SerialReader(); });
            t11.IsBackground = true;
            t11.Name = "Main Serial reader";
            t11.Start();
        }

        /// <summary>          
        /// little web server for sending network link kml's          
        /// </summary>          

        void listernforclients()
        {
            listener.Start();
            // Enter the listening loop.               
            while (true)
            {
                // Perform a blocking call to accept requests.           
                // You could also user server.AcceptSocket() here.               
                try
                {
                    Console.WriteLine("Listerning for client - 1 client at a time");
                    TcpClient client = listener.AcceptTcpClient();
                    // Get a stream object for reading and writing          
                    Console.WriteLine("Accepted Client "+ client.Client.RemoteEndPoint.ToString());
                    //client.SendBufferSize = 100 * 1024; // 100kb
                    //client.LingerState.Enabled = true;
                    //client.NoDelay = true;

                    NetworkStream stream = client.GetStream();

                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

                    byte[] request = new byte[1024];

                    int len = stream.Read(request, 0, request.Length);
                    string head = System.Text.ASCIIEncoding.ASCII.GetString(request, 0, len);
                    Console.WriteLine(head);

                    int index = head.IndexOf('\n');

                    string url = head.Substring(0,index - 1);
                    //url = url.Replace("\r", "");
                    //url = url.Replace("GET ","");
                    //url = url.Replace(" HTTP/1.0", "");
                    //url = url.Replace(" HTTP/1.1", "");

                    string header = "HTTP/1.1 200 OK\r\nContent-Type: multipart/x-mixed-replace;boundary=APMPLANNER\n\n--APMPLANNER\r\n";
                    byte[] temp = encoding.GetBytes(header);
                    stream.Write(temp, 0, temp.Length);

                    while (client.Connected)
                    {
                        System.Threading.Thread.Sleep(200); // 5hz
                        byte[] data = null;

                        if (url.ToLower().Contains("hud"))
                        {
                            data = GCSViews.FlightData.myhud.streamjpg.ToArray();
                        }
                        else if (url.ToLower().Contains("map"))
                        {
                            data = GCSViews.FlightData.mymap.streamjpg.ToArray();
                        }
                        else
                        {
                            Image img1 = Image.FromStream(GCSViews.FlightData.myhud.streamjpg);
                            Image img2 = Image.FromStream(GCSViews.FlightData.mymap.streamjpg);
                            int bigger = img1.Height > img2.Height ? img1.Height : img2.Height;
                            Image imgout = new Bitmap(img1.Width + img2.Width,bigger);
                            
                            Graphics grap = Graphics.FromImage(imgout);

                            grap.DrawImageUnscaled(img1,0,0);
                            grap.DrawImageUnscaled(img2,img1.Width,0);

                            MemoryStream streamjpg = new MemoryStream();
                            imgout.Save(streamjpg,System.Drawing.Imaging.ImageFormat.Jpeg);
                            data = streamjpg.ToArray();

                        }

                        header = "Content-Type: image/jpeg\r\nContent-Length: " + data.Length + "\r\n\r\n";
                        temp = encoding.GetBytes(header);
                        stream.Write(temp, 0, temp.Length);

                        stream.Write(data, 0, data.Length);

                        header = "\r\n--APMPLANNER\r\n";
                        temp = encoding.GetBytes(header);
                        stream.Write(temp, 0, temp.Length);

                    }
                    /*
                    while (client.Connected)
                    {

                        byte[] data = GCSViews.FlightData.myhud.streamjpg.ToArray();

                        byte[] request = new byte[1024];

                        int len = stream.Read(request, 0, request.Length);
                        Console.WriteLine(System.Text.ASCIIEncoding.ASCII.GetString(request, 0, len));

                        string header = "HTTP/1.1 200 OK\nContent-Length: " + data.Length + "\nContent-Type: image/jpeg\n\n";
                        byte[] temp = encoding.GetBytes(header);
                        stream.Write(temp, 0, temp.Length);

                        stream.Write(data, 0, data.Length);

                    }
                    */
                    stream.Close();

                }
                catch (Exception) { }
            }
        }

        private void TOOL_APMFirmware_SelectedIndexChanged(object sender, EventArgs e)
        {
            APMFirmware = (MainV2.Firmwares)Enum.Parse(typeof(MainV2.Firmwares), TOOL_APMFirmware.Text);
            MainV2.cs.firmware = APMFirmware;
        }

        private void MainV2_Resize(object sender, EventArgs e)
        {
            Console.WriteLine("myview width "+MyView.Width + " height " + MyView.Height );
            Console.WriteLine("this   width " + this.Width + " height " + this.Height);
        }

        private void MenuHelp_Click(object sender, EventArgs e)
        {
            MyView.Controls.Clear();

            UserControl temp = new GCSViews.Help();

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Dock = DockStyle.Fill;

            MyView.Controls.Add(temp);

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
        }

        public static void updatecheck(Label loadinglabel)
        {
            try
            {
                //string baseurl = "http://ardupilot-mega.googlecode.com/svn/Tools/trunk/ArdupilotMegaPlanner/bin/Release/";
                string baseurl = "http://ardupirates.googlecode.com/svn/branches/Sandmen/MegaPirate_source/ArdupilotMegaPlanner/bin/Release/";
                bool update = updatecheck(loadinglabel, baseurl, "");
                System.Diagnostics.Process P = new System.Diagnostics.Process();
                if (MAC)
                {
                    P.StartInfo.FileName = "mono";
                    P.StartInfo.Arguments = " \"" + Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "Updater.exe\"";
                }
                else
                {
                    P.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "Updater.exe";
                    P.StartInfo.Arguments = "";
                    try
                    {
                        foreach (string newupdater in Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "Updater.exe*.new"))
                        {
                            File.Copy(newupdater, newupdater.Remove(newupdater.Length - 4), true);
                            File.Delete(newupdater);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                if (loadinglabel != null)
                    loadinglabel.Text = "Starting Updater";
                Console.WriteLine("Start " + P.StartInfo.FileName + " with " + P.StartInfo.Arguments);
                P.Start();
                try
                {
                    Application.Exit();
                }
                catch { }
            }
            catch (Exception ex) { MessageBox.Show("Update Failed " + ex.Message); }
        }
        private static bool updatecheck(Label loadinglabel, string baseurl, string subdir)
        {
                bool update = false;
                List<string> files = new List<string>();

                // Create a request using a URL that can receive a post. 
                WebRequest request = WebRequest.Create(baseurl);
                request.Timeout = 10000;
                // Set the Method property of the request to POST.
                request.Method = "GET";
                // Get the request stream.
                Stream dataStream; //= request.GetRequestStream();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                Regex regex = new Regex("href=\"([^\"]+)\"", RegexOptions.IgnoreCase);
                if (regex.IsMatch(responseFromServer))
                {
                    MatchCollection matchs = regex.Matches(responseFromServer);
                    for (int i = 0; i < matchs.Count; i++)
                    {
                        if (matchs[i].Groups[1].Value.ToString().Contains(".."))
                            continue;
                        if (matchs[i].Groups[1].Value.ToString().Contains("http"))
                            continue;
                        files.Add(System.Web.HttpUtility.UrlDecode(matchs[i].Groups[1].Value.ToString()));
                    }
                }

                //Console.WriteLine(responseFromServer);
                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();

            string dir = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + subdir;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
                foreach (string file in files)
                {
                    if (file.EndsWith("/"))
                {
                    update = updatecheck(loadinglabel, baseurl + file, file) && update;
                        continue;
                }
                    if (loadinglabel != null)
                        loadinglabel.Text = "Checking " + file;
                    // Create a request using a URL that can receive a post. 
                    request = WebRequest.Create(baseurl + file);
                    Console.Write(baseurl + file + " ");
                    // Set the Method property of the request to POST.
                    request.Method = "HEAD";
                    // Get the response.
                    response = request.GetResponse();
                    // Display the status.
                    Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                    // Get the stream containing content returned by the server.
                    dataStream = response.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.

                string path = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + subdir + file;

                    bool getfile = false;

                    if (File.Exists(path))
                    {
                        FileInfo fi = new FileInfo(path);

                        if (fi.Length != response.ContentLength || fi.LastWriteTimeUtc < DateTime.Parse(response.Headers["Last-Modified"].ToString()))
                        {
                            getfile = true;
                            Console.WriteLine("NEW FILE " + file + " " + fi.LastWriteTime + " < " + DateTime.Parse(response.Headers["Last-Modified"].ToString()));
                        }
                    }
                    else
                    {
                        getfile = true;
                        Console.WriteLine("NEW FILE " + file);
                        // get it
                    }

                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    if (getfile)
                    {
                        if (!update)
                        {
                            //DialogResult dr = MessageBox.Show("Update Found\n\nDo you wish to update now?", "Update Now", MessageBoxButtons.YesNo);
                            //if (dr == DialogResult.Yes)
                            {
                                update = true;
                            }
                            //else
                            {
                                //    return;
                            }
                        }
                        if (loadinglabel != null)
                            loadinglabel.Text = "Getting " + file;

                        // Create a request using a URL that can receive a post. 
                        request = WebRequest.Create(baseurl + file);
                        // Set the Method property of the request to POST.
                        request.Method = "GET";
                        // Get the response.
                        response = request.GetResponse();
                        // Display the status.
                        Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                        // Get the stream containing content returned by the server.
                        dataStream = response.GetResponseStream();

                        long bytes = response.ContentLength;
                        long contlen = bytes;

                        byte[] buf1 = new byte[1024];

                        FileStream fs = new FileStream(path + ".new", FileMode.Create); // 

                        DateTime dt = DateTime.Now;

                        while (dataStream.CanRead && bytes > 0)
                        {
                            try
                            {
                                if (dt.Second != DateTime.Now.Second)
                                {
                                    if (loadinglabel != null)
                                        loadinglabel.Text = "Getting " + file + ":" + (((double)(contlen - bytes) / (double)contlen) * 100).ToString("0.0") + "%";
                                    dt = DateTime.Now;
                                }
                            }
                            catch { }
                            Console.WriteLine(file + " " + bytes);
                            int len = dataStream.Read(buf1, 0, 1024);
                            bytes -= len;
                            fs.Write(buf1, 0, len);
                        }

                        fs.Close();
                        dataStream.Close();
                        response.Close();
                    }


                }

                //P.StartInfo.CreateNoWindow = true;
                //P.StartInfo.RedirectStandardOutput = true;
            return update;


        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                Form frm = new temp();
                frm.Show();
                return true; 
            }
            if (keyData == (Keys.Control | Keys.G)) // test
            {
                Form frm = new Setup.Setup();
                frm.Show();
                return true;
            }
            if (keyData == (Keys.Control | Keys.R)) // for ryan beall
            {
                MainV2.comPort.Open(false);
                return true;
            }
            if (keyData == (Keys.Control | Keys.J)) // for jani
            {
                string data = "!!";
                Common.InputBox("inject", "enter data to be written", ref data);
                MainV2.comPort.Write(data + "\r");
                return true;
            } 
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void starttest()
        {
            MyView.Controls.Clear();

            UserControl temp = new GCSViews.test();

            fixtheme(temp);

            temp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            temp.Dock = DockStyle.Fill;

            MyView.Controls.Add(temp);

            temp.ForeColor = Color.White;

            temp.BackColor = Color.FromArgb(0x26, 0x27, 0x28);
        }
        public void changelanguage(CultureInfo ci)
        {
            if (ci != null && !Thread.CurrentThread.CurrentUICulture.Equals(ci))
            {
                Thread.CurrentThread.CurrentUICulture = ci;
                config["language"] = ci.Name;
                //System.Threading.Thread.CurrentThread.CurrentCulture = ci;

                HashSet<Control> views = new HashSet<Control> { FlightData, FlightPlanner, Simulation, Firmware };

                foreach (Control view in MyView.Controls)
                    views.Add(view);

                foreach (Control view in views)
                {
                    if (view != null)
                    {
                        ComponentResourceManager rm = new ComponentResourceManager(view.GetType());
                        foreach (Control ctrl in view.Controls)
                            applyresource(rm, ctrl);
                        rm.ApplyResources(view, "$this");
                    }
                }
            }
        }

        private void applyresource(ComponentResourceManager rm, Control ctrl)
        {
            rm.ApplyResources(ctrl, ctrl.Name);
            foreach (Control subctrl in ctrl.Controls)
                applyresource(rm, subctrl);

            if (ctrl.ContextMenu != null)
                applyresource(rm, ctrl.ContextMenu);
            

            if (ctrl is DataGridView)
            {
                foreach (DataGridViewColumn col in (ctrl as DataGridView).Columns)
                    rm.ApplyResources(col, col.Name);
            }

            
        }

        private void applyresource(ComponentResourceManager rm, Menu menu)
        {
            rm.ApplyResources(menu, menu.Name);
            foreach (MenuItem submenu in menu.MenuItems)
                applyresource(rm, submenu);
        }

        public static CultureInfo getcultureinfo(string name)
        {
            try
            {
                return new CultureInfo(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void MainV2_FormClosing(object sender, FormClosingEventArgs e)
        {
            config["MainHeight"] = this.Height;
            config["MainWidth"] = this.Width;
            config["MainMaximised"] = this.WindowState.ToString();
        }

        public static string getConfig(string paramname)
        {
            if (config[paramname] != null)
                return config[paramname].ToString();
            return "";
        }

        public void changeunits()
        {
            try
            {
                // dist
                if (MainV2.config["distunits"] != null)
                {
                    switch ((Common.distances)Enum.Parse(typeof(Common.distances), MainV2.config["distunits"].ToString()))
                    {
                        case Common.distances.Meters:
                            MainV2.cs.multiplierdist = 1;
                            break;
                        case Common.distances.Feet:
                            MainV2.cs.multiplierdist = 3.2808399f;
                            break;
                    }
                }

                // speed
                if (MainV2.config["speedunits"] != null)
                {
                    switch ((Common.speeds)Enum.Parse(typeof(Common.speeds), MainV2.config["speedunits"].ToString()))
                    {
                        case Common.speeds.ms:
                            MainV2.cs.multiplierspeed = 1;
                            break;
                        case Common.speeds.fps:
                            MainV2.cs.multiplierdist = 3.2808399f;
                            break;
                        case Common.speeds.kph:
                            MainV2.cs.multiplierspeed = 3.6f;
                            break;
                        case Common.speeds.mph:
                            MainV2.cs.multiplierspeed = 2.23693629f;
                            break;
                        case Common.speeds.knots:
                            MainV2.cs.multiplierspeed = 1.94384449f;
                            break;
                    }
                }
            }
            catch { }
            
        }
    }
}
