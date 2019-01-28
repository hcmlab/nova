using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ssi
{
    public partial class MainHandler
    {
        private void updateMenu()
        {
            AnnoTier tier = AnnoTierStatic.Selected;
            bool hasTier = tier != null;
            SignalTrack track = SignalTrackStatic.Selected;
            bool hasTrack = track != null;
            MediaBox box = MediaBoxStatic.Selected;
            bool hasBox = box != null;
            bool isConnected = DatabaseHandler.IsConnected;
            bool isConnectedAndHasSession = isConnected && DatabaseHandler.IsSession;
            bool hasDatabaseTier = false;
            if (isConnectedAndHasSession && hasTier && tier.AnnoList.Source.HasDatabase)
            {
                hasDatabaseTier = true;
            }
            DatabaseAuthentication authentication = DatabaseHandler.CheckAuthentication();

            // file

            control.fileSaveProjectMenu.IsEnabled = hasTier || hasTrack || hasBox;
            control.exportSamplesMenu.IsEnabled = hasTier && hasTrack;            
            control.exportToGenie.IsEnabled = hasTier;
            control.exportSelectedTrackMenu.IsEnabled = hasTrack;
            control.exportSelectedTierMenu.IsEnabled = hasTier;

            // database
                         
            control.databaseLoadSessionMenu.IsEnabled = isConnected;
            control.databasePasswordMenu.IsEnabled = isConnected;
            control.databaseUpdateMenu.IsEnabled = isConnected;

            control.databaseCMLMenu.IsEnabled = isConnected;
            control.databaseCMLCompleteStepMenu.IsEnabled = isConnectedAndHasSession;
            control.databaseCMLExtractFeaturesMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);
            control.databaseCMLMergeAnnotationsMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);
            control.databaseCMLTrainAndPredictMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);            

            control.databaseAdminMenu.Visibility = isConnected && (authentication > DatabaseAuthentication.READWRITE) ? Visibility.Visible : Visibility.Collapsed;
            control.databaseManageUsersMenu.Visibility = isConnected && (authentication > DatabaseAuthentication.DBADMIN) ? Visibility.Visible : Visibility.Collapsed;


            control.fusionmenu.Visibility = (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\bayesfusion.exe") == true) ? Visibility.Visible : Visibility.Collapsed;

            // annotation
            control.annoNewMenu.IsEnabled = ((Time.TotalDuration > 0) == true);
            control.annoSaveAllMenu.IsEnabled = hasTier;
            control.annoSaveMenu.IsEnabled = hasTier;
            control.annoSaveAsFinishedMenu.IsEnabled = hasTier;
            control.annoReloadMenu.IsEnabled = hasTier;
            control.annoReloadBackupMenu.IsEnabled = hasDatabaseTier;
            control.annoExportMenu.IsEnabled = hasTier;
            control.convertSelectedTierMenu.IsEnabled = hasTier;
            control.convertAnnoContinuousToDiscreteMenu.IsEnabled = hasTier && (tier.IsContinuous || tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE);
            control.removeRemainingSegmentsMenu.IsEnabled = hasTier && tier.IsDiscreteOrFree;
            control.convertAnnoToSignalMenu.IsEnabled = hasTier && tier.IsContinuous;
            control.convertSignalMenu.IsEnabled = hasTrack;
            control.XAIMenu.IsEnabled = hasBox;
            control.XAIMenu.Visibility = control.updatePythonMenu.Visibility = (MainHandler.ENABLE_PYTHON ? Visibility.Visible : Visibility.Collapsed);
            
        }

        private void tierMenu_MouseEnter(object sender, RoutedEventArgs e)
        {
            updateMenu();
        }

        private void helpDocumentationMenu_Click(object sender, RoutedEventArgs e)
        {            
            System.Diagnostics.Process.Start("https://rawgit.com/hcmlab/nova/master/docs/index.html");
        }

        private void helpShortcutsMenu_Click(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Process.Start("https://rawgit.com/hcmlab/nova/master/docs/index.html#shortcut-cheat-sheet");
        }



    }
}
