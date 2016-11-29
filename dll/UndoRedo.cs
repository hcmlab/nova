using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    #region UndoRedo

    internal interface IUndoRedo
    {
        void Undo(int level);

        void Redo(int level);

        void InsertObjectforUndoRedo(ChangeRepresentationObject dataobject);
    }

    public partial class UnDoRedo : IUndoRedo
    {
        private Stack<ChangeRepresentationObject> _UndoActionsCollection = new Stack<ChangeRepresentationObject>();
        private Stack<ChangeRepresentationObject> _RedoActionsCollection = new Stack<ChangeRepresentationObject>();

        public event EventHandler EnableDisableUndoRedoFeature;

        private Canvas _Container;

        public Canvas Container
        {
            get { return _Container; }
            set { _Container = value; }
        }

        #region IUndoRedo Members

        public void Undo(int level)
        {
            for (int i = 1; i <= level; i++)
            {
                if (_UndoActionsCollection.Count == 0) return;

                ChangeRepresentationObject Undostruct = _UndoActionsCollection.Pop();
                if (Undostruct.Action == ActionType.Delete)
                {
                    AnnoListItem ali = ((AnnoTrackSegment)Undostruct.UiElement).Item;
                    ((AnnoTrack)Container).AnnoList.AddSorted(ali);
                    AnnoTrackSegment at = ((AnnoTrack)Container).addSegment(ali);
                    this.RedoPushInUnDoForDelete(at);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                    ((AnnoTrack)Container).deleteSegment((AnnoTrackSegment)Undostruct.UiElement);
                    this.RedoPushInUnDoForInsert(Undostruct.UiElement);
                }
                //else if (Undostruct.Action == ActionType.Resize)
                //{
                //    if (_UndoActionsCollection.Count != 0)
                //    {
                //        Point previousMarginOfSelectedObject = new Point(((FrameworkElement)Undostruct.UiElement).Margin.Left, ((FrameworkElement)Undostruct.UiElement).Margin.Top);
                //        this.RedoPushInUnDoForResize(previousMarginOfSelectedObject, Undostruct.UiElement.Width, Undostruct.UiElement.Height, Undostruct.UiElement);
                //        Undostruct.UiElement.Margin = new Thickness(Undostruct.Margin.X, Undostruct.Margin.Y, 0, 0);
                //        Undostruct.UiElement.Height = Undostruct.height;
                //        Undostruct.UiElement.Width = Undostruct.Width;

                //       if(Undostruct.isresizeright)
                //        {
                //            ((AnnoTrackSegment)Undostruct.UiElement).resize_right(Undostruct.Margin.X - Undostruct.Width);
                //        }

                //        if (Undostruct.isresizeleft)
                //        {
                //            ((AnnoTrackSegment)Undostruct.UiElement).resize_left(Undostruct.Margin.X + Undostruct.Width);
                //        }

                //        if (Undostruct.ismoved)
                //        {
                //            ((AnnoTrackSegment)Undostruct.UiElement).move(Undostruct.Margin.X + Undostruct.Width);
                //        }

                //    }
                //}
                //else if (Undostruct.Action == ActionType.Move)
                //{

                //    AnnoListItem ali = ((AnnoTrackSegment)Undostruct.UiElement).Item;
                //    ((AnnoTrack)Container).deleteSegment((AnnoTrackSegment)Undostruct.UiElement);
                //    ((AnnoTrack)Container).AnnoList.AddSorted(ali);
                //    AnnoTrackSegment at = ((AnnoTrack)Container).addSegment(ali);


                //    Point previousMarginOfSelectedObject = new Point(((FrameworkElement)Undostruct.UiElement).Margin.Left, ((FrameworkElement)Undostruct.UiElement).Margin.Top);
                //    this.RedoPushInUnDoForMove(previousMarginOfSelectedObject, Undostruct.UiElement);

                //}

                else if (Undostruct.Action == ActionType.Move)
                {
                  
                    Point previousMarginOfSelectedObject = new Point((Canvas.GetLeft((FrameworkElement)Undostruct.UiElement)), 0);
                    Canvas.SetLeft(Undostruct.UiElement,Undostruct.Margin.X);     
                    Undostruct.UiElement.Width = Undostruct.Width;


                   // ((AnnoTrackSegment)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                    this.RedoPushInUnDoForMove(previousMarginOfSelectedObject, Undostruct.UiElement, Undostruct.Width, Undostruct.Duration);
                }
            }

            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        public void Redo(int level)
        {
            for (int i = 1; i <= level; i++)
            {
                if (_RedoActionsCollection.Count == 0) return;

                ChangeRepresentationObject Undostruct = _RedoActionsCollection.Pop();
                if (Undostruct.Action == ActionType.Delete)
                {
                    ((AnnoTrack)Container).deleteSegment((AnnoTrackSegment)Undostruct.UiElement);

                    ChangeRepresentationObject ChangeRepresentationObjectForDelete = this.MakeChangeRepresentationObjectForDelete(Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForDelete);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                     AnnoListItem ali = ((AnnoTrackSegment)Undostruct.UiElement).Item;
                    ((AnnoTrack)Container).AnnoList.AddSorted(ali);
                    AnnoTrackSegment at = ((AnnoTrack)Container).addSegment(ali);

                    ChangeRepresentationObject ChangeRepresentationObjectForInsert = this.MakeChangeRepresentationObjectForInsert(at);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForInsert);
                }
                //else if (Undostruct.Action == ActionType.Resize)
                //{
                //    Point previousMarginOfSelectedObject = new Point(((FrameworkElement)Undostruct.UiElement).Margin.Left, ((FrameworkElement)Undostruct.UiElement).Margin.Top);
                //    ChangeRepresentationObject ChangeRepresentationObjectforResize = this.MakeChangeRepresentationObjectForResize(previousMarginOfSelectedObject, Undostruct.UiElement.Width, Undostruct.UiElement.Height, Undostruct.UiElement,false,false,false);
                //    _UndoActionsCollection.Push(ChangeRepresentationObjectforResize);

                //    Undostruct.UiElement.Margin = new Thickness(Undostruct.Margin.X, Undostruct.Margin.Y, 0, 0);
                //    Undostruct.UiElement.Height = Undostruct.height;
                //    Undostruct.UiElement.Width = Undostruct.Width;

                //}
                else if (Undostruct.Action == ActionType.Move)
                {

                    Point previousMarginOfSelectedObject = new Point((Canvas.GetLeft((FrameworkElement)Undostruct.UiElement)), 0);


                   
                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    // ((AnnoTrackSegment)Undostruct.UiElement).Item.Duration = Undostruct.Duration;


                    AnnoListItem ali = ((AnnoTrackSegment)Undostruct.UiElement).Item;
                    ((AnnoTrack)Container).deleteSegment((AnnoTrackSegment)Undostruct.UiElement);
                    ((AnnoTrack)Container).AnnoList.AddSorted(ali);
                    AnnoTrackSegment at = ((AnnoTrack)Container).addSegment(ali);
                  


                    ChangeRepresentationObject ChangeRepresentationObjectForMove = this.MakeChangeRepresentationObjectForMove(previousMarginOfSelectedObject, Undostruct.UiElement, Undostruct.Width, Undostruct.Duration);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForMove);
                }
            }
            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        public void InsertObjectforUndoRedo(ChangeRepresentationObject dataobject)
        {
            _UndoActionsCollection.Push(dataobject);
            _RedoActionsCollection.Clear();
            if (EnableDisableUndoRedoFeature != null)
            {
                EnableDisableUndoRedoFeature(null, null);
            }
        }

        #endregion IUndoRedo Members

        #region UndoHelperFunctions

        public ChangeRepresentationObject MakeChangeRepresentationObjectForInsert(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataObject = new ChangeRepresentationObject();
            dataObject.Action = ActionType.Insert;
            dataObject.UiElement = ApbOrDevice;
            return dataObject;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForDelete(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Delete;
            dataobject.UiElement = ApbOrDevice;
            return dataobject;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForMove(Point margin, FrameworkElement UIelement, double Width, double duration)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin = margin;
            MoveStruct.Width = Width;
            MoveStruct.Duration = duration;
            MoveStruct.UiElement = UIelement;
            return MoveStruct;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForResize(Point margin, double width, double height, FrameworkElement UIelement, bool isresizeright, bool isresizeleft, bool ismoved)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Action = ActionType.Resize;
            ResizeStruct.Margin = margin;
            ResizeStruct.Width = width;
            ResizeStruct.isresizeright = isresizeright;
            ResizeStruct.isresizeleft = isresizeleft;
            ResizeStruct.ismoved = ismoved;

            ResizeStruct.UiElement = UIelement;
            return ResizeStruct;
        }

        public void clearUnRedo()
        {
            _UndoActionsCollection.Clear();
        }

        #endregion UndoHelperFunctions

        #region RedoHelperFunctions

        public void RedoPushInUnDoForInsert(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Insert;
            dataobject.UiElement = ApbOrDevice;
            _RedoActionsCollection.Push(dataobject);
        }

        public void RedoPushInUnDoForDelete(FrameworkElement ApbOrDevice)
        {
            ChangeRepresentationObject dataobject = new ChangeRepresentationObject();
            dataobject.Action = ActionType.Delete;
            dataobject.UiElement = ApbOrDevice;
            _RedoActionsCollection.Push(dataobject);
        }

        public void RedoPushInUnDoForMove(Point margin, FrameworkElement UIelement, double Width, double Duration)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin = margin;
            MoveStruct.Width = Width;
            MoveStruct.UiElement = UIelement;
            _RedoActionsCollection.Push(MoveStruct);
        }

        public void RedoPushInUnDoForResize(Point margin, double width, double height, FrameworkElement UIelement, double Width)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Margin = margin;
            ResizeStruct.Width = Width;
            ResizeStruct.UiElement = UIelement;
            ResizeStruct.Action = ActionType.Resize;
            _RedoActionsCollection.Push(ResizeStruct);
        }

        #endregion RedoHelperFunctions

        public bool IsUndoPossible()
        {
            if (_UndoActionsCollection.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsRedoPossible()
        {
            if (_RedoActionsCollection.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #endregion UndoRedo

    #region enums

    public enum ActionType
    {
        Delete = 0,
        Move = 1,
        Resize = 2,
        Insert = 3
    }

    #endregion enums

    #region datastructures

    public class ChangeRepresentationObject
    {
        public ActionType Action;
        public bool isresizeright;
        public bool isresizeleft;
        public bool ismoved;
        public Point Margin;
        public double Width;
        public double Duration;
        public FrameworkElement UiElement;
    }

    #endregion datastructures
}