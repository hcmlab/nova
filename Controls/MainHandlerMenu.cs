using System;
using System.Collections.Generic;
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
            int authentication = DatabaseHandler.CheckAuthentication();

            // file

            control.fileSaveProjectMenu.IsEnabled = hasTier || hasTrack || hasBox;
            control.exportSamplesMenu.IsEnabled = hasTier && hasTrack;            
            control.exportToGenie.IsEnabled = hasTier;
            control.exportSelectedTrackMenu.IsEnabled = hasTier;
            control.exportSelectedTierMenu.IsEnabled = hasTier;

            // database

            Visibility databaseVisibility = isConnected ? Visibility.Visible : Visibility.Collapsed;

            control.databaseCMLCompleteStepMenu.Visibility = databaseVisibility;
            control.databaseCMLTransferStepMenu.Visibility = databaseVisibility;
            control.databaseCMLExtractFeaturesMenu.Visibility = databaseVisibility;
            control.databaseCMLMergeMenu.Visibility = databaseVisibility;
            control.databaseLoadSessionMenu.Visibility = databaseVisibility;
            control.databaseAdminMenu.Visibility = Visibility.Collapsed;

            if (isConnected && authentication > 2)
            {
                control.databaseAdminMenu.Visibility = Visibility.Visible;
                control.databaseManageDBsMenu.Visibility = Visibility.Visible;
                control.databaseManageSessionsMenu.Visibility = Visibility.Visible;

                control.databaseCMLTransferStepMenu.Visibility = Visibility.Visible;
                control.databaseCMLMergeMenu.Visibility = Visibility.Visible;

                if (isConnected && authentication > 3)
                {
                    control.databaseManageUsersMenu.Visibility = Visibility.Visible;
                }
            }

            // annotation

            control.annoSaveAllMenu.IsEnabled = hasTier;
            control.annoSaveMenu.IsEnabled = hasTier;
            control.annoSaveAsMenu.IsEnabled = hasTier;
            control.convertSelectedTierMenu.IsEnabled = hasTier;
            control.convertAnnoContinuousToDiscreteMenu.IsEnabled = hasTier && tier.IsDiscreteOrFree;
            control.convertAnnoToSignalMenu.IsEnabled = hasTier && tier.IsDiscreteOrFree;
            control.convertSignalToAnnoContinuousMenu.IsEnabled = hasTrack;
        }

        private void tierMenu_MouseEnter(object sender, RoutedEventArgs e)
        {
            updateMenu();
        }

        private void helpMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Shortcuts:\n\nalt + return to enter fullscreen, esc to close fullscreen\nleftctrl for continuous anno mode, again to close\nalt+click or W on discrete anno to change label/color\nDel on Anno to delete Anno, on tier to delete tier\nalt + right/left to move signalmarker framewise\nshift + alt + right/left to move annomarker framewise\nQ to move signalmarker to start and annomarker to end of selected Segment\nE move annomarker to start and signalmarker to end of selected Segment\na for new Anno between boths markers\nSpace Play/Pause media ", "Quick Reference");
        }



    }
}
