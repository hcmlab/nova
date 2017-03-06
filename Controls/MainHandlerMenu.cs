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
        private void tierMenu_Click(object sender, RoutedEventArgs e)
        {
            control.saveAnnoMenu.IsEnabled = false;
            control.saveAnnoMenuAs.IsEnabled = false;
            control.convertAnnoContinuousToDiscreteMenu.IsEnabled = false;
            control.convertAnnoToSignalMenu.IsEnabled = false;
            control.exportMenu.IsEnabled = false;            

            AnnoTier a = AnnoTierStatic.Selected;
            if (a != null)
            {
                if (!a.IsDiscreteOrFree)
                {
                    control.convertAnnoContinuousToDiscreteMenu.IsEnabled = true;
                    control.convertAnnoToSignalMenu.IsEnabled = true;
                }
                control.saveAnnoMenu.IsEnabled = true;
                control.saveAnnoMenuAs.IsEnabled = true;
                control.exportMenu.IsEnabled = true;
            }
        }

        private void helpMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Shortcuts:\n\nalt + return to enter fullscreen, esc to close fullscreen\nleftctrl for continuous anno mode, again to close\nalt+click or W on discrete anno to change label/color\nDel on Anno to delete Anno, on tier to delete tier\nalt + right/left to move signalmarker framewise\nshift + alt + right/left to move annomarker framewise\nQ to move signalmarker to start and annomarker to end of selected Segment\nE move annomarker to start and signalmarker to end of selected Segment\na for new Anno between boths markers\nSpace Play/Pause media ", "Quick Reference");
        }



    }
}
