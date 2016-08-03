using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ssi
{
    internal class DatabaseHandler
    {
        private IMongoDatabase database;
        private IMongoCollection<BsonDocument> annos;
        private MongoClient mongo;
        private string dbname = "simple";
        private string connectionstring = "mongodb://127.0.0.1:27017";

        public DatabaseHandler(string constr)
        {
            this.connectionstring = constr;
        }

        public void StoretoDatabase(string data, AnnoList annolist = null)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(data);
            annos = database.GetCollection<BsonDocument>(annolist.Name);
            Clean(data, annolist.Name);
            bool metawritten = false;

            if (annolist != null)
            {
                foreach (AnnoListItem ali in annolist)
                {
                    if (!metawritten && !annolist.isDiscrete)
                    {
                        annos.InsertOne(new BsonDocument { { "Start", ali.Start }, { "Stop", ali.Stop }, { "Label", ali.Label }, { "Confidence", ali.Meta }, { "Tier", ali.Tier }, { "NeedsRelabel", "False" }, { "LowBorder", annolist.Lowborder }, { "HighBorder", annolist.Highborder } });
                        metawritten = true;
                    }
                    else
                    {
                        annos.InsertOne(new BsonDocument { { "Start", ali.Start }, { "Stop", ali.Stop }, { "Label", ali.Label }, { "Confidence", ali.Meta }, { "Tier", ali.Tier }, { "NeedsRelabel", "False" } });
                    }
                }
            }

            //Todo: Maybe add more tables, eg. for meta information if we want to move from tiers to sessions
        }

        private void Clean(string db, string table)
        {
            try
            {
                database = mongo.GetDatabase(db);
                annos = database.GetCollection<BsonDocument>(table);
                var result = annos.DeleteManyAsync(new BsonDocument());
            }
            catch
            {
                Console.Write("Didnt find collection");
            }
        }

        public AnnoList LoadfromDatabase()
        {
            AnnoList al = new AnnoList();

            mongo = new MongoClient(connectionstring);
            List<string> Databases = new List<string>();
            List<string> Collections = new List<string>();

            var databases = mongo.ListDatabasesAsync().Result.ToListAsync().Result;
            foreach (var c in databases)
            {
                Databases.Add(c.GetElement(0).Value.ToString());
            }

            DataBaseResultsWindow dbw = new DataBaseResultsWindow(Databases);
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.ShouldDelete() == true)
            {
                deletedatabase(dbw.Result());
            }
            else if (dbw.DialogResult == true)
            {
                if (dbw.Result() != null)

                    database = mongo.GetDatabase(dbw.Result());

                var collectionsdb = database.ListCollectionsAsync().Result.ToListAsync().Result;

                foreach (var c in collectionsdb)
                {
                    Collections.Add(c.GetElement(0).Value.ToString());
                }

                DataBaseResultsWindow dbcol = new DataBaseResultsWindow(Collections);
                dbcol.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dbcol.ShowDialog();

                if (dbcol.ShouldDelete() == true)
                {
                    deletecollection(dbw.Result(), dbcol.Result());
                }
                else if (dbcol.DialogResult == true)
                {
                    if (dbcol.Result() != null)
                        annos = database.GetCollection<BsonDocument>(dbcol.Result());
                }

                if (annos != null)
                {
                    bool metaloaded = false;
                    var all = annos.Find(_ => true).ToList();
                    foreach (var doc in all)
                    {
                        if (metaloaded == false)
                        {
                            try
                            {
                                double lowborder = double.Parse(doc.GetValue("LowBorder").ToString());
                                double highborder = double.Parse(doc.GetValue("HighBorder").ToString());
                                al.Lowborder = lowborder;
                                al.Highborder = highborder;
                                metaloaded = true;
                            }
                            catch
                            {
                                metaloaded = true;
                            }
                        }

                        double start = double.Parse(doc.GetValue("Start").ToString());
                        double stop = double.Parse(doc.GetValue("Stop").ToString());
                        double duration = stop - start;
                        string Label = doc.GetValue("Label").ToString();
                        string Tier = doc.GetValue("Tier").ToString();
                        string Meta = doc.GetValue("Confidence").ToString();

                        //not handled right now.. todo
                        string NeedsRelabel = doc.GetValue("NeedsRelabel").ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, Label, Meta, Tier);
                        al.Add(ali);
                    }
                    return al;
                }
            }
            return null;
        }

        public void deletedatabase(string db)
        {
            try
            {
                mongo = new MongoClient(connectionstring);
                var all = mongo.ListDatabases();
                mongo.DropDatabase(db);
            }
            catch
            {
                Console.Write("Didnt find collection");
            }
        }

        public void deletecollection(string db, string collection)

        {
            try
            {
                mongo = new MongoClient(connectionstring);
                var database = mongo.GetDatabase(db);
                database.DropCollectionAsync(collection);
            }
            catch
            {
                Console.Write("Didnt find collection");
            }
        }
    }
}