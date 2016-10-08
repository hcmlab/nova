using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseEditSubjectWindow.xaml
    /// </summary>
    public partial class DatabaseEditSubjectWindow : Window
    {

        private MongoClient mongo;
        IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        public DatabaseEditSubjectWindow(string subject)
        {

            InitializeComponent();

            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.Database);
            var session = database.GetCollection<BsonDocument>(Properties.Settings.Default.LastSessionId);

            List<BsonDocument> documents;
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("document", "subject") & builder.Eq("name", subject);
            documents = session.Find(filter).ToList();


            try
            {
                Namefield.Text =  documents[0]["name"].ToString();
            }
            catch { }
           
            

        }
    }
}
