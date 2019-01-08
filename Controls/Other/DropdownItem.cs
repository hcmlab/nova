using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ssi { 

    public class DropdownItem : Control
    {
        static DropdownItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropdownItem), new FrameworkPropertyMetadata(typeof(DropdownItem)));
        }


        public const string PART_Textpresenter = "PART_Textpresenter";
        private TextBlock m_Textpresenter = null;

        const string TAG_NORMALTEXT = "Normaltext";
        const string TAG_FILTERTEXT = "Filtertext";
   

      
        public static readonly DependencyProperty NormalBrushProperty = DependencyProperty.Register("NormalBrush", typeof(Brush), typeof(DropdownItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(NormalBrushChanged)));

        private static void NormalBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownItem ditem = d as DropdownItem;
            ditem.OnNormalBrushChanged();
        }

        public Brush NormalBrush
        {
            get { return (Brush)GetValue(NormalBrushProperty); }
            set
            {
                SetValue(NormalBrushProperty, value);
            }
        }

        private void OnNormalBrushChanged()
        {
            if (m_Textpresenter != null)
            {
                foreach (var item in m_Textpresenter.Inlines)
                {
                    string tag = item.Tag as string;
                    if (tag.ToLower() == TAG_NORMALTEXT)
                    {
                        item.Foreground = NormalBrush ;
                    }
                }
            }
        }
        /*-----------------------------------------------------------*/
        public static readonly DependencyProperty FilterBrushProperty = DependencyProperty.Register("FilterBrush", typeof(Brush), typeof(DropdownItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(FilterBrushChanged)));

        private static void FilterBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownItem dropitem = d as DropdownItem;
            dropitem.OnFilterBrushChanged();
        }
        public Brush FilterBrush
        {
            get { return (Brush)GetValue(FilterBrushProperty); }
            set {
                SetValue(FilterBrushProperty, value);
            }

        }

        private void OnFilterBrushChanged()
        {
            if (m_Textpresenter != null)
            {
                foreach (var item in m_Textpresenter.Inlines)
                {
                    string tag = item.Tag as string;
                    if (tag == TAG_FILTERTEXT)
                    {
                        item.Foreground = FilterBrush;
                    }
                }
            }
        }
        /*------------------------------------------------------------*/
        public static readonly DependencyProperty NormalTextProperty = DependencyProperty.Register("NormalText", typeof(string), typeof(DropdownItem),
            new FrameworkPropertyMetadata(string.Empty, new PropertyChangedCallback(NormalTextChanged)));

        private static void NormalTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownItem ditem = d as DropdownItem;
            if (e.NewValue != e.OldValue)
            {
                string val = e.NewValue as string;
                ditem.OnNormalTextChanged(val);
            }
        }

        public string NormalText {
            get { return (string)GetValue(NormalTextProperty); }
            set
            {
                SetValue(NormalTextProperty, value);
            }
        }

        private void OnNormalTextChanged(string text)
        {
            if (m_Textpresenter != null)
            {
                m_Textpresenter.Text = text;
            }
        }
        /*-------------------------------------------------------------*/

        public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register("FilterText", typeof(string), typeof(DropdownItem), new FrameworkPropertyMetadata(string.Empty, new PropertyChangedCallback(FilterTextChanged)));

        private static void FilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownItem di = d as DropdownItem;
            if (e.NewValue != e.OldValue)
            {
                string str = e.NewValue as string;
                di.FilterTextCore(str);
            }
        }


        public string FilterText
        {
            get { return (string)GetValue(FilterTextProperty); }
            set { SetValue(FilterTextProperty, value); }
        }


        private void FilterTextCore(string filter)
        {
            if (m_Textpresenter == null) return;
            if (string.IsNullOrEmpty(NormalText)) return;
            if (string.IsNullOrWhiteSpace(filter)) return;

            m_Textpresenter.Inlines.Clear();
            char[] chars = NormalText.ToCharArray();
            foreach (char c in chars)
            {
                Run runTxt = new Run();
                runTxt.Text = c.ToString();
                if (filter.Contains(c)) 
                {
                    runTxt.Foreground = FilterBrush;
                    runTxt.Tag = TAG_FILTERTEXT;
                }
                else     
                {
                    runTxt.Foreground = NormalBrush;
                    runTxt.Tag = TAG_NORMALTEXT;
                }
                m_Textpresenter.Inlines.Add(runTxt);
            }
        }

        public override void OnApplyTemplate()
        {
            m_Textpresenter = GetTemplateChild(PART_Textpresenter) as TextBlock;
            if (m_Textpresenter != null)
            {
                m_Textpresenter.Text = NormalText;
            }
            base.OnApplyTemplate();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            RoutedEventArgs arg = new RoutedEventArgs(DropdownTextBox.ItemActivedEvent, this);
            this.RaiseEvent(arg);
            e.Handled = true;
        }

    }
}
