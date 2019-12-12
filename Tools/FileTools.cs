using System;
using System.IO;
using System.Windows;

namespace ssi
{
    public class FileTools
    {        
        public static string[] OpenFileDialog(string filter, bool multi)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = filter;
            dlg.Multiselect = multi;

            bool? result = dlg.ShowDialog();

            string[] filenames = null;
            if (result == true)
            {
                filenames = dlg.FileNames;
            }

            return filenames;
        }

        public static string SaveFileDialog(string defaultName, string defaultExtension, string filterExtension, string directory)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = defaultName;
            dlg.DefaultExt = defaultExtension;
            dlg.Filter = filterExtension;
            dlg.FilterIndex = 0;
            dlg.InitialDirectory = directory;

            bool? result = dlg.ShowDialog();

            string filename = null;
            if (result == true)
            {
                filename = dlg.FileName;
            }

            return filename;
        }

        public static string FormatSeconds(double seconds)
        {
            TimeSpan interval = TimeSpan.FromSeconds(seconds);
            string timeInterval = interval.ToString();

            int pIndex = timeInterval.IndexOf(':');
            pIndex = timeInterval.IndexOf('.', pIndex);

            if (pIndex > 0)
            {
                return timeInterval.Substring(0, pIndex + 3);
            }
            else
            {
                return timeInterval;
            }
        }

        public static string FormatFrames(double seconds, double fps)
        {
            return Math.Round(seconds * fps).ToString();
        }

        public static int FormatFramesInteger(double seconds, double fps)
        {
            return (int) (Math.Round(seconds * fps));
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetAbsolutePath(string filespec, string folder)
        {
            if (Path.IsPathRooted(filespec))
            {
                return filespec;
            }

            return folder + "\\" + filespec;
        }


    }
}