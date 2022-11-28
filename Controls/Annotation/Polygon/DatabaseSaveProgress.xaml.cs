using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace ssi.Controls.Annotation.Polygon
{
    /// <summary>
    /// Interaktionslogik für PolygonProgress.xaml
    /// </summary>
    public partial class DatabaseSaveProgress : Window
    {
        private AnnoList annoList;
        private BsonDocument schemeDoc;
        private IMongoCollection<BsonDocument> annotationData;

        public DatabaseSaveProgress(AnnoList annoList, BsonDocument schemeDoc, IMongoCollection<BsonDocument> annotationData)
		{
			InitializeComponent();
			this.annoList = annoList;
			this.schemeDoc = schemeDoc;
			this.annotationData = annotationData;

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
        }


        void worker_DoWork(object sender, DoWorkEventArgs e)
		{
            const long MAX_DOCUMENT_SIZE = 16000000; // with buffer
            (sender as BackgroundWorker).ReportProgress(1);
            List<AnnoList> fittedParts = DatabaseHandler.splitDataInFittingParts(annoList, schemeDoc, MAX_DOCUMENT_SIZE);
            (sender as BackgroundWorker).ReportProgress(8);
            BsonArray data = DatabaseHandler.AnnoListToBsonArray(fittedParts[0], schemeDoc);
            (sender as BackgroundWorker).ReportProgress(15);
            BsonDocument newAnnotationDataDoc = new BsonDocument();
            (sender as BackgroundWorker).ReportProgress(16);
            newAnnotationDataDoc.Add(new BsonElement("_id", annoList.Source.Database.DataOID));
            newAnnotationDataDoc.Add("labels", data);
            (sender as BackgroundWorker).ReportProgress(17);
            ObjectId nextId = ObjectId.GenerateNewId();
            (sender as BackgroundWorker).ReportProgress(18);
            newAnnotationDataDoc.Add("nextEntry", nextId);
            (sender as BackgroundWorker).ReportProgress(19);
            annotationData.InsertOne(newAnnotationDataDoc);
            (sender as BackgroundWorker).ReportProgress(20);

            double percentagePerStep = 80 / (fittedParts.Count - 1) / 4;
            double currentPercentage = 20;
            for (int i = 1; i < fittedParts.Count; i++)
            {
                currentPercentage = updateProgress(currentPercentage, percentagePerStep, sender);
                ObjectId currentId = nextId;
                data = DatabaseHandler.AnnoListToBsonArray(fittedParts[i], schemeDoc);
                currentPercentage = updateProgress(currentPercentage, percentagePerStep, sender);
                newAnnotationDataDoc = new BsonDocument();
                newAnnotationDataDoc.Add(new BsonElement("_id", currentId));
                newAnnotationDataDoc.Add("labels", data);
                currentPercentage = updateProgress(currentPercentage, percentagePerStep, sender);
                if (i + 1 != fittedParts.Count)
                {
                    nextId = ObjectId.GenerateNewId();
                    newAnnotationDataDoc.Add("nextEntry", nextId);
                }
                annotationData.InsertOne(newAnnotationDataDoc);
                currentPercentage = updateProgress(currentPercentage, percentagePerStep, sender);
            }
            (sender as BackgroundWorker).ReportProgress(100);
            this.Dispatcher.BeginInvoke(new Action(() => this.Close()));
        }

        private double updateProgress(double currentPercentage, double percentagePerStep, object sender)
        {
            for(int i = 0; i < percentagePerStep; i++)
            {
                currentPercentage += 1;
                (sender as BackgroundWorker).ReportProgress((int)currentPercentage);

            }

            return currentPercentage;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			pbStatus.Value = e.ProgressPercentage;
		}
	}
}
