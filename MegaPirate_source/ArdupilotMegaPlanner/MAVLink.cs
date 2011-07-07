﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Collections; // hashs
using System.Diagnostics; // stopwatch
using System.Reflection;
using System.Reflection.Emit;
using System.IO;


namespace ArdupilotMega
{
    public partial class MAVLink : SerialPort
    {
        byte packetcount = 0;
        public byte sysid = 0;
        public byte compid = 0;
        public Hashtable param = new Hashtable();
        public static byte[][] packets = new byte[256][];
        public double[] packetspersecond = new double[256];
        DateTime[] packetspersecondbuild = new DateTime[256];
        static object objlock = new object();
        public DateTime lastvalidpacket = DateTime.Now;

        public bool debugmavlink = false;

        public bool logreadmode = false;
        public DateTime lastlogread = DateTime.MinValue;
        public BinaryReader logplaybackfile = null;
        public BinaryWriter logfile = null;

        public static byte[] streams = new byte[256];

        int bps1 = 0;
        int bps2 = 0;
        public int bps = 0;
        public DateTime bpstime = DateTime.Now;
        int recvpacketcount = 0;
        int packetslost = 0;

        //Stopwatch stopwatch = new Stopwatch();

        public new void Close()
        {
            base.Close();
        }

        public new void Open()
        {
            Open(false);
        }

        public void Open(bool getparams)
        {
            if (this.IsOpen)
                return;

            System.Windows.Forms.Form frm = Common.LoadingBox("Mavlink Connecting..", "Mavlink Connecting..");

            // reset
            sysid = 0;
            compid = 0;
            param = new Hashtable();

            try
            {
                MainV2.givecomport = true;

                this.ReadBufferSize = 4 * 1024;

                lock (objlock) // so we dont have random traffic
                {

                    base.Open();

                    this.DiscardInBuffer();

                    // allow 2560 connect timeout on usb
                    System.Threading.Thread.Sleep(1000);

                }

                byte[] buffer;
                byte[] buffer1;

                DateTime start = DateTime.Now;

                int count = 0;

                while (true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    frm.Refresh();

                    // incase we are in setup mode
                    base.WriteLine("planner\rgcs\r");

                    frm.Controls[0].Text = (start.AddSeconds(30) - DateTime.Now).Seconds.ToString("Timeout in 0");

                    if (lastbad[0] == '!' && lastbad[1] == 'G') // waiting for gps lock
                    {
                        frm.Controls[0].Text = "Waiting for GPS detection..";
                        start = start.AddSeconds(5); // each round is 1.1 seconds
                    }

                    frm.Refresh();
                    
                    if (!(start.AddSeconds(30) > DateTime.Now))
                    {
                        /*
                        System.Windows.Forms.DialogResult dr = System.Windows.Forms.MessageBox.Show("Data recevied but no mavlink packets where read from this port\nWhat do you want to do",
                            "Read Fail", System.Windows.Forms.MessageBoxButtons.RetryCancel);
                        if (dr == System.Windows.Forms.DialogResult.Retry)
                        {
                            port.toggleDTRnow(); // force reset on usb
                            start = DateTime.Now;
                        }
                        else*/
                        {
                            frm.Close();
                            this.Close();
                            throw new Exception("Data Recevied But No Mavlink Packets where read from this port - Verify Baud Rate and setup\nIt might also be waiting for GPS Lock");
                        }
                    }

                    System.Threading.Thread.Sleep(1);

                    // incase we are in setup mode
                    base.WriteLine("planner\rgcs\r");

                    frm.Refresh();

                    buffer = getHeartBeat();

                    frm.Refresh();

                    // incase we are in setup mode
                    base.WriteLine("planner\rgcs\r");

                    System.Threading.Thread.Sleep(1);

                    frm.Refresh();

                    buffer1 = getHeartBeat();

                    frm.Refresh();

                    try
                    {
                        Console.WriteLine("MAv Data: len " + buffer.Length + " btr " + this.BytesToRead);
                    }
                    catch { }

                    count++;

                    if (buffer.Length > 5 && buffer1.Length > 5 && buffer[3] == buffer1[3] && buffer[4] == buffer1[4])
                    {
                        sysid = buffer[3];
                        compid = buffer[4];
                        recvpacketcount = buffer[2];
                        Console.WriteLine("ID " + sysid + " " + compid);
                        break;
                    }
                }

                frm.Controls[0].Text = "Getting Params.. (sysid "+sysid+") ";
                frm.Refresh();
                if (getparams == true)
                    getParamList();
            }
            catch (Exception e) { MainV2.givecomport = false; frm.Close(); throw e; }

            frm.Close();

            MainV2.givecomport = false;

            Console.WriteLine("Done open "+ sysid + " " + compid);

        }

        public static byte[] StructureToByteArrayEndian(params object[] list)
        {
            // The copy is made becuase SetValue won't work on a struct.
            // Boxing was used because SetValue works on classes/objects.
            // Unfortunately, it results in 2 copy operations.
            object thisBoxed = list[0]; // Why make a copy?
            Type test = thisBoxed.GetType();

            int offset = 0;
            byte[] data = new byte[Marshal.SizeOf(thisBoxed)];

            // System.Net.IPAddress.NetworkToHostOrder is used to perform byte swapping.
            // To convert unsigned to signed, 'unchecked()' was used.
            // See http://stackoverflow.com/questions/1131843/how-do-i-convert-uint-to-int-in-c

            // Enumerate each structure field using reflection.
            foreach (var field in test.GetFields())
            {
                // field.Name has the field's name.

                object fieldValue = field.GetValue(thisBoxed); // Get value

                // Get the TypeCode enumeration. Multiple types get mapped to a common typecode.
                TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());

                switch (typeCode)
                {
                    case TypeCode.Single: // float
                        {
                            byte[] temp = BitConverter.GetBytes((Single)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Single));
                            break;
                        }
                    case TypeCode.Int32:
                        {
                            byte[] temp = BitConverter.GetBytes((Int32)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int32));
                            break;
                        }
                    case TypeCode.UInt32:
                        {
                            byte[] temp = BitConverter.GetBytes((UInt32)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt32));
                            break;
                        }
                    case TypeCode.Int16:
                        {
                            byte[] temp = BitConverter.GetBytes((Int16)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int16));
                            break;
                        }
                    case TypeCode.UInt16:
                        {
                            byte[] temp = BitConverter.GetBytes((UInt16)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt16));
                            break;
                        }
                    case TypeCode.Int64:
                        {
                            byte[] temp = BitConverter.GetBytes((Int64)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Int64));
                            break;
                        }
                    case TypeCode.UInt64:
                        {
                            byte[] temp = BitConverter.GetBytes((UInt64)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(UInt64));
                            break;
                        }
                    case TypeCode.Double:
                        {
                            byte[] temp = BitConverter.GetBytes((Double)fieldValue);
                            Array.Reverse(temp);
                            Array.Copy(temp, 0, data, offset, sizeof(Double));
                            break;
                        }
                    case TypeCode.Byte:
                        {
                            data[offset] = (Byte)fieldValue;
                            break;
                        }
                    default:
                        {
                            //System.Diagnostics.Debug.Fail("No conversion provided for this type : " + typeCode.ToString());
                            break;
                        }
                }; // switch
                if (typeCode == TypeCode.Object)
                {
                    int length = ((byte[])fieldValue).Length;
                    Array.Copy(((byte[])fieldValue), 0, data, offset, length);
                    offset += length;
                }
                else
                {
                    offset += Marshal.SizeOf(fieldValue);
                }
            } // foreach

            return data;
        } // Swap

        byte[] getHeartBeat()
        {
            DateTime start = DateTime.Now;
            while (true)
            {
                byte[] buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_HEARTBEAT)
                    {
                        return buffer;
                    }
                }
                if (DateTime.Now > start.AddMilliseconds(1200))
                    return new byte[0];
            }
        }

        /// <summary>
        /// Generate a Mavlink Packet and write to serial
        /// </summary>
        /// <param name="messageType">type number</param>
        /// <param name="indata">struct of data</param>
        public void generatePacket(byte messageType, object indata)
        {
            byte[] data = StructureToByteArrayEndian(indata);

            //Console.WriteLine(DateTime.Now + " PC Doing req "+ messageType + " " + this.BytesToRead);
            byte[] packet = new byte[data.Length + 6 + 2];

            packet[0] = (byte)'U';
            packet[1] = (byte)data.Length;
            packet[2] = packetcount;
            packet[3] = 255; // this is always 255 - MYGCS
            packet[4] = (byte)MAV_COMPONENT.MAV_COMP_ID_WAYPOINTPLANNER;
            packet[5] = messageType;

            int i = 6;
            foreach (byte b in data)
            {
                packet[i] = b;
                i++;
            }

            ushort checksum = crc_calculate(packet, packet[1] + 6);
            byte ck_a = (byte)(checksum & 0xFF); ///< High byte
            byte ck_b = (byte)(checksum >> 8); ///< Low byte

            packet[i] = ck_a;
            i += 1;
            packet[i] = ck_b;
            i += 1;

            if (this.IsOpen)
            {
                lock (objlock)
                {
                    this.Write(packet, 0, i);
                }
            }

            try
            {
                if (logfile != null)
                {
                    byte[] datearray = ASCIIEncoding.ASCII.GetBytes(DateTime.Now.ToBinary() + ":");
                    logfile.BaseStream.Write(datearray, 0, datearray.Length);
                    logfile.BaseStream.Write(packet, 0, i);
                    logfile.BaseStream.Write(new byte[] { (byte)'\n' }, 0, 1);
                }

            }
            catch { }

            if (messageType == ArdupilotMega.MAVLink.MAVLINK_MSG_ID_PARAM_SET)
            {
                BinaryWriter bw = new BinaryWriter(File.OpenWrite("serialsent.raw"));
                bw.Seek(0, SeekOrigin.End);
                bw.Write(packet, 0, i);
                bw.Write("\n");
                bw.Close();
            }
            
            packetcount++;

            //System.Threading.Thread.Sleep(1);
        }

        public new bool Write(string line)
        {
            lock (objlock)
            {
                base.Write(line);
            }
            return true;
        }

        /// <summary>
        /// Set parameter on apm
        /// </summary>
        /// <param name="paramname">name as a string</param>
        /// <param name="value"></param>
        public bool setParam(string paramname,float value)
        {
            MainV2.givecomport = true;

            __mavlink_param_set_t req = new __mavlink_param_set_t();
            req.target_system = sysid;
            req.target_component = compid;

            byte[] temp = ASCIIEncoding.ASCII.GetBytes(paramname);

            Array.Resize(ref temp, 15);

            req.param_id = temp;
            req.param_value = (value);

            generatePacket(MAVLINK_MSG_ID_PARAM_SET, req);

            Console.WriteLine("setParam '{0}' = '{1}' sysid {2} compid {3}", paramname, req.param_value, sysid, compid);

            DateTime start = DateTime.Now;
            int retrys = 3;

            while (true)
            {
                if (!(start.AddMilliseconds(1000) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("setParam Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_PARAM_SET, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - setParam");
                }

                byte[] buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_PARAM_VALUE)
                    {
                        param[paramname] = req.param_value;
                        MainV2.givecomport = false;
                        return true;
                    }
                }
            }
        }

        public struct _param
        {
            public string name;
            public float value;
        }

        /// <summary>
        /// Get param list from apm
        /// </summary>
        /// <returns></returns>
        public Hashtable getParamList()
        {
            MainV2.givecomport = true;
            List<int> missed = new List<int>();
            _param[] paramarray = new _param[300];

            __mavlink_param_request_list_t req = new __mavlink_param_request_list_t();
            req.target_system = sysid;
            req.target_component = compid;

            generatePacket(MAVLINK_MSG_ID_PARAM_REQUEST_LIST, req);

            DateTime start = DateTime.Now;

            int retrys = 3;
            int nextid = 0;
            int a = 0;
            int z = 5;
            while (a < z)
            {
                if (!(start.AddMilliseconds(5000) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("getParamList Retry " + retrys + " sys " + sysid + " comp " + compid);
                        generatePacket(MAVLINK_MSG_ID_PARAM_REQUEST_LIST, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - getParamList");
                }
                byte[] buffer = readPacket();
                if (buffer.Length > 5)
                {
                    //stopwatch.Reset();
                    //stopwatch.Start();
                    if (buffer[5] == MAVLINK_MSG_ID_PARAM_VALUE)
                    {
                        start = DateTime.Now;

                        __mavlink_param_value_t par = new __mavlink_param_value_t();

                        object temp = par;

                        ByteArrayToStructure(buffer, ref temp, 6);

                        par = (__mavlink_param_value_t)temp;

                        z = (par.param_count);

                        Console.WriteLine(DateTime.Now.Millisecond +" got param " + (par.param_index) + " of " + (z - 1));

                        if (nextid == (par.param_index))
                        {
                            nextid++;
                        }
                        else
                        {
                            if (retrys > 0) { 
                                generatePacket(MAVLINK_MSG_ID_PARAM_REQUEST_LIST, req); 
                                a = 0; 
                                nextid = 0; 
                                retrys--; 
                                continue; 
                            }
                            missed.Add(nextid); // for later devel
                            MainV2.givecomport = false;
                            throw new Exception("Missed ID expecting " + nextid + " got " + (par.param_index) + "\nPlease try loading again");                             
                        }
                       
                        string st = System.Text.ASCIIEncoding.ASCII.GetString(par.param_id);

                        int pos = st.IndexOf('\0');

                        if (pos != -1) {
                            st = st.Substring(0,pos);
                        }

                        param[st] = (par.param_value);
                        
                        a++;
                    }
                    else
                    {
                        //Console.WriteLine(DateTime.Now + " PC paramlist " + buffer[5] + " " + this.BytesToRead);
                    }
                    //stopwatch.Stop();
                    //Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
                }
            }
            MainV2.givecomport = false;
            return param;
        }

        public _param getParam()
        {
            throw new Exception("getParam Not implemented");
            /*
            _param temp = new _param();

            temp.name = "none";
            temp.value = 0;

            return temp;
             */
        }

        /// <summary>
        /// Stops all requested data packets.
        /// </summary>
        public void stopall(bool forget)
        {
            __mavlink_request_data_stream_t req = new __mavlink_request_data_stream_t();
            req.target_system = sysid;
            req.target_component = compid;

            req.req_message_rate = 10;
            req.start_stop = 0; // stop
            req.req_stream_id = 0; // all

            // reset all
            if (forget)
            {
                lock (objlock)
                {
                    streams = new byte[streams.Length];
                }
            }

            // no error on bad
            try
            {
                generatePacket(MAVLINK_MSG_ID_REQUEST_DATA_STREAM, req);
                System.Threading.Thread.Sleep(20);
                generatePacket(MAVLINK_MSG_ID_REQUEST_DATA_STREAM, req);
                System.Threading.Thread.Sleep(20);
                generatePacket(MAVLINK_MSG_ID_REQUEST_DATA_STREAM, req);
                Console.WriteLine("Stopall Done");

            }
            catch { }
        }

        public void setWPACK()
        {
            MAVLink.__mavlink_waypoint_ack_t req = new MAVLink.__mavlink_waypoint_ack_t();
            req.target_system = sysid;
            req.target_component = compid;
            req.type = 0;

            generatePacket(MAVLINK_MSG_ID_WAYPOINT_ACK, req);
        }

        public bool setWPCurrent(ushort index)
        {
            MainV2.givecomport = true;
            byte[] buffer;

            __mavlink_waypoint_set_current_t req = new __mavlink_waypoint_set_current_t();

            req.target_system = sysid;
            req.target_component = compid;
            req.seq = index;

            generatePacket(MAVLINK_MSG_ID_WAYPOINT_SET_CURRENT, req);

            DateTime start = DateTime.Now;
            int retrys = 5;

            while (true)
            {
                if (!(start.AddMilliseconds(2000) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("setWPCurrent Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_WAYPOINT_SET_CURRENT, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - setWPCurrent");
                }

                buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT_CURRENT)
                    {
                        MainV2.givecomport = false;
                        return true;
                    }
                }
            }
        }

        public bool doAction(MAV_ACTION actionid)
        {
            MainV2.givecomport = true;
            byte[] buffer;

            __mavlink_action_t req = new __mavlink_action_t();

            req.target = sysid;
            req.target_component = compid;

            req.action = (byte)actionid;

            generatePacket(MAVLINK_MSG_ID_ACTION, req);

            DateTime start = DateTime.Now;
            int retrys = 3;

            while (true)
            {
                if (!(start.AddMilliseconds(2000) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("doAction Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_ACTION, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - doAction");
                }

                buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_ACTION_ACK)
                    {
                        if (buffer[7] == 1) {
                            MainV2.givecomport = false;
                            return true;
                        } else {
                            MainV2.givecomport = false;
                            return false;
                        }
                    }
                }
            }
        }

        public void requestDatastream(byte id,byte hzrate)
        {
            lock (objlock)
            {
                streams[id] = hzrate;
            }

            double pps = 0;

            switch (id) {
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_ALL:

                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_EXTENDED_STATUS:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_GPS_STATUS] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_GPS_STATUS];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_EXTRA1:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_ATTITUDE] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_ATTITUDE];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_EXTRA2:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_VFR_HUD] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_VFR_HUD];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_EXTRA3:

                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_POSITION:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_GLOBAL_POSITION_INT] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_GLOBAL_POSITION_INT];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_RAW_CONTROLLER:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_RC_CHANNELS_SCALED] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_RC_CHANNELS_SCALED];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_RAW_SENSORS:

                    break;
                case (byte)MAVLink.MAV_DATA_STREAM.MAV_DATA_STREAM_RC_CHANNELS:
                    if (packetspersecondbuild[MAVLINK_MSG_ID_RC_CHANNELS_RAW] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[MAVLINK_MSG_ID_RC_CHANNELS_RAW];
                    if (hzratecheck(pps,hzrate))
                        {
                            return;
                        }
                    break;
            }
           
            //packetspersecond[temp[5]];

            if (pps == 0 && hzrate == 0)
            {
                return;
            }

                Console.WriteLine("Request stream {0} at {1} hz : currently {2}", Enum.Parse(typeof(MAV_DATA_STREAM), id.ToString()), hzrate,pps);
                getDatastream(id, hzrate);
        }

        // returns true for ok
        bool hzratecheck(double pps, int hzrate)
        {

            if (hzrate == 0 && pps == 0)
            {
                return true;
            } else if (hzrate == 1 && pps >= 1 && pps <= 2)
            {
                return true;
            }
            else if (hzrate == 3 && pps  >= 2 && hzrate < 5)
            {
                return true;
            }
            else if (hzrate == 10 && pps > 5 && hzrate < 15)
            {
                return true;
            }
            else if (hzrate > 15 && pps > 15)
            {
                return true;
            }

            return false;

        }

        void getDatastream(byte id, byte hzrate)
        {
            __mavlink_request_data_stream_t req = new __mavlink_request_data_stream_t();
            req.target_system = sysid;
            req.target_component = compid;

            req.req_message_rate = hzrate;
            req.start_stop = 1; // start
            req.req_stream_id = id; // id

            generatePacket(MAVLINK_MSG_ID_REQUEST_DATA_STREAM, req);
            generatePacket(MAVLINK_MSG_ID_REQUEST_DATA_STREAM, req);
        }

        /// <summary>
        /// Returns WP count
        /// </summary>
        /// <returns></returns>
        public byte getWPCount()
        {
            MainV2.givecomport = true;
            byte[] buffer;

            __mavlink_waypoint_request_list_t req = new __mavlink_waypoint_request_list_t();

            req.target_system = sysid;
            req.target_component = compid;

            // request list
            generatePacket(MAVLINK_MSG_ID_WAYPOINT_REQUEST_LIST, req);

            DateTime start = DateTime.Now;
            int retrys = 3;

            while (true)
            {
                if (!(start.AddMilliseconds(1000) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("getWPCount Retry " + retrys + " - giv com " + MainV2.givecomport);
                        generatePacket(MAVLINK_MSG_ID_WAYPOINT_REQUEST_LIST, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    //return (byte)int.Parse(param["WP_TOTAL"].ToString());
                    throw new Exception("Timeout on read - getWPCount");
                }

                buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT_COUNT)
                    {

                        Console.WriteLine("wpcount: " + buffer[9]);
                        MainV2.givecomport = false;
                        return buffer[9]; // should be ushort, but apm has limited wp count < byte
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + " PC wpcount " + buffer[5] + " need " + MAVLINK_MSG_ID_WAYPOINT_COUNT + " " + this.BytesToRead);
                    }
                }
            }
        }
        /// <summary>
        /// Gets specfied WP
        /// </summary>
        /// <param name="index"></param>
        /// <returns>WP</returns>
        public Locationwp getWP(ushort index)
        {
            MainV2.givecomport = true;
            Locationwp loc = new Locationwp();

            __mavlink_waypoint_request_t req = new __mavlink_waypoint_request_t();

            req.target_system = sysid;
            req.target_component = compid;

            req.seq = index;

            // request
            generatePacket(MAVLINK_MSG_ID_WAYPOINT_REQUEST, req);

            DateTime start = DateTime.Now;
            int retrys = 5;

            while (true)
            {
                if (!(start.AddMilliseconds(800) > DateTime.Now)) // apm times out after 1000ms
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("getWP Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_WAYPOINT_REQUEST, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - getWP");
                }
                byte[] buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT)
                    {
                        __mavlink_waypoint_t wp = new __mavlink_waypoint_t();

                        object temp = (object)wp;

                        //Array.Copy(buffer, 6, buffer, 0, buffer.Length - 6);

                        ByteArrayToStructure(buffer, ref temp , 6);

                        wp = (__mavlink_waypoint_t)(temp);

                        loc.options = (byte)(wp.frame & 0x1);
                        loc.id = (byte)(wp.command);
                        loc.p1 = (byte)(wp.param1);
                        if (loc.id < (byte)MAV_CMD.LAST)
                        {
                            loc.alt = (int)((wp.z) * 100);
                            loc.lat = (int)((wp.x) * 10000000);
                            loc.lng = (int)((wp.y) * 10000000);
                        }
                        else
                        {

                            loc.alt = (int)((wp.z));
                            loc.lat = (int)((wp.x));
                            loc.lng = (int)((wp.y));

                            switch (loc.id)
                            {					// Switch to map APM command fields inot MAVLink command fields
                                case (byte)MAV_CMD.LOITER_TURNS:
                                case (byte)MAV_CMD.TAKEOFF:
                                case (byte)MAV_CMD.DO_SET_HOME:
                                case (byte)MAV_CMD.DO_SET_ROI:
                                    loc.p1 = (byte)wp.param1;
                                    break;

                                case (byte)MAV_CMD.CONDITION_CHANGE_ALT:
                                    loc.p1 = (byte)(wp.param1 * 100);
                                    break;

                                case (byte)MAV_CMD.LOITER_TIME:
                                    if (MainV2.APMFirmware == MainV2.Firmwares.ArduPilotMega)
                                    {
                                        loc.p1 = (byte)(wp.param1 / 10);	// APM loiter time is in ten second increments
                                    }
                                    else
                                    {
                                        loc.p1 = (byte)wp.param1;
                                    }
                                    break;

                                case (byte)MAV_CMD.CONDITION_DELAY:
                                case (byte)MAV_CMD.CONDITION_DISTANCE:
                                    loc.lat = (int)wp.param1;
                                    break;

                                case (byte)MAV_CMD.DO_JUMP:
                                    loc.lat = (int)wp.param2;
                                    loc.p1 = (byte)wp.param1;
                                    break;

                                case (byte)MAV_CMD.DO_REPEAT_SERVO:
                                    loc.lng = (int)wp.param4;
                                    goto case (byte)MAV_CMD.DO_CHANGE_SPEED;
                                case (byte)MAV_CMD.DO_REPEAT_RELAY:
                                case (byte)MAV_CMD.DO_CHANGE_SPEED:
                                    loc.lat = (int)wp.param3;
                                    loc.alt = (int)wp.param2;
                                    loc.p1 = (byte)wp.param1;
                                    break;

                                case (byte)MAV_CMD.DO_SET_PARAMETER:
                                case (byte)MAV_CMD.DO_SET_RELAY:
                                case (byte)MAV_CMD.DO_SET_SERVO:
                                    loc.alt = (int)wp.param2;
                                    loc.p1 = (byte)wp.param1;
                                    break;

                                case (byte)MAV_CMD.WAYPOINT:
                                    loc.p1 = (byte)wp.param1;
                                    break;
                            }
                        }

                        Console.WriteLine("getWP {0} {1} {2} {3} {4} opt {5}",loc.id,loc.p1,loc.alt,loc.lat,loc.lng,loc.options);

                        break;
                    }
                    else
                    {
                        //Console.WriteLine(DateTime.Now + " PC getwp " + buffer[5]);
                    }
                }
            }
            MainV2.givecomport = false;
            return loc;
        }

        /// <summary>
        /// Print entire decoded packet to console
        /// </summary>
        /// <param name="datin">packet byte array</param>
        /// <returns>struct of data</returns>
        public object DebugPacket(byte[] datin)
        {
            try
            {
                if (datin.Length > 5)
                {
                    byte header = datin[0];
                    byte length = datin[1];
                    byte seq = datin[2];
                    byte sysid = datin[3];
                    byte compid = datin[4];
                    byte messid = datin[5];

                    object data = Activator.CreateInstance(mavstructs[messid]);

                    ByteArrayToStructure(datin, ref data, 6);

                    Type test = data.GetType();

                    Console.Write(test.Name + " ");

                    foreach (var field in test.GetFields())
                    {
                        // field.Name has the field's name.

                        object fieldValue = field.GetValue(data); // Get value

                        if (field.FieldType.IsArray)
                        {
                            Console.Write(field.Name + "=");
                            byte[] crap = (byte[])fieldValue;
                            foreach (byte fiel in crap)
                            {
                                Console.Write(fiel + ",");
                            }
                            Console.Write(" ");
                        }
                        else
                        {
                            Console.Write(field.Name + "=" + fieldValue.ToString() + " ");
                        }
                    }
                    Console.WriteLine(" Len:" + datin.Length);

                    return data;
                }
            }
            catch { }

            return null;
        }       

        /// <summary>
        /// Sets wp total count
        /// </summary>
        /// <param name="wp_total"></param>
        public void setWPTotal(ushort wp_total)
        {
            MainV2.givecomport = true;
            __mavlink_waypoint_count_t req = new __mavlink_waypoint_count_t();

            req.target_system = sysid;
            req.target_component = compid; // MAVLINK_MSG_ID_WAYPOINT_COUNT

            req.count = wp_total;

            generatePacket(MAVLINK_MSG_ID_WAYPOINT_COUNT, req);

            DateTime start = DateTime.Now;
            int retrys = 3;

            while (true)
            {
                if (!(start.AddMilliseconds(700) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("setWPTotal Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_WAYPOINT_COUNT, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - setWPTotal");
                }
                byte[] buffer = readPacket();
                if (buffer.Length > 9)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT_REQUEST && buffer[9] == 0)
                    {
                        param["WP_TOTAL"] = (float)wp_total - 1;
                        MainV2.givecomport = false;
                        return;
                    }
                    else
                    {
                        //Console.WriteLine(DateTime.Now + " PC getwp " + buffer[5]);
                    }
                }
            }
        }

        /// <summary>
        /// Save wp to eeprom
        /// </summary>
        /// <param name="loc">location struct</param>
        /// <param name="index">wp no</param>
        /// <param name="frame">global or relative</param>
        /// <param name="current">0 = no , 2 = guided mode</param>
        public void setWP(Locationwp loc, ushort index,MAV_FRAME frame,byte current)
        {
            MainV2.givecomport = true;
            __mavlink_waypoint_t req = new __mavlink_waypoint_t();

            req.target_system = sysid;
            req.target_component = compid; // MAVLINK_MSG_ID_WAYPOINT

            req.command = loc.id;
            req.param1 = loc.p1;

            req.current = current;
            
            if (loc.id < (byte)MAV_CMD.LAST)
            {
                req.frame = (byte)frame;
                req.y = (float)(loc.lng / 10000000.0);
                req.x = (float)(loc.lat / 10000000.0);
                req.z = (float)(loc.alt / 100.0);
            }
            else
            {
                req.frame = (byte)MAVLink.MAV_FRAME.MAV_FRAME_MISSION; // mission
                req.y = (float)(loc.lng);
                req.x = (float)(loc.lat);
                req.z = (float)(loc.alt);

                switch (loc.id)
                {					// Switch to map APM command fields inot MAVLink command fields
                    case (byte)MAV_CMD.LOITER_TURNS:
                    case (byte)MAV_CMD.TAKEOFF:
                    case (byte)MAV_CMD.DO_SET_HOME:
                        req.param1 = loc.p1;
                        break;

                    case (byte)MAV_CMD.CONDITION_CHANGE_ALT:
                        req.param1 = loc.p1 / 100;
                        break;

                    case (byte)MAV_CMD.LOITER_TIME:
                        req.param1 = loc.p1 * 10;	// APM loiter time is in ten second increments
                        break;

                    case (byte)MAV_CMD.CONDITION_DELAY:
                    case (byte)MAV_CMD.CONDITION_DISTANCE:
                        req.param1 = loc.lat;
                        break;

                    case (byte)MAV_CMD.DO_JUMP:
                        req.param2 = loc.lat;
                        req.param1 = loc.p1;
                        break;

                    case (byte)MAV_CMD.DO_REPEAT_SERVO:
                        req.param4 = loc.lng;
                        goto case (byte)MAV_CMD.DO_CHANGE_SPEED;
                    case (byte)MAV_CMD.DO_REPEAT_RELAY:
                    case (byte)MAV_CMD.DO_CHANGE_SPEED:
                        req.param3 = loc.lat;
                        req.param2 = loc.alt;
                        req.param1 = loc.p1;
                        break;

                    case (byte)MAV_CMD.DO_SET_PARAMETER:
                    case (byte)MAV_CMD.DO_SET_RELAY:
                    case (byte)MAV_CMD.DO_SET_SERVO:
                        req.param2 = loc.alt;
                        req.param1 = loc.p1;
                        break;
                }
            }
            req.seq = index;

            Console.WriteLine("setWP {6} frame {0} cmd {1} p1 {2} x {3} y {4} z {5}", req.frame, req.command, req.param1, req.x, req.y, req.z, index);
            
            // request
            generatePacket(MAVLINK_MSG_ID_WAYPOINT, req);

            DateTime start = DateTime.Now;
            int retrys = 6;

            while (true)
            {
                if (!(start.AddMilliseconds(700) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                        Console.WriteLine("setWP Retry " + retrys);
                        generatePacket(MAVLINK_MSG_ID_WAYPOINT, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    MainV2.givecomport = false;
                    throw new Exception("Timeout on read - setWP");
                }
                byte[] buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT_ACK)
                    { //__mavlink_waypoint_request_t
                        Console.WriteLine("set wp " + index + " ACK 47 : " + buffer[5]);
                        break;
                    }
                    else if (buffer[5] == MAVLINK_MSG_ID_WAYPOINT_REQUEST)
                    { 
                        __mavlink_waypoint_request_t ans = new __mavlink_waypoint_request_t();

                        object temp = (object)ans;

                        //Array.Copy(buffer, 6, buffer, 0, buffer.Length - 6);

                        ByteArrayToStructure(buffer, ref temp , 6);

                        ans = (__mavlink_waypoint_request_t)(temp);

                        if (ans.seq == (index+1)) {
                            Console.WriteLine("set wp doing " + index + " req " + ans.seq + " REQ 40 : " + buffer[5]);
                            MainV2.givecomport = false;
                            break;
                        } else {
                            Console.WriteLine("set wp fail doing " + index + " req " + ans.seq + " ACK 47 or REQ 40 : " + buffer[5] + " seq {0} ts {1} tc {2}", req.seq,req.target_system,req.target_component);
                            //break;
                        }
                    }
                    else
                    {
                        //Console.WriteLine(DateTime.Now + " PC setwp " + buffer[5]);
                    }
                }
            }
        }

        /// <summary>
        /// used for last bad serial characters
        /// </summary>
        byte[] lastbad = new byte[2];
       
        /// <summary>
        /// Serial Reader to read mavlink packets. POLL method
        /// </summary>
        /// <returns></returns>
        public byte[] readPacket()
        {
            byte[] temp = new byte[300];
            int count = 0;
            int length = 0;
            int readcount = 0;
            lastbad = new byte[2];

            this.ReadTimeout = 1100; // 1100 ms between bytes

            DateTime start = DateTime.Now;

                while (this.IsOpen || logreadmode)
                {
                    try
                    {
                        if (readcount > 300)
                        {
                            Console.WriteLine("MAVLink readpacket No valid mavlink packets");
                            break;
                        }
                        readcount++;
                        if (logreadmode)
                        {
                            temp = readlogPacket();
                        }
                        else
                        {
                            temp[count] = (byte)this.ReadByte();
                        }
                    }
                    catch (Exception e) { Console.WriteLine("MAVLink readpacket read error: " + e.Message); break; }

                    if (temp[0] != 'U' || lastbad[0] == 'I' && lastbad[1] == 'M') // out of sync
                    {
                        if (temp[0] >= 0x20 && temp[0] <= 127 || temp[0] == '\n')
                        {
                            Console.Write((char)temp[0]);
                        }
                        count = 0;
                        lastbad[0] = lastbad[1];
                        lastbad[1] = temp[0];
                        temp[1] = 0;
                        continue;
                    }
                    // reset count on valid packet
                    readcount = 0;

                    if (temp[0] == 'U')
                    {
                        length = temp[1] + 6 + 2 - 2; // data + header + checksum - U - length
                        if (count >= 5 || logreadmode)
                        {
                            if (sysid != 0)
                            {
                                if (sysid != temp[3] || compid != temp[4])
                                {
                                    Console.WriteLine("Mavlink Bad Packet (not addressed to this MAV) got {0} {1} vs {2} {3}", temp[3], temp[4], sysid, compid);
                                    return new byte[0];
                                }
                            }

                            try
                            {
                                if (logreadmode)
                                {

                                }
                                else
                                {
                                    int to = 0;
                                    while (this.BytesToRead < length)
                                    {
                                        if (to > 1000)
                                        {
                                            Console.WriteLine("MAVLINK: wait time out btr {0} len {1}", this.BytesToRead, length);
                                            break;
                                        }
                                        System.Threading.Thread.Sleep(1);
                                        to++;
                                    }
                                    int read = this.Read(temp, 6, length - 4);
                                }
                                //Console.WriteLine("data " + read + " " + length + " aval " + this.BytesToRead);
                                count = length + 2;
                            }
                            catch { break; }
                            break;
                        }
                    }

                    count++;
                    if (count == 299)
                        break;
                }

            Array.Resize<byte>(ref temp, count);

            if (bpstime.Second != DateTime.Now.Second && !logreadmode)
            {
                Console.WriteLine("bps {0} loss {1} left {2}", bps1, packetslost, this.BytesToRead);
                bps2 = bps1; // prev sec
                bps1 = 0; // current sec
                bpstime = DateTime.Now;
            }

            bps1 += temp.Length;

            bps = (bps1 + bps2) / 2;

            if (temp.Length >= 5 && temp[3] == 255 && logreadmode) // gcs packet
            {
                return new byte[0];
            }

            ushort crc = crc_calculate(temp, temp.Length - 2);

            if (temp.Length < 5 || temp[temp.Length - 1] != (crc >> 8) || temp[temp.Length - 2] != (crc & 0xff))
            {
                int packetno = 0;
                if (temp.Length > 5)
                {
                    packetno = temp[5];
                }
                Console.WriteLine("Mavlink Bad Packet (crc fail) len {0} crc {1} pkno {2}", temp.Length, crc, packetno);
                return new byte[0];
            }

            try
            {
                if (temp[0] == 'U' && temp.Length >= temp[1])
                {
                    if (temp[2] != ((recvpacketcount + 1) % 0x100))
                    {
                        Console.WriteLine("lost {0}",temp[2]);
                        packetslost++; // actualy sync loss's
                    }
                    recvpacketcount = temp[2];

                    //MAVLINK_MSG_ID_GPS_STATUS
                    //if (temp[5] == MAVLINK_MSG_ID_GPS_STATUS)
                        
//                    Console.Write(temp[5] + " " + DateTime.Now.Millisecond + " " + packetspersecond[temp[5]] + " " + (DateTime.Now - packetspersecondbuild[temp[5]]).TotalMilliseconds + "     \n");

                    if (double.IsInfinity(packetspersecond[temp[5]]))
                        packetspersecond[temp[5]] = 0;

                    packetspersecond[temp[5]] = (((1000 / ((DateTime.Now - packetspersecondbuild[temp[5]]).TotalMilliseconds) + packetspersecond[temp[5]] ) / 2));

                    packetspersecondbuild[temp[5]] = DateTime.Now;

                    if (debugmavlink)
                        DebugPacket(temp);

                    //Console.WriteLine("Packet {0}",temp[5]);
                    // store packet history
                    lock (objlock)
                    {
                        packets[temp[5]] = temp;
                    }

                    if (temp[5] == MAVLink.MAVLINK_MSG_ID_STATUSTEXT) // status text
                    {
                        string logdata = DateTime.Now + " " + Encoding.ASCII.GetString(temp, 6, temp.Length - 6);
                        Console.WriteLine(logdata);
                    }

                    try
                    {
                        if (logfile != null)
                        {
                            byte[] datearray = ASCIIEncoding.ASCII.GetBytes(DateTime.Now.ToBinary() + ":");
                            logfile.BaseStream.Write(datearray,0,datearray.Length);
                            logfile.BaseStream.Write(temp,0,temp.Length);
                            logfile.BaseStream.Write(new byte[] {(byte)'\n'},0,1);
                        }

                    }
                    catch { }
                }
            }
            catch { }

            lastvalidpacket = DateTime.Now;

            Console.Write((DateTime.Now - start).TotalMilliseconds.ToString("00.000") + "\t" + temp.Length + "     \r");

            return temp;
        }

        byte[] readlogPacket()
        {
            byte[] temp = new byte[300];

            sysid = 0;

            int a = 0;
            while (a < temp.Length && logplaybackfile.BaseStream.Position != logplaybackfile.BaseStream.Length)
            {
                temp[a] = (byte)logplaybackfile.BaseStream.ReadByte();
                //Console.Write((char)temp[a]);
                if (temp[a] == ':')
                {
                    break;
                }
                a++;
                if (temp[0] != '-')
                {
                    a = 0;
                }
            }

            //Console.Write('\n');

            //Encoding.ASCII.GetString(temp, 0, a);
            string datestring = Encoding.ASCII.GetString(temp, 0, a);
            //Console.WriteLine(datestring);
            long date = Int64.Parse(datestring);
            DateTime date1 = DateTime.FromBinary(date);

            lastlogread = date1;

            int length = 5;
            a = 0;
            while (a < length)
            {
                temp[a] = (byte)logplaybackfile.BaseStream.ReadByte();
                if (a == 1)
                {
                    length = temp[1] + 6 + 2 + 1;
                }
                a++;
            }

            return temp;
        }

        const int X25_INIT_CRC = 0xffff;
        const int X25_VALIDATE_CRC = 0xf0b8;

        ushort crc_accumulate(byte b, ushort crc)
        {
            unchecked
            {
                byte ch = (byte)(b ^ (byte)(crc & 0x00ff));
                ch = (byte)(ch ^ (ch << 4));
                return (ushort)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
            }
        }

        ushort crc_calculate(byte[] pBuffer, int length)
        {
            if (length < 1)
            {
                return 0xffff;
            }
            // For a "message" of length bytes contained in the unsigned char array
            // pointed to by pBuffer, calculate the CRC
            // crcCalculate(unsigned char* pBuffer, int length, unsigned short* checkConst) < not needed

            ushort crcTmp;
            int i;

            crcTmp = X25_INIT_CRC;

            for (i = 1; i < length; i++) // skips header U
            {
                crcTmp = crc_accumulate(pBuffer[i], crcTmp);
                //Console.WriteLine(crcTmp + " " + pBuffer[i] + " " + length);
            }

            return (crcTmp);
        }


        public static byte[] StructureToByteArray(object obj)
        {

            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;

        }

        public static void ByteArrayToStructure(byte[] bytearray, ref object obj, int startoffset)
        {
            int len = Marshal.SizeOf(obj);

            IntPtr i = Marshal.AllocHGlobal(len);

            try
            {
                Marshal.Copy(bytearray, startoffset, i, len);
            }
            catch (Exception ex) { Console.WriteLine("ByteArrayToStructure FAIL: sourcelen " + bytearray.Length + " startoff " + startoffset + " len " + len + " error " + ex.ToString()); }

            obj = Marshal.PtrToStructure(i, obj.GetType());


            // do endian swap
            byte[] temparray = new byte[bytearray.Length];
            Array.Copy(bytearray, temparray, bytearray.Length);

            object thisBoxed = obj;
            Type test = thisBoxed.GetType();



            int reversestartoffset = startoffset;

            // Enumerate each structure field using reflection.
            foreach (var field in test.GetFields())
            {
                // field.Name has the field's name.

                object fieldValue = field.GetValue(thisBoxed); // Get value

                // Get the TypeCode enumeration. Multiple types get mapped to a common typecode.
                TypeCode typeCode = Type.GetTypeCode(fieldValue.GetType());

                if (typeCode != TypeCode.Object)
                {
                    Array.Reverse(temparray, reversestartoffset, Marshal.SizeOf(fieldValue));
                    reversestartoffset += Marshal.SizeOf(fieldValue);
                }
                else
                {
                    reversestartoffset += ((byte[])fieldValue).Length;
                }

            }
            

            Marshal.Copy(temparray, startoffset, i, len);

            obj = Marshal.PtrToStructure(i, obj.GetType());
            /*
            Console.Write(test.Name + " ");

            foreach (var field in test.GetFields())
            {
                // field.Name has the field's name.

                object fieldValue = field.GetValue(obj); // Get value

                Console.Write(field.Name + "=" + fieldValue.ToString() + " ");
            }
            Console.WriteLine();
            */
            Marshal.FreeHGlobal(i);
        }

        public short swapend11(short value)
        {
            int len = Marshal.SizeOf(value);

            byte[] temp = BitConverter.GetBytes(value);

            Array.Reverse(temp);

            return BitConverter.ToInt16(temp, 0);
        }

        public ushort swapend11(ushort value)
        {
            int len = Marshal.SizeOf(value);

            byte[] temp = BitConverter.GetBytes(value);

            Array.Reverse(temp);

            return BitConverter.ToUInt16(temp, 0);
        }

        public ulong swapend11(ulong value)
        {
            int len = Marshal.SizeOf(value);

            byte[] temp = BitConverter.GetBytes(value);

            Array.Reverse(temp);

            return BitConverter.ToUInt64(temp, 0);
        }

        public float swapend11(float value)
        {
            byte[] temp = BitConverter.GetBytes(value);
            if (temp[0] == 0xff)
                temp[0] = 0xfe;
            Array.Reverse(temp);
            return BitConverter.ToSingle(temp, 0);
        }

        public int swapend11(int value)
        {
            int len = Marshal.SizeOf(value);

            byte[] temp = BitConverter.GetBytes(value);

            Array.Reverse(temp);

            return BitConverter.ToInt32(temp, 0);
        }

        public double swapend11(double value)
        {
            int len = Marshal.SizeOf(value);

            byte[] temp = BitConverter.GetBytes(value);

            Array.Reverse(temp);

            return BitConverter.ToDouble(temp, 0);
        }
    }
}
