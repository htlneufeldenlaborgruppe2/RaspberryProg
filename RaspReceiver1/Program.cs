
using System;
using Microsoft.Azure.Devices.Client;
using System.IO.Ports;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Linq;
using System.Configuration;

namespace RaspReceiver1
{
    class Program
    {
        static string DEVICEID = "Raspberry11";

        static string url = "http://htlneufelden-datameasurement.azurewebsites.net//api/SampleData/UploadMsg";
        static string portname = "";
        static int baudrate = 115200;
        static SerialPort port;


       
        static System.Globalization.NumberFormatInfo formatInfo;

        static void Main(string[] args)
        {
            ConstantConf conf = Newtonsoft.Json.JsonConvert.DeserializeObject<ConstantConf>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"appsettings.json")));
            DEVICEID = conf.deviceid;
            url = conf.uploadurl;
            portname = conf.comport;
            baudrate = conf.baudrate;

            var currentCulture = System.Globalization.CultureInfo.InstalledUICulture;
            var numberFormat = (System.Globalization.NumberFormatInfo)currentCulture.NumberFormat.Clone();
            numberFormat.NumberDecimalSeparator = ".";
            formatInfo = numberFormat;
            Console.WriteLine("Hello World!");
            //port = new SerialPort("COM3", 115200)
            port = new SerialPort(portname, baudrate)
            {
                DataBits = 8,
                Parity = Parity.None,

            };

            port.DataReceived += (o, e) => Task.Run(() => Port_DataReceived(o, e));
            port.Open();
            
           
            
            Timer t = new Timer((p2) =>
            {
                SerialPort p1 = p2 as SerialPort;
                port.Write("send\n");
          
                Console.Write("Request sent.");
            }, port, 0,60000);
            Console.ReadLine();
        }

        private static void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try {
                Thread.Sleep(5000);
                //int bytesToRead = port.BytesToRead;
                //byte[] buffer = new byte[bytesToRead];
                // port.Read(buffer, 0, bytesToRead);
                //string data = Encoding.ASCII.GetString(buffer);
                string data = port.ReadLine();
                port.DiscardOutBuffer();
            port.DiscardInBuffer();
                Console.WriteLine("data: " + data);

            Dictionary<string, object> returnDict = new Dictionary<string, object>();
            returnDict.Add("deviceID", DEVICEID);
            returnDict.Add("timesent", DateTime.Now);

            foreach (var item in data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] splitBursch = item.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitBursch[0] == "noisevalues")
                {
                    //string[] valuesSplit = splitBursch[1].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                    //double[] valuesDouble = new double[valuesSplit.Length];

                    //for (int i = 0; i < valuesSplit.Length; i++)
                    //{
                    //    if (Double.TryParse(valuesSplit[i], System.Globalization.NumberStyles.Any, formatInfo, out double res1))
                    //    {
                    //        valuesDouble[i] = res1;
                    //    }
                    //}
                    //var templist = valuesDouble.ToList();
                    //templist.Sort();
                    //valuesDouble = templist.ToArray();
                    //(double quartil1, double median, double quartil3) = Quartiles(valuesDouble);
                    //returnDict.Add("noisequartal1", quartil1);
                    //returnDict.Add("noisemedian", median);
                    //returnDict.Add("noisequartal3", quartil3);

                }
                else if (Double.TryParse(splitBursch[1], System.Globalization.NumberStyles.Any, formatInfo, out double res))
                {
                    returnDict.Add(splitBursch[0], res);
                }
                else
                {
                    Console.WriteLine("Failed parsing to double: '" + splitBursch[1] + "' at property '" + splitBursch[0] + "'");
                    Console.WriteLine("At data: " + data);
                }

            }
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(returnDict));
                if (returnDict.Count > 7)
                {
                   
                    SendToApi(Newtonsoft.Json.JsonConvert.SerializeObject(returnDict));
                }
                else
                {
                    Console.WriteLine("Sending was cancelled due to weird values in dictionary");
                }
                

            }
            catch (Exception er)
            {
                Console.WriteLine("*************************");
                Console.WriteLine(er.Message);
                Console.WriteLine("*************************");
            }
        }

        
        private static void SendToApi(string json)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Console.WriteLine("Response\n" + new StreamReader(httpResponse.GetResponseStream()).ReadToEnd());
            }
            catch(Exception e)
            {
                Console.WriteLine("*************************");
                Console.WriteLine(e.Message);
                Console.WriteLine("*************************");
            }
           
        }


        //private static void SendToIotHub(string json)
        //{
        //    _deviceclient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Mqtt);
        //    Console.WriteLine(json);
        //    Message message = new Message(Encoding.ASCII.GetBytes(json));
        //    _deviceclient.SendEventAsync(message);
        //}

        internal static Tuple<double, double, double> Quartiles(double[] afVal)
        {
            int iSize = afVal.Length;
            int iMid = iSize / 2; //this is the mid from a zero based index, eg mid of 7 = 3;

            double fQ1 = 0;
            double fQ2 = 0;
            double fQ3 = 0;

            if (iSize % 2 == 0)
            {
                //================ EVEN NUMBER OF POINTS: =====================
                //even between low and high point
                fQ2 = (afVal[iMid - 1] + afVal[iMid]) / 2;

                int iMidMid = iMid / 2;

                //easy split 
                if (iMid % 2 == 0)
                {
                    fQ1 = (afVal[iMidMid - 1] + afVal[iMidMid]) / 2;
                    fQ3 = (afVal[iMid + iMidMid - 1] + afVal[iMid + iMidMid]) / 2;
                }
                else
                {
                    fQ1 = afVal[iMidMid];
                    fQ3 = afVal[iMidMid + iMid];
                }
            }
            else if (iSize == 1)
            {
                //================= special case, sorry ================
                fQ1 = afVal[0];
                fQ2 = afVal[0];
                fQ3 = afVal[0];
            }
            else
            {
                //odd number so the median is just the midpoint in the array.
                fQ2 = afVal[iMid];

                if ((iSize - 1) % 4 == 0)
                {
                    //======================(4n-1) POINTS =========================
                    int n = (iSize - 1) / 4;
                    fQ1 = (afVal[n - 1] * .25) + (afVal[n] * .75);
                    fQ3 = (afVal[3 * n] * .75) + (afVal[3 * n + 1] * .25);
                }
                else if ((iSize - 3) % 4 == 0)
                {
                    //======================(4n-3) POINTS =========================
                    int n = (iSize - 3) / 4;

                    fQ1 = (afVal[n] * .75) + (afVal[n + 1] * .25);
                    fQ3 = (afVal[3 * n + 1] * .25) + (afVal[3 * n + 2] * .75);
                }
            }

            return new Tuple<double, double, double>(fQ1, fQ2, fQ3);
        }
    }
}
