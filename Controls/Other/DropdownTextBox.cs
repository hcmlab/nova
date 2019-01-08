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
using System.Collections;

namespace ssi
{

    public class DropdownTextBox : Control
    {
        static DropdownTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropdownTextBox), new FrameworkPropertyMetadata(typeof(DropdownTextBox)));
        }

 

        /*----------------------------------------------------------------*/
        public static readonly DependencyProperty IsDropdownOpenedProperty = DependencyProperty.Register("IsDropdownOpened", typeof(bool), typeof(DropdownTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(IsDropdownOpenedPropertyChanged)));

    
        public bool IsDropdownOpened
        {
            get { return (bool)GetValue(IsDropdownOpenedProperty); }
            set { SetValue(IsDropdownOpenedProperty, value); }
        }

        private static void IsDropdownOpenedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool b = (bool)e.NewValue;
            DropdownTextBox db = d as DropdownTextBox;
            db.OnDropdownStateChanged(b);
        }

        protected virtual void OnDropdownStateChanged(bool isopened)
        {
      
            RoutedEventArgs args = new RoutedEventArgs();
            if (isopened)
            {
                args.RoutedEvent = DropdownOpenedEvent;
            }
            else
            {
                args.RoutedEvent = DropdownClosedEvent;
            }
            args.Source = this;
            RaiseEvent(args);
        }

        public static readonly DependencyProperty DropItemsProperty = DependencyProperty.Register("DropItems", typeof(IEnumerable), typeof(DropdownTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(DropItemsPropertyChanged)));

        public IEnumerable DropItems
        {
            get { return (IEnumerable)GetValue(DropItemsProperty); }
            set { SetValue(DropItemsProperty, value); }
        }

        private static void DropItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownTextBox dt = d as DropdownTextBox;
            if (dt != null)
            {
                IEnumerable oldval, newval;
                oldval = (IEnumerable)e.OldValue;
                newval = (IEnumerable)e.NewValue;
                dt.OnDropItemsChanged(oldval, newval);
            }
        }

        protected virtual void OnDropItemsChanged(IEnumerable oldItems, IEnumerable newItems)
        {
            if (oldItems != newItems)
            {
                this.UpdateUIItems(newItems);
            }
        }

        /*------------------------------------------------------*/
        public static readonly DependencyProperty MinDropdownHeightProperty = DependencyProperty.Register("MinDropdownHeight", typeof(double), typeof(DropdownTextBox));

        public double MinDropdownHeight
        {
            get { return (double)GetValue(MinDropdownHeightProperty); }
            set { SetValue(MinDropdownHeightProperty, value); }
        }

        /*--------------------------------------------------------------*/
        public static readonly DependencyProperty MaxDropdownHeightProperty = DependencyProperty.Register("MaxDropdownHeight", typeof(double), typeof(DropdownTextBox));

   
        public double MaxDropdownHeight
        {
            get { return (double)GetValue(MaxDropdownHeightProperty); }
            set { SetValue(MaxDropdownHeightProperty, value); }
        }

        /*----------------------------------------------------------*/
        public static readonly DependencyProperty TextProperty = TextBox.TextProperty.AddOwner(typeof(DropdownTextBox),new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DropdownTextBox db = d as DropdownTextBox;
            db.OnTextChanged();
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
  

        private TextBox m_InputBox = null;
 
        const string PART_InputBox = "PART_InputBox";

        const string PART_ItemPanel = "PART_ItemPanel";
 
        private Panel m_itemsPanel = null;
   

   
        protected virtual UIElement CreateItemContainer()
        {
            DropdownItem ue = new DropdownItem();
            AddItemActivedHandler(ue, new RoutedEventHandler(Item_Activated));
            return ue;
        }

        private void Item_Activated(object sender, RoutedEventArgs e)
        {
            if (m_InputBox == null) return;
            DropdownItem item = e.Source as DropdownItem;
            if (item != null)
            {
                string str = item.NormalText;
                m_InputBox.Tag = true;
                m_InputBox.Text = str;
                m_InputBox.Tag = false;
                IsDropdownOpened = false;
            }
        }

        protected virtual void ClearAllItems()
        {
            if (m_itemsPanel != null)
            {
                m_itemsPanel.Children.Clear();
            }
        }

        private void UpdateUIItems(IEnumerable items)
        {
            if (m_itemsPanel != null)
            {
                ClearAllItems();
                if (items != null)
                {
                    foreach (object item in items)
                    {
                        string strContent = string.Empty;
                        if (item is string)
                            strContent = item as string;
                        else
                            strContent = item.ToString();
                        UIElement ui = CreateItemContainer();
                        ui.SetValue(DropdownItem.NormalTextProperty, strContent);
                        m_itemsPanel.Children.Add(ui);
                    }
                    m_itemsPanel.UpdateLayout();

                }
            }
        }


        private void FilterText(string txt)
        {
            if (m_itemsPanel != null)
            {
                foreach (UIElement ue in m_itemsPanel.Children)
                {
                    ue.SetValue(DropdownItem.FilterTextProperty, txt);
                }
            }
        }

        /*--------------------------------------------------------------------*/
        public override void OnApplyTemplate()
        {
            if (m_InputBox != null)
            {
                m_InputBox.TextChanged -= m_InputBox_TextChanged;
                m_InputBox = null;
            }
            m_InputBox = GetTemplateChild(PART_InputBox) as TextBox;
            if (m_InputBox != null)
                m_InputBox.Tag = false;
            m_InputBox.TextChanged += m_InputBox_TextChanged;

            if (m_itemsPanel != null)
            {
                m_itemsPanel = null;
            }
            m_itemsPanel = GetTemplateChild(PART_ItemPanel) as VirtualizingStackPanel;
            if (m_itemsPanel != null)
            {
                UpdateUIItems(DropItems);
            }

            base.OnApplyTemplate();
        }

        void m_InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool b = (bool)m_InputBox.Tag;
            if (b == false)
            {
                if (IsDropdownOpened == false)
                {
                    IsDropdownOpened = true;
                }
            }

            FilterText(m_InputBox.Text);
        }

        protected virtual void OnTextChanged()
        {
            RoutedEventArgs args = new RoutedEventArgs(TextChangedEvent, this);
            RaiseEvent(args);
        }

   
      
        public static readonly RoutedEvent ItemActivedEvent = EventManager.RegisterRoutedEvent("ItemActived", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropdownTextBox));

        public static void AddItemActivedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement u = d as UIElement;
            if (u != null)
            {
                u.AddHandler(ItemActivedEvent, handler);
            }
        }

        public static void RemoveItemActivedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement u = d as UIElement;
            if (u != null)
            {
                u.RemoveHandler(ItemActivedEvent, handler);
            }
        }

        /*-------------------------------------------------------------------*/

        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent("TextChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropdownTextBox));

        public event TextChangedEventHandler TextChanged
        {
            add
            {
                AddHandler(TextChangedEvent, value);
            }
            remove
            {
                RemoveHandler(TextChangedEvent, value);
            }
        }

        /*---------------------------------------------------*/
        public static readonly RoutedEvent DropdownOpenedEvent = EventManager.RegisterRoutedEvent("DropdownOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropdownTextBox));

     
        public event RoutedEventHandler DropdownOpened
        {
            add { AddHandler(DropdownOpenedEvent, value); }
            remove { RemoveHandler(DropdownOpenedEvent, value); }
        }

        /*---------------------------------------------------------*/
        public static readonly RoutedEvent DropdownClosedEvent = EventManager.RegisterRoutedEvent("DropdownClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DropdownTextBox));

        public event RoutedEventHandler DropdownClosed
        {
            add { AddHandler(DropdownClosedEvent, value); }
            remove { RemoveHandler(DropdownClosedEvent, value); }
        }


    }
}
