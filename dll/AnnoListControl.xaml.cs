using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoListControl.xaml
    /// </summary>
    public partial class AnnoListControl : UserControl
    {
        public AnnoListControl()
        {
            InitializeComponent();
        }

        private void MenuItemDeleteClick(object sender, RoutedEventArgs e)
        {
            if (AnnoTrack.GetSelectedTrack().isDiscrete)
            {
                if (annoDataGrid.SelectedItems.Count > 0)
                {
                    AnnoListItem[] selected = new AnnoListItem[annoDataGrid.SelectedItems.Count];
                    annoDataGrid.SelectedItems.CopyTo(selected, 0);
                    annoDataGrid.SelectedIndex = -1;

                    AnnoTrack track = AnnoTrackStatic.GetSelectedTrack();
                    foreach (AnnoListItem s in selected)
                    {
                        AnnoTrackSegment segment = track.getSegment(s);
                        if (segment != null)
                        {
                            track.remSegment(segment);
                        }
                    }
                }
            }
        }

        private void MenuItemCopyWithMetaClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine("name=" + s.Label + ";from=" + s.Start + ";to=" + s.Stop + ";" + s.Meta.Replace('\n', ';'));
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyWithoutMetaClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine("name=" + s.Label + ";from=" + s.Start + ";to=" + s.Stop + ";");
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine(s.Meta.Replace('\n', ';'));
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaNumbersOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    string[] split = Regex.Split(s.Meta, @"[=\n]");
                    if (split.Length > 1)
                    {
                        sb.Append(split[1]);
                        for (int i = 3; i < split.Length; i += 2)
                        {
                            sb.Append(";" + split[i]);
                        }
                        sb.AppendLine();
                    }
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaStringsOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    string[] split = Regex.Split(s.Meta, @"[=\n]");
                    if (split.Length > 0)
                    {
                        sb.Append(split[0]);
                        for (int i = 2; i < split.Length; i += 2)
                        {
                            sb.Append(";" + split[i]);
                        }
                        sb.AppendLine();
                    }
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void editTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }
    }
}