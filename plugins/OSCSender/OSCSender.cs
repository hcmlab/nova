using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SharpOSC;
using Ventuz.OSC;

namespace ssi
{

    enum OSCLibrary
    {
        Ventuz,
        SharpOSC
    }


    public class OSCSender
    {
        //Chose backend library to use
        OSCLibrary library = OSCLibrary.SharpOSC;


        Ventuz.OSC.UdpWriter ventuzclient;
        SharpOSC.UDPSender sharposcclient;

        public string[] dependencies(Dictionary<string, object> parameters)
        {
            if (library == OSCLibrary.SharpOSC)
            {
                return new string[] { "SharpOSC" };
            }
            else
            {
                return new string[] { "Ventuz.OSC" };
            }
        }

        public string open(Dictionary<string,object> parameters)
        {
            try
            {
                string host = (string)parameters["host"];
                int port = int.Parse((string)parameters["port"]);

                if(library == OSCLibrary.SharpOSC)
                {
                    sharposcclient = new SharpOSC.UDPSender(host, port);
                }

                else
                {
                     ventuzclient = new Ventuz.OSC.UdpWriter(host, port);
                }
 
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }            

            return null;
        }

        public string update_enter(Dictionary<string, object> parameters)
        {
            try
            {
                double dur =   (double)parameters["dur"];
                int state = (int)parameters["state"];
                string label = (string)parameters["label"];
                string scheme = (string)parameters["scheme"];
                double time = (double)parameters["time"];


                if (library == OSCLibrary.SharpOSC)
                {
                    var message = new SharpOSC.OscMessage("/evnt", scheme, label, (Int32)(time * 1000), (Int32)(dur * 1000), (Int32)state, (Int32)0);
                    sharposcclient.Send(message);
                }
                else
                {
                    OscElement message = new OscElement("/evnt", scheme, label, (Int32)(time * 1000), (Int32)(dur * 1000), (Int32)state, (Int32)0);
                    ventuzclient.Send(message);
                }

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }


        public string update_leave(Dictionary<string, object> parameters)
        {
            try
            {
                double dur = (double)parameters["dur"];
                int state = (int)parameters["state"];
                string label = (string)parameters["label"];
                string scheme = (string)parameters["scheme"];
                double time = (double)parameters["time"];


                if (library == OSCLibrary.SharpOSC)
                {
                    var message = new SharpOSC.OscMessage("/evnt", scheme, label, (Int32)(time * 1000), (Int32)(dur * 1000), (Int32)state, (Int32)0);
                    sharposcclient.Send(message);
                }
                else
                {
                    OscElement message = new OscElement("/evnt", scheme, label, (Int32)(time * 1000), (Int32)(dur * 1000), (Int32)state, (Int32)0);
                    ventuzclient.Send(message);
                }

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }

        public string close(Dictionary<string, object> parameters)
        {
            try
            {
                if (library == OSCLibrary.SharpOSC)
                {
                    sharposcclient.Close();
                }
                else
                {
                    ventuzclient.Dispose();
                }
              
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }
    }
}
