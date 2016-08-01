using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using MongoDB;
using MongoDB.Configuration;
using MongoDB.Linq;

namespace ssi
{
    internal class DatabaseHandler
    {

        private IMongoDatabase database;
        private IMongoCollection<Document> labels;
        private Mongo mongo;

        string connectionstring = "Server=127.0.0.1:27017";
        string sessionid = "Test_#Test";
        AnnoList annolist;


        public DatabaseHandler(string constr, string id, AnnoList annol = null)
        {
            this.connectionstring = constr;
            this.annolist = annol;
            this.sessionid = id;
        }

        public void StoretoDatabase()
        {
            mongo = new Mongo(connectionstring);
            mongo.Connect();

            database = mongo[sessionid];
            labels = database.GetCollection<Document>("labels");
            Clean();

            if(annolist != null)
            {
                foreach (AnnoListItem ali in annolist)
                {
                    labels.Insert(new Document { { "Start", ali.Start }, { "Stop", ali.Stop }, { "Label", ali.Label }, { "Confidence", ali.Meta }, { "Tier", ali.Tier }, { "NeedsRelabel", "False" } });
                }
            }

            mongo.Disconnect();
        }

        private void Clean()
        {
            try
            {
                database = mongo[sessionid];
                labels = database.GetCollection<Document>("labels");
                labels.Remove(new Document()); //remove everything from the labels collection.
            }

            catch
            {
                Console.Write("Didnt find collection");
            }
        }


        public void CleanTable(string id)
        {
            mongo = new Mongo(connectionstring);
            mongo.Connect();
            try
            {
                database = mongo[id];
                labels = database.GetCollection<Document>("labels");
                labels.Remove(new Document()); //remove everything from the categories collection.

            }

            catch
            {
                Console.Write("Didnt find collection");
            }

            mongo.Disconnect();
        }


        public AnnoList LoadfromDatabase(string id)
        {
            AnnoList al = new AnnoList();
          

            mongo = new Mongo(connectionstring);
            mongo.Connect();
            database = mongo[id];
            labels = database.GetCollection<Document>("labels");


            var all = labels.FindAll();
            foreach (var doc in all.Documents)
            {
               
                Console.WriteLine(doc.ToString());
                double start = double.Parse(doc.Get("Start").ToString());
                double stop = double.Parse(doc.Get("Stop").ToString());
                double duration = stop - start;
                string Label = doc.Get("Label").ToString();
                string Tier = doc.Get("Tier").ToString();
                string Meta = doc.Get("Confidence").ToString();

                //not handled right now.. todo
                string NeedsRelabel = doc.Get("NeedsRelabel").ToString();

                AnnoListItem ali = new AnnoListItem(start, duration, Label, Meta, Tier);
                al.Add(ali);

            }


            mongo.Disconnect();
            return al;
        }

    }


    public static class OidExtensions
    {
        public static Oid ToOid(this string str)
        {
            if (str.Length == 24)
                return new Oid(str);

            return new Oid(str.Replace("\"", ""));
        }
    }
}
