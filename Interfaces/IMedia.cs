using System.Windows;
using System.Windows.Media.Imaging;

namespace ssi
{
    public enum MediaType
    {
        AUDIO,
        VIDEO,
        SKELETON,
        FACE
    };

    public interface IMedia
    { 

        UIElement GetView();

        WriteableBitmap GetOverlay();

        MediaType GetMediaType();

        bool HasAudio();        

        void SetVolume(double volume);

        double GetVolume();

        void Play();

        void Stop();

        void Pause();

        void Clear();

        void Move(double time);

        double GetPosition();

        double GetLength();

        double GetSampleRate();

        string GetFilepath();

        string GetDirectory();
    }
}