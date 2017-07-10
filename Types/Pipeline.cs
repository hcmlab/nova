using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ssi
{


    public class Pipeline : IMedia
    {
        /*
         *  Call XMLpipe.exe when annotation contains pipeline flag: example pipeline="bn\laughter\bn.pipeline" (relative to xmlpipe folder)
         *  
         *
         * */

        UdpClient client;


        ////alternative way, set SSI pipeline in foreground, send enter
        //[DllImport("user32.dll")]
        //private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //[DllImport("user32.dll")]
        //private static extern bool SetForegroundWindow(IntPtr hWnd);

        AnnoList annoList;
        string pipelinepath;
        Process process = new Process();

        public Pipeline(AnnoList annoList, string pipelinepath)
        {
            this.annoList = annoList;
            this.pipelinepath = pipelinepath;
            client = new UdpClient("127.0.0.1", 1111);

            //check if xmlpipe is available, else download it from git
            string SSIbinaryGitPath = "https://github.com/hcmlab/ssi/raw/master/bin/x64/vc140/";
            string xmlpipeexe = "xmlpipe.exe";
            string xmlpipeexePath = AppDomain.CurrentDomain.BaseDirectory + xmlpipeexe;


            if (!(File.Exists(xmlpipeexePath)))
            {
                DownloadFile(SSIbinaryGitPath + xmlpipeexe, xmlpipeexePath);
            }

            //call the pipeline in wait state

            CallXMLPipe(pipelinepath);


        }

        ~Pipeline()
        { 
            client.Close();

        }
        public void move(double newPosition, double threshold)
        {
        }


        public string CallXMLPipe(string pipelinepath)
        {
            string result = "";

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName =  "xmlpipe.exe";
                startInfo.Arguments = pipelinepath;
                process.StartInfo = startInfo;
                process.Start();
              
               // process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }


        public void Clear()
        {
            process.Kill();
        }

        public string GetDirectory()
        {
            if (annoList.Source.HasFile)
            {
                return annoList.Source.File.Directory;
            }
            return "";
        }


        public string GetFilepath()
        {
            if (annoList.Source.HasFile)
            {
                return annoList.Source.File.Path;
            }
            return "";
        }

        public double GetLength()
        {
            return annoList[annoList.Count - 1].Stop;
        }

        public MediaType GetMediaType()
        {
            return MediaType.PIPELINE;
        }

        public WriteableBitmap GetOverlay()
        {
            return null;
        }

        public double GetPosition()
        {
            return 0;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public UIElement GetView()
        {
            return null;
        }

        public double GetVolume()
        {
            return 0;
        }

        public void SetVolume(double volume)
        {
        }

        public bool HasAudio()
        {
            return false;
        }

        public void Move(double time)
        {
            if (annoList != null && annoList.Count > 0)
            {
                move(time, 0.2);
            }
        }

        public void Pause()
        {
            ////alternative way, set SSI pipeline in foreground, send enter
            //IntPtr zero = IntPtr.Zero;
            //string folder = AppDomain.CurrentDomain.BaseDirectory + "xmlpipe\\xmlpipe.exe";

            //zero = FindWindow(null, folder);
            //if (zero != IntPtr.Zero)
            //{
            //    SetForegroundWindow(zero);
            //    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            //    System.Windows.Forms.SendKeys.Flush();
            //}

            string message = "SSI:STOP:RUN1\0";
            byte[] bytes = Encoding.ASCII.GetBytes(message);

            client.Send(bytes, bytes.Length);
            Thread.Sleep(3000); 
            CallXMLPipe(pipelinepath);
        }

        public void Play()
        {
                string message = "SSI:STRT:RUN1\0";
                byte[] bytes = Encoding.ASCII.GetBytes(message);

                client.Send(bytes, bytes.Length);

        }

        public void Stop()
        {
            process.Kill();
        }

        public void DownloadFile(string urlAddress, string location)
        {
            using (WebClient webClient = new WebClient())
            {

                Uri URL = new Uri(urlAddress);
                // Start the stopwatch which we will be using to calculate the download speed

                try
                {
                    // Start downloading the file
                    webClient.DownloadFile(URL, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
