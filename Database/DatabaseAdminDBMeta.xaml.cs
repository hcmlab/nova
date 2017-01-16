using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseDBMeta.xaml
    /// </summary>
    public partial class DatabaseAdminDBMeta : Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";

        public DatabaseAdminDBMeta()
        {
            InitializeComponent();

            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.DatabaseAddress;
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);

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