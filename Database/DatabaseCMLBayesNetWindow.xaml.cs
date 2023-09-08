using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLBayesNetWindow : Window
    {
        private MainHandler handler;

        private string tempTrainerPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        private int TrainerPathComboBoxindex = -1;
        int SheetRows = 0;


        private List<SchemeRoleAnnotator> selectedAnnotations = new List<SchemeRoleAnnotator>();

        public DatabaseCMLBayesNetWindow(MainHandler handler)
        {
            InitializeComponent();

            this.handler = handler;

            Loaded += DatabaseCMLTrainAndPredictWindow_Loaded;

            HelpTrainLabel.Content = "Filename of the Trainingsset.\r\n\rAnnotations are chunked in frames of this size (in ms).\r\n\rDiscretisize continuous Annotations\r\n\rContinous Annotations are discretized in these classes (seperate by ;)\r\n\rTimesteps (0 for static)\r\n\rIf checked, node format is role__scheme, else annotations are added up ";
       
       
                    Title = "Train Bayesian Network  ";
                    ApplyButton.Content = "Create Samples";
                    ApplyButton2.Content = "Train";
                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    TrainOptionsPanel.Visibility = Visibility.Visible;
                    ForceCheckBox.Visibility = Visibility.Visible;
                   





        }

        private void DatabaseCMLTrainAndPredictWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetDatabases(DatabaseHandler.DatabaseName);
            GetAnnotators();
            GetRoles();
            GetSchemes();
            parseFiles();
            GetSessions();
            ApplyButton.Focus();
            Update();
        }


        private void parseFiles()
        {
            try
            {
                string cmlfolderpath = Properties.Settings.Default.CMLDirectory + "\\" +
                  Defaults.CML.FusionFolderName + "\\" +
                  Defaults.CML.FusionBayesianNetworkFolderName + "\\";

                string trainingsetpath = cmlfolderpath + "training.set";
                StreamReader reader = File.OpenText(trainingsetpath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(':');
                    SchemeRoleAnnotator sap = new SchemeRoleAnnotator { Name = items[0], Annotator = items[1], Role = items[2], Classes = Int32.Parse(items[3]) };
                    AnnotationSelectionBox.Items.Add(sap);
                }

                AnnotationSelectionBox.SelectAll();


            }
            catch { }
        }




        private void Apply2_Click(object sender, RoutedEventArgs e)
        {
            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                 Defaults.CML.FusionFolderName + "\\" +
                 Defaults.CML.FusionBayesianNetworkFolderName + "\\" + NetworkSelectionBox.SelectedItem + ".xdsl";

            string datasetDir = Properties.Settings.Default.CMLDirectory + "\\" +
               Defaults.CML.FusionFolderName + "\\" +
               Defaults.CML.FusionBayesianNetworkFolderName + "\\" + namebox.Text;

            int tempsteps;
            int.TryParse(timestepsbox.Text, out tempsteps);

            bool isdynamic = tempsteps > 0 ? true : false;

            logTextBox.Text += handler.CMLTrainBayesianNetwork(networkrDir, datasetDir, isdynamic);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.SettingCMLDefaultBN = NetworkSelectionBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultAnnotator = AnnotatorsBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultRole = RolesBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultScheme = SchemesBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultTrainer = NetworkSelectionBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            bool force = ForceCheckBox.IsChecked.Value;

            string database = DatabaseHandler.DatabaseName;

            var sessions = SessionsBox.SelectedItems;

            logTextBox.Text = "";

            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                Defaults.CML.FusionFolderName + "\\" +
                Defaults.CML.FusionBayesianNetworkFolderName + "\\" + NetworkSelectionBox.SelectedItem + ".xdsl";

            string datasetDir = Properties.Settings.Default.CMLDirectory + "\\" +
               Defaults.CML.FusionFolderName + "\\" +
               Defaults.CML.FusionBayesianNetworkFolderName + "\\" + namebox.Text;

            double chunksizeinMS;
            double.TryParse(chunksizebox.Text, out chunksizeinMS);

            int tempsteps;
            int.TryParse(timestepsbox.Text, out tempsteps);

            bool isdynamic = tempsteps > 0 ? true : false;

            if (File.Exists(datasetDir) && ForceCheckBox.IsChecked == false)
            {
                // logTextBox.Text = "dataset exists, skip.\n";
                logTextBox.Text += "\nData sheet exits, check force to overwrite";
                //  logTextBox.Text += handler.CMLTrainBayesianNetwork(networkrDir, datasetDir, isdynamic);
                return;
            }

            File.Delete(datasetDir);

            bool ishead = true;
            foreach (DatabaseSession session in SessionsBox.SelectedItems)
            {
                List<AnnoList> annoLists = new List<AnnoList>();
                foreach (SchemeRoleAnnotator item in AnnotationSelectionBox.Items)
                {
                    DatabaseRole role = DatabaseHandler.Roles.Find(r => r.Name == item.Role);
                    DatabaseScheme scheme = DatabaseHandler.Schemes.Find(m => m.Name == item.Name);
                    ObjectId annotatorID = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Id;

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("scheme_id", scheme.Id) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", role.Id) & builder.Eq("session_id", session.Id);
                    List<DatabaseAnnotation> list = DatabaseHandler.GetAnnotations(filter);
                    foreach (DatabaseAnnotation anno in list)
                    {
                        AnnoList annolist = DatabaseHandler.LoadAnnoList(anno.Id);
                        if(annolist.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                        {
                            for(int i=0; i < item.Classes; i++ )
                            {
                                annolist.Scheme.Labels.Add(new AnnoScheme.Label("s" +(i+1).ToString(), System.Windows.Media.Colors.Black))
;                            }
                           
                        }
                       
                        annoLists.Add(annolist);
                        logTextBox.Text = logTextBox.Text + "Session: " + session.Name + " Role: " + annolist.Meta.Role + " Scheme: " + annolist.Scheme.Name + "\n";

                        logTextBox.Focus();
                        logTextBox.CaretIndex = logTextBox.Text.Length;
                        logTextBox.ScrollToEnd();
                    }
                }
                logTextBox.Text = logTextBox.Text + "----------------------------------\n";

                if (rolecheckbox.IsChecked == true)
                {
                    ExportFrameWiseAnnotations(chunksizeinMS, ';', "REST", datasetDir, annoLists, ishead, session.Name, tempsteps);
                }
                else

                {
                    ExportFrameWiseAnnotationsRolesSeperated(chunksizeinMS, ';', "REST", datasetDir, annoLists, ishead, session.Name, tempsteps);
                }

                if (ishead) ishead = false;
            }

            string[] pairs = new string[AnnotationSelectionBox.Items.Count];
            int s = 0;
            foreach (SchemeRoleAnnotator item in AnnotationSelectionBox.Items)
            {
                pairs[s] = item.Name + ":" + item.Annotator + ":" + item.Role + ":" + item.Classes;
                s++;
            }

            string cmlfolderpath = Properties.Settings.Default.CMLDirectory + "\\" +
                  Defaults.CML.FusionFolderName + "\\" +
                  Defaults.CML.FusionBayesianNetworkFolderName + "\\";

            string trainingsetpath = cmlfolderpath + "training.set";


            System.IO.File.WriteAllLines(trainingsetpath, pairs);


            logTextBox.Text += "\nCreating Data sheet successful\nHit train to train the network or use it in GenIE";
        }

        private void ExportFrameWiseAnnotationsRolesSeperated(double chunksize, char seperator, string restclass, string filepath, List<AnnoList> annoLists, bool ishead, string sessionname, int tempsteps = 0)
        {
     
            string headline = "";
            double maxdur = 0;
          
            bool skipwarning = false;

            string[][] history = new string[annoLists.Count][];

            List<AnnoScheme> schemes = new List<AnnoScheme>();
            List<string> roles = new List<string>();
            List<AnnoList> newLists = new List<AnnoList>();

            for (int a = 0; a < annoLists.Count; a++)
            {
                if (schemes.Find(n => n.Name == annoLists[a].Scheme.Name) == null)
                {
                    schemes.Add(annoLists[a].Scheme);
                    AnnoList list = annoLists[a];
                    newLists.Add(list);
                }

                history[a] = new string[tempsteps];

                if (roles.Find(n => n == annoLists[a].Meta.Role) == null) roles.Add(annoLists[a].Meta.Role);
            }

            for (int a = 0; a < newLists.Count; a++)
            {
               

                if (ishead)
                {
                    headline += newLists[a].Scheme.Name + seperator;

                    for (int i = 0; i < tempsteps; i++)
                    {
                        history[a][i] = restclass;
                        headline += newLists[a].Scheme.Name + "_" + (i+1) + seperator;
                    }
                }
            }
            if (writerolecheckbox.IsChecked == true)
            {
                headline += "role" + seperator;

                for (int i = 0; i < tempsteps; i++)
                {
                    headline += "role_" + (i + 1) + seperator;
                }
            }

            if (storesession.IsChecked == true)
            {
                headline += "session" + seperator;

                for (int i = 0; i < tempsteps; i++)
                {
                    headline += "session_" + (i + 1) + seperator;
                }
            }


       

            for (int a = 0; a < annoLists.Count; a++)
            {
                double localdur = 0;

                if (annoLists[a].Count > 0)
                {
                    localdur = annoLists[a][annoLists[a].Count - 1].Stop * 1000;
                }

                maxdur = Math.Max(maxdur, localdur);
            }

            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
                {
                    if (ishead)
                    {
                        headline = headline.Remove(headline.Length - 1);

                        //  filetoprint += headline + "\n";
                        file.WriteLine(headline);
                        string[] split = headline.Split(seperator);
                        SheetRows = split.Length;
                    }

             


                    headline = "";

                    List<AnnoList> newlists = new List<AnnoList>();

                    double targetsr = 1000.0 / chunksize;

                    foreach (AnnoList al in annoLists)
                    {
                        if (al.Scheme.Type == AnnoScheme.TYPE.DISCRETE || al.Scheme.Type == AnnoScheme.TYPE.FREE)
                        {
                            AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxdur, restclass);
                            newlists.Add(list);
                        }
                        else if (al.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                        {
                            if (al.Scheme.SampleRate != targetsr)
                            {
                                AnnoList newlist = ResampleContinuousList(al, targetsr);
                                if (newlist == null)
                                {
                                    MessageBox.Show("Samplerates do not fit!");
                                    return;
                                }
                                newlists.Add(newlist);
                            }
                            else
                            {
                                newlists.Add(al);
                            }
                        }
                    }

                    double minSize = double.MaxValue;
                    foreach (AnnoList a in newlists)
                    {
                        if (a.Count < minSize) minSize = a.Count;
                    }

                    string discretelabel = "";

                    foreach (string role in roles)
                    {
                        for (int a = 0; a < newlists.Count; a++)
                        {
                            for (int i = 0; i < tempsteps; i++)
                            {
                                if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                                {
                                    double value = newlists[a][i].Score;
                                    discretelabel = discretize(value, newlists[a].Scheme.MinScore, newlists[a].Scheme.MaxScore, newlists[a].Scheme.Labels);
                                    history[a][i] = discretelabel;
                                }
                                else if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false)
                                    {
                                    history[a][i] = newlists[a][i].Score.ToString();
                                }

                                    else
                                    {
                                        history[a][i] = newlists[a][i].Label;
                                    }
                            }
                        }

                        for (int i = 0; i < minSize; i++)
                        {
                            for (int a = 0; a < newlists.Count; a++)
                            {
                                if (newlists[a].Meta.Role == role)
                                {
                                    if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                                    {
                                        double value = newlists[a][i].Score;

                                        discretelabel = discretize(value, newlists[a].Scheme.MinScore, newlists[a].Scheme.MaxScore, newlists[a].Scheme.Labels);
                                        headline += discretelabel + seperator;
                                    }

                                    else if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false)
                                    {
                                        headline += newlists[a][i].Score + seperator;
                                    }

                                    else 
                                    {
                                        headline += newlists[a][i].Label + seperator;
                                    }

                                    if (tempsteps > 0)
                                    {
                                        for (int k = 0; k < tempsteps; k++)
                                        {
                                            headline += history[a][k] + seperator;
                                        }

                                        for (int k = tempsteps - 1; k > 0; k--)
                                        {
                                            history[a][k] = history[a][k - 1];
                                        }

                                        if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                                        {
                                            history[a][0] = discretelabel;
                                        }
                                        else if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false)
                                        {
                                            history[a][0] = newlists[a][i].Score.ToString();
                                        }

                                        else
                                        {
                                            history[a][0] = newlists[a][i].Label;
                                        }
                                    }
                                }
                            }

                            Action EmptyDelegate = delegate () { };
                            this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                            if (writerolecheckbox.IsChecked == true)
                            {
                                headline += role.ToUpper() + seperator;
                                for (int j = 0; j < tempsteps; j++)
                                {
                                    headline += role.ToUpper() + seperator;
                                }
                            }

                            if (storesession.IsChecked == true)
                            {
                                headline += sessionname.ToUpper() + seperator;
                                for (int j = 0; j < tempsteps; j++)
                                {
                                    headline += sessionname.ToUpper() + seperator;
                                }
                            }

                            

                       headline = headline.Remove(headline.Length - 1);

                            string[] splitline = headline.Split(seperator);

                            if (splitline.Length == SheetRows)
                            {
                                file.WriteLine(headline);
                            }
                            else
                            {
                                skipwarning = true;
                            }

                            headline = "";
                        }

                       
                    }
                    
                }

                if(skipwarning) logTextBox.Text += "Some Annotations are missing. They have been skipped\n";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create Sampled Annotations Data File! " + ex, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "Rest")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
            double currentpos = 0;

            bool foundlabel = false;

            while (currentpos < end)
            {
                foundlabel = false;
                foreach (AnnoListItem orgitem in annolist)
                {
                    if (orgitem.Start * 1000 < currentpos && orgitem.Stop * 1000 > currentpos)
                    {
                        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                        result.Add(ali);
                        foundlabel = true;
                        break;
                    }
                }

                if (foundlabel == false)
                {
                    AnnoListItem ali = new AnnoListItem(currentpos, chunksize, restclass);
                    result.Add(ali);
                }

                currentpos = currentpos + chunksize;
            }

            return result;
        }

        private AnnoList ResampleContinuousList(AnnoList annolist, double targetsr)
        {
            double sr = annolist.Scheme.SampleRate;

            if (sr == targetsr)
            {
                return annolist;
            }

            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;

            //upsample
            if (sr < targetsr)
            {
                double factor = targetsr / sr;
                double round = Math.Round(factor);

                if (factor - round == 0)
                {
                    for (int i = 0; i < annolist.Count; i++)
                    {
                        for (int j = 0; j < round; j++)
                        {
                            AnnoListItem ali = new AnnoListItem(annolist[i].Start + j * (1.0 / round), (1.0 / round), annolist[i].Score.ToString());

                            result.Add(ali);
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            //downsample
            else if (sr > targetsr)
            {
                double factor = sr / targetsr;
                double round = Math.Round(factor);
                for (int i = 0; i < annolist.Count; i = i + (int)round)
                {
                    result.Add(annolist[i]);
                }
            }

            return result;
        }

        private void ExportFrameWiseAnnotations(double chunksize, char seperator, string restclass, string filepath, List<AnnoList> annoLists, bool ishead, string sessionname, int tempsteps = 0)
        {
            string headline = "";
            double maxdur = 0;
          
            bool skipwarning = false;

            string[][] history = new string[annoLists.Count][];

            for (int a = 0; a < annoLists.Count; a++)
            {
                history[a] = new string[tempsteps];

                if (ishead)
                {
                    headline += annoLists[a].Meta.Role + "_" + annoLists[a].Scheme.Name + seperator;

                    for (int i = 0; i < tempsteps; i++)
                    {
                        history[a][i] = restclass;
                        headline += annoLists[a].Meta.Role + "_" + annoLists[a].Scheme.Name + "_" + (i+1) + seperator;
                    }
                }

              
                double localdur = 0;

                if (annoLists[a].Count > 0)
                {
                    localdur = annoLists[a][annoLists[a].Count - 1].Stop * 1000;
                }

                maxdur = Math.Max(maxdur, localdur);
            }

            if (storesession.IsChecked == true)
            {
                headline += "session" + seperator;

                for (int i = 0; i < tempsteps; i++)
                {
                    headline += "session_" + (i + 1) + seperator;
                }
            }

            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
                {
                    if (ishead)
                    {
                        headline = headline.Remove(headline.Length - 1);

                        file.WriteLine(headline);
                        string[] split = headline.Split(seperator);
                        SheetRows = split.Length;
                    }

                  



                    headline = "";

                    List<AnnoList> newlists = new List<AnnoList>();

                    double targetsr = 1000.0 / chunksize;

                    foreach (AnnoList al in annoLists)
                    {
                        if (al.Scheme.Type == AnnoScheme.TYPE.DISCRETE || al.Scheme.Type == AnnoScheme.TYPE.FREE)
                        {
                            AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxdur, restclass);
                            newlists.Add(list);
                        }
                        else if (al.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                        {
                            if (al.Scheme.SampleRate != targetsr)
                            {
                                AnnoList newlist = ResampleContinuousList(al, targetsr);
                                if (newlist == null)
                                {
                                    MessageBox.Show("Samplerates do not fit!");
                                    return;
                                }
                                newlists.Add(newlist);
                            }
                            else
                            {
                                newlists.Add(al);
                            }
                        }
                    }

                    double minSize = double.MaxValue;
                    foreach (AnnoList a in newlists)
                    {
                        if (a.Count < minSize) minSize = a.Count;
                    }

                    AnnoList session = new AnnoList();
                    for (int i = 0; i < minSize; i++)
                    {
                        session.Add(new AnnoListItem(0, 1, sessionname));
                    }
                    newlists.Add(session);

                    string discretelabel = "";

                    for (int a = 0; a < newlists.Count; a++)
                    {
                        for (int i = 0; i < tempsteps; i++)
                        {
                            if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                            {
                                double value = newlists[a][i].Score;
                                discretelabel = discretize(value, newlists[a].Scheme.MinScore, newlists[a].Scheme.MaxScore, newlists[a].Scheme.Labels);
                                history[a][i] = discretelabel;
                            }

                            else if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false)
                            {
                                history[a][i] = newlists[a][i].Score.ToString();
                            }

                            else
                            {
                                history[a][i] = newlists[a][i].Label;
                            }
                        }
                    }

                    for (int i = 0; i < minSize; i++)
                    {
                        for (int a = 0; a < newlists.Count; a++)
                        {
                            if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                            {
                                double value = newlists[a][i].Score;

                                discretelabel = discretize(value, newlists[a].Scheme.MinScore, newlists[a].Scheme.MaxScore, newlists[a].Scheme.Labels);
                                headline += discretelabel + seperator;
                            }

                            else if ((newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false))
                            {
                                headline += newlists[a][i].Score + seperator;
                            }

                            else
                            {
                                headline += newlists[a][i].Label + seperator;
                            }

                            if (tempsteps > 0)
                            {
                                for (int k = 0; k < tempsteps; k++)
                                {
                                    headline += history[a][k] + seperator;
                                }

                                for (int k = tempsteps - 1; k > 0; k--)
                                {
                                    history[a][k] = history[a][k - 1];
                                }

                                if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == true)
                                {
                                    history[a][0] = discretelabel;
                                }
                                else if (newlists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && discretisizeeckbox.IsChecked == false)
                                {
                                    history[a][0] = newlists[a][i].Score.ToString();
                                }

                                else
                                {
                                    history[a][0] = newlists[a][i].Label;
                                }
                            }
                        }

                     
                        headline = headline.Remove(headline.Length - 1);

                        string[] splitline = headline.Split(seperator);

                        if (splitline.Length == SheetRows)
                        {
                            file.WriteLine(headline);
                        }
                        else
                        {
                            skipwarning = true;
                        }

                        headline = "";


                    }

                    Action EmptyDelegate = delegate () { };
                    this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create Sampled Annotations Data File! " + ex, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string discretize(double value, double MinScore, double MaxScore, List<AnnoScheme.Label> classes)
        {
            int numclasses = classes.Count;

            double range = MaxScore - MinScore;

            double chunk = range / numclasses; 

            if(double.IsNaN(value))
            {
                return "REST";
            }
            else
            {
                for (int i = 0; i < numclasses; i++)
                {
                    if (value >= (MinScore + i * chunk) && value < (MinScore + (i + 1) * chunk))
                    {
                        return classes[i].Name;
                    }
                }
            }

            return classes[classes.Count - 1].Name;
        }

        private void Select(ListBox list, string select)
        {
            if (select != null)
            {
                foreach (string item in list.Items)
                {
                    if (item == select)
                    {
                        list.SelectedItem = item;
                    }
                }
            }
        }

        public void GetDatabases(string selectedItem = null)
        {
            DatabasesBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases();

            foreach (string db in databases)
            {
                DatabasesBox.Items.Add(db);
            }

            Select(DatabasesBox, selectedItem);
        }

        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetAnnotators();
                GetRoles();
                GetSchemes();
                GetSessions();
            }

            Update();
        }

        public void GetSchemes()
        {
            SchemesBox.Items.Clear();

            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseScheme> schemesValid = new List<DatabaseScheme>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseScheme scheme in schemes)
            {
                schemesValid.Add(scheme);
            }

            foreach (DatabaseScheme item in schemesValid)
            {
                SchemesBox.Items.Add(item.Name);
            }

            if (SchemesBox.Items.Count > 0)
            {
                if (SchemesBox.SelectedIndex == -1) SchemesBox.SelectedIndex = 0;
                SchemesBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;
            }
            SchemesBox.ScrollIntoView(SchemesBox.SelectedItem);
        }

        public void GetRoles()
        {
            RolesBox.Items.Clear();

            foreach (DatabaseRole item in DatabaseHandler.Roles)
            {
                RolesBox.Items.Add(item.Name);
            }

            if (RolesBox.Items.Count > 0)
            {
                if (RolesBox.SelectedIndex == -1) RolesBox.SelectedIndex = 0;
                RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
            }
        }

        public void GetAnnotators()
        {
            AnnotatorsBox.Items.Clear();

            foreach (DatabaseAnnotator annotator in DatabaseHandler.Annotators)
            {
                AnnotatorsBox.Items.Add(annotator.FullName);
            }

            if (AnnotatorsBox.Items.Count > 0)
            {
                AnnotatorsBox.SelectedIndex = 0;
                AnnotatorsBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
            }

            AnnotatorsBox.ScrollIntoView(AnnotatorsBox.SelectedItem);
        }

        public void GetSessions()
        {
            //if (AnnotationSelectionBox.SelectedItem == null)
            //{
            //   return;
            ////}


            if (AnnotationSelectionBox.HasItems)
            {
                AnnotationSelectionBox.ItemsSource = null;
            }

          
            
                // show sessions for which an annotation exists or is missing

                List<BsonDocument> annotations = new List<BsonDocument>();

            if(AnnotationSelectionBox.Items.Count > 0)
            {

           

                List<string> sessionNames = new List<string>();

                {
                    var item = (SchemeRoleAnnotator)AnnotationSelectionBox.Items[0];
                    string schemeName = item.Name;
                    ObjectId schemeID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, schemeName);
                    string roleName = item.Role;
                    ObjectId roleID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, roleName);

                    string annotatorName = "";
                    DatabaseAnnotator annotator = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator);
                    if(annotator != null)
                    {
                        annotatorName = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Name;
                    }


                    ObjectId annotatorID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotatorName);

                    var builder = Builders<BsonDocument>.Filter;

                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID);

                    annotations.AddRange(DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, true, filter));

                    foreach (BsonDocument annotation in annotations)
                    {
                        string sessionName = "";
                        DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
                        if (sessionName != "" && !sessionNames.Contains(sessionName))
                        {
                            sessionNames.Add(sessionName);
                        }
                    }
                }

                for (int i = 1; i < AnnotationSelectionBox.Items.Count; i++)
                {
                    List<BsonDocument> annotationstemp = new List<BsonDocument>();
                    List<string> sessionNamestemp = new List<string>();
                    var item = (SchemeRoleAnnotator)AnnotationSelectionBox.Items[i];
                    string schemeName = item.Name;
                    ObjectId schemeID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, schemeName);
                    string roleName = item.Role;
                    ObjectId roleID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, roleName);

                    string annotatorName = "";
                    var test = DatabaseHandler.Annotators;
                    DatabaseAnnotator annotator = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator);
                    if (annotator != null)
                    {
                        annotatorName = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Name;
                    }
                    ObjectId annotatorID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotatorName);

                    var builder = Builders<BsonDocument>.Filter;

                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID);

                     annotationstemp.AddRange(DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, true, filter));

                    foreach (BsonDocument annotation in annotationstemp)
                    {
                        string sessionName = "";
                        DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
                        if (sessionName != "" && !sessionNamestemp.Contains(sessionName))
                        {
                            sessionNamestemp.Add(sessionName);
                        }
                    }

                    sessionNames.RemoveAll(thing => !sessionNamestemp.Contains(thing));
                }

            List<DatabaseSession> sessions = new List<DatabaseSession>();
                foreach (string sessionName in sessionNames)
                {
                    DatabaseSession session = new DatabaseSession() { Name = sessionName };
                    if (DatabaseHandler.GetSession(ref session))
                    {
                        sessions.Add(session);
                    }
                }

                SessionsBox.ItemsSource = sessions.OrderBy(s => s.Name).ToList();
           

            if (SessionsBox.HasItems)
            {
                if (SessionsBox.SelectedItem == null)
                {
                    SessionsBox.SelectedIndex = 0;
                }
            }
            }


        }

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrainerPathComboBoxindex = NetworkSelectionBox.SelectedIndex;
            NetworkSelectionBox.Items.Clear();
            List<string> nets = getBayesianNetworks();
            foreach (string net in nets)
                if (SessionsBox.HasItems)
                {
                    NetworkSelectionBox.Items.Add(Path.GetFileNameWithoutExtension(net));
                }

            if (TrainerPathComboBoxindex == -1)
            {
                NetworkSelectionBox.SelectedIndex = 0;
                TrainerPathComboBoxindex = NetworkSelectionBox.SelectedIndex;
            }
            else
            {
                NetworkSelectionBox.SelectedIndex = TrainerPathComboBoxindex;
            }

            Update();
        }

        private List<string> getBayesianNetworks()
        {
            List<string> networks = new List<string>();

            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                 Defaults.CML.FusionFolderName + "\\" +
                 Defaults.CML.FusionBayesianNetworkFolderName + "\\";

            if (Directory.Exists(networkrDir))
            {
                //string[] searchDirs = Directory.GetDirectories(networkrDir);
                //foreach (string searchDir in searchDirs)
                //{
                string[] networkFiles = Directory.GetFiles(networkrDir, "*." + Defaults.CML.BayesianNetworkextension);
                foreach (string network in networkFiles)
                {
                    networks.Add(network);
                }
                NetworkSelectionBox.SelectedItem = Properties.Settings.Default.SettingCMLDefaultBN;

                // }
            }

            return networks;
        }

        private void Annotations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //GetSessions();

            Update();
        }

        private void ShowAllSessionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // GetSessions();
        }

        private void ShowAllSessionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // GetSessions();
        }

        

        private void TrainerPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NetworkSelectionBox.SelectedItem != null)
            {
                string trainer = (string)NetworkSelectionBox.SelectedItem;

                string database = "";
                if (DatabasesBox.SelectedItem != null)
                {
                    database = DatabasesBox.SelectedItem.ToString();
                }

                namebox.Text = NetworkSelectionBox.SelectedItem.ToString() + ".txt";

                // TrainerNameTextBox.Text = mode == Mode.COMPLETE ? Path.GetFileName(tempTrainerPath) : database;
            }
        }

        private void Update()
        {
            bool enable = false;

            if (NetworkSelectionBox.Items.Count > 0
                && DatabasesBox.SelectedItem != null
                && SessionsBox.SelectedItem != null
                && RolesBox.SelectedItem != null
                && AnnotatorsBox.SelectedItem != null
                && SchemesBox.SelectedItem != null)
            {
                enable = true;
            }

            ApplyButton.IsEnabled = enable;
            ApplyButton2.IsEnabled = enable;
            TrainOptionsPanel.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            NetworkSelectionBox.IsEnabled = enable;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void AnnotationSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            removePair.IsEnabled = false;
            if (AnnotationSelectionBox.SelectedItems != null)
            {
                removePair.IsEnabled = true;
            }

          //  GetSessions();
        }

        private void removePair_Click(object sender, RoutedEventArgs e)
        {
            selectedAnnotations.Remove((SchemeRoleAnnotator)AnnotationSelectionBox.SelectedItem);
            AnnotationSelectionBox.Items.Remove(AnnotationSelectionBox.SelectedItem);
            AnnotationSelectionBox.SelectAll();
            GetSessions();
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var scheme in SchemesBox.SelectedItems)
            {
                foreach (var role in RolesBox.SelectedItems)
                {

                    int classes = 3; //Default value..
                    AnnoScheme annoscheme = DatabaseHandler.GetAnnotationScheme(scheme.ToString());
                    if(annoscheme.Type == AnnoScheme.TYPE.DISCRETE)
                    {
                        classes = annoscheme.Labels.Count;
                    }
                    

                    SchemeRoleAnnotator stp = new SchemeRoleAnnotator() { Name = scheme.ToString(), Role = role.ToString(), Annotator = AnnotatorsBox.SelectedItem.ToString(), Classes = classes };

                    if (selectedAnnotations.Find(item => item.Role == stp.Role && item.Name == stp.Name) == null)
                    {
                        selectedAnnotations.Add(stp);
                        AnnotationSelectionBox.Items.Add(stp);

                        AnnotationSelectionBox.SelectAll();
                    }
                }
            }

            GetSessions();
        }

        private void discretisizeeckbox_Checked(object sender, RoutedEventArgs e)
        {
           
        }

        private void discretisizeeckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        
        }

        private void rolecheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (writerolecheckbox != null)
            {
                writerolecheckbox.IsEnabled = false;
            }
        }

        private void rolecheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (writerolecheckbox != null)
            {
                writerolecheckbox.IsEnabled = true;
            }
        }

      
    }

    public class SchemeRoleAnnotator
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string Annotator { get; set; }
        public int Classes { get; set; }
    }
}