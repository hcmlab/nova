using System.Windows;

namespace ssi
{
    public interface IMedia
    {
        UIElement GetView();

        void Url(string url);

        void SetVolume(double volume);

        void Play();

        void Stop();

        void Pause();

        void Clear();

        void Move(double to_in_seconds);

        double GetPosition();

        double GetLength();

        double GetSampleRate();

        bool IsVideo();

        string GetFilepath();

        string GetFolderepath();

        void zoomIn(double factor, double width, double height);

        void zoomOut(double factor, double width, double height);
    }
}