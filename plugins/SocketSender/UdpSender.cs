using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class UdpSender
    {
        UdpClient client;

        public string[] dependencies(Dictionary<string, object> parameters)
        {
            return null;
        }

        public string open(Dictionary<string,object> parameters)
        {
            try
            {
                string host = (string)parameters["host"];
                int port = int.Parse((string)parameters["port"]);
                client = new UdpClient(host, port);

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
                string label = (string)parameters["label"];
                byte[] bytes = Encoding.ASCII.GetBytes(label);

                client.Send(bytes, bytes.Length);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }

        public string update_leave(Dictionary<string, object> parameters)
        {
            return null;
        }

        public string close(Dictionary<string, object> parameters)
        {
            try
            {                
                client.Close();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return null;
        }
    }
}
