using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    public delegate void MediaBoxChangeEventHandler(MediaBox track, EventArgs e);

    public partial class MediaBoxStatic
    {
        static public MediaBox Selected = null;

        static public event MediaBoxChangeEventHandler OnBoxChange;



        static public void Select(MediaBox box)
        {
            Unselect();
            Selected = box;

            if (Selected.Border != null)
            {
                Selected.Border.BorderBrush = Defaults.Brushes.Highlight;
            }

            OnBoxChange?.Invoke(Selected, null);
        }

        static public void Unselect()
        {
            if (Selected != null)
            {
                Selected.Border.BorderBrush = Defaults.Brushes.Conceal;
                Selected = null;
            }
        }
    }

    public partial class MediaBox : UserControl, INotifyPropertyChanged
    {
        private IMedia media = null;
        public IMedia Media
        { 
            get { return media; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public MediaBox(IMedia media)
        {
            this.media = media;

            InitializeComponent();

            Grid.SetColumn(media.GetView(), 0);
            Grid.SetRow(media.GetView(), 0);

            zoomControl.Child = media.GetView();
        }

        public Border Border { get; set; }

        public void RemoveMediaBox(IMedia media)
        {
            media.Stop();
            media.Clear();
            zoomControl.Child = null;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (MediaBoxStatic.Selected != this)
            {
                MediaBoxStatic.Select(this);
            }
        }
    }
}