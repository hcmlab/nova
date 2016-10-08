using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseDBMeta.xaml
    /// </summary>
    public partial class DatabaseDBMeta : Window
    {
        private MongoClient mongo;
        IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        public DatabaseDBMeta()
        {
            InitializeComponent();
    
            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.Database);



            var session = database.GetCollection<BsonDocument>(Properties.Settings.Default.LastSessionId);

            List<BsonDocument> documents;
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("name", "subject") & builder.Eq("name", "");
            documents = session.Find(filter).ToList();


            try
            {
                Namefield.Text = documents[0]["name"].ToString();
            }
            catch { }

        }
    }
}
