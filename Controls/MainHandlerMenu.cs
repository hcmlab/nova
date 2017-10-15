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
            control.exportSelectedTrackMenu.IsEnabled = hasTier;
            control.exportSelectedTierMenu.IsEnabled = hasTier;

            // database
                         
            control.databaseLoadSessionMenu.IsEnabled = isConnected;
            control.databasePasswordMenu.IsEnabled = isConnected;

            control.databaseCMLMenu.IsEnabled = isConnected;
            control.databaseCMLCompleteStepMenu.IsEnabled = isConnectedAndHasSession;
            control.databaseCMLExtractFeaturesMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);
            control.databaseCMLMergeAnnotationsMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READWRITE);
            control.databaseCMLTrainMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);
            control.databaseCMLPredictMenu.IsEnabled = isConnected && (authentication > DatabaseAuthentication.READ);

            control.databaseAdminMenu.Visibility = isConnected && (authentication > DatabaseAuthentication.READWRITE) ? Visibility.Visible : Visibility.Collapsed;
            control.databaseManageUsersMenu.Visibility = isConnected && (authentication > DatabaseAuthentication.DBADMIN) ? Visibility.Visible : Visibility.Collapsed;


            control.fusionmenu.Visibility = (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\bayesfusion.exe") == true) ? Visibility.Visible : Visibility.Collapsed;  

            // annotation

            control.annoSaveAllMenu.IsEnabled = hasTier;
            control.annoSaveMenu.IsEnabled = hasTier;
            control.annoSaveAsFinishedMenu.IsEnabled = hasTier;
            control.annoReloadMenu.IsEnabled = hasTier;
            control.annoReloadBackupMenu.IsEnabled = hasDatabaseTier;
            control.annoExportMenu.IsEnabled = hasTier;
            control.convertSelectedTierMenu.IsEnabled = hasTier;
            control.convertAnnoContinuousToDiscreteMenu.IsEnabled = hasTier && tier.IsContinuous;
            control.removeRemainingSegmentsMenu.IsEnabled = hasTier && tier.IsDiscreteOrFree;
            control.convertAnnoToSignalMenu.IsEnabled = hasTier && tier.IsContinuous;
            control.convertSignalMenu.IsEnabled = hasTrack;
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
            MessageBox.Show("Shortcuts:\n\nalt + return to enter fullscreen, esc to close fullscreen\nleftctrl for continuous anno mode, again to close\nalt+click or W on discrete anno to change label/color\nDel on Anno to delete Anno, on tier to delete tier\nalt + right/left to move signalmarker framewise\nshift + alt + right/left to move annomarker framewise\nQ to move signalmarker to start and annomarker to end of selected Segment\nE move annomarker to start and signalmarker to end of selected Segment\na for new Anno between boths markers\nSpace Play/Pause media ", "Quick Reference");
        }



    }
}
