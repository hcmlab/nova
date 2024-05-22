using System;
using System.IO;
using System.Windows;
using Label = ssi.Types.Label;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using ssi.Types;
using System.Net.WebSockets;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using System.Data;


namespace ssi
{
    public partial class MainHandler
    {
  
        public async Task listenAsync()
        {

            var ws = new WebSocket("wss://relay.damus.io");
            ws.OnMessage += (sender, e) => Console.WriteLine("Received: " + e.Data);

            ws.Connect();

            JArray array = new JArray();
            JArray kinds = new JArray();
            kinds.Add(1);


            JObject filter = new JObject
            { 
                { "kinds", kinds} ,
              
            };

            array.Add("REQ");
            array.Add("asdjnasdlkjashdajskdhasjdasd");
            array.Add(filter);

            Console.WriteLine(array.ToString());
            ws.Send(array.ToString());

            Console.ReadKey(true);
            ws.Close();



        }



    }
}