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

    public partial class AnnoTierUndoRedo : IUndoRedo
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
                    AnnoListItem ali = ((AnnoTierLabel)Undostruct.UiElement).Item;
                    ((AnnoTier)Container).AnnoList.AddSorted(ali);
                    AnnoTierLabel at = ((AnnoTier)Container).addSegment(ali);
                    this.RedoPushInUnDoForDelete(at);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                    ((AnnoTier)Container).deleteSegment((AnnoTierLabel)Undostruct.UiElement);
                    this.RedoPushInUnDoForInsert(Undostruct.UiElement);
                }
                else if (Undostruct.Action == ActionType.Resize)
                {
                        this.RedoPushInUnDoForResize(Canvas.GetLeft(Undostruct.UiElement), Undostruct.UiElement);
                        Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                        Undostruct.UiElement.Width = Undostruct.Width;
                        ((AnnoTierLabel)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                        ((AnnoTierLabel)Undostruct.UiElement).Item.Start = Undostruct.Start;
                        ((AnnoTierLabel)Undostruct.UiElement).Item.Stop = Undostruct.Stop;
                       

                }
    
                else if (Undostruct.Action == ActionType.Move)
                {
                    this.RedoPushInUnDoForMove(Canvas.GetLeft(Undostruct.UiElement), Undostruct.UiElement);
                    Canvas.SetLeft(Undostruct.UiElement,Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    ((AnnoTierLabel)Undostruct.UiElement).Item.Duration = Undostruct.Duration;
                    ((AnnoTierLabel)Undostruct.UiElement).Item.Start = Undostruct.Start;
                    ((AnnoTierLabel)Undostruct.UiElement).Item.Stop = Undostruct.Stop;
                   
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
                    ((AnnoTier)Container).deleteSegment((AnnoTierLabel)Undostruct.UiElement);

                    ChangeRepresentationObject ChangeRepresentationObjectForDelete = this.MakeChangeRepresentationObjectForDelete(Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForDelete);
                }
                else if (Undostruct.Action == ActionType.Insert)
                {
                     AnnoListItem ali = ((AnnoTierLabel)Undostruct.UiElement).Item;
                    ((AnnoTier)Container).AnnoList.AddSorted(ali);
                    AnnoTierLabel at = ((AnnoTier)Container).addSegment(ali);

                    ChangeRepresentationObject ChangeRepresentationObjectForInsert = this.MakeChangeRepresentationObjectForInsert(at);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForInsert);
                }
                else if (Undostruct.Action == ActionType.Resize)
                {
                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    Undostruct.Start = ((AnnoTierLabel)Undostruct.UiElement).Item.Start;
                    Undostruct.Stop = ((AnnoTierLabel)Undostruct.UiElement).Item.Stop;
                    Undostruct.Duration = ((AnnoTierLabel)Undostruct.UiElement).Item.Duration;

                    ChangeRepresentationObject ChangeRepresentationObjectForResize = this.MakeChangeRepresentationObjectForResize(Undostruct.Margin.X, Undostruct.UiElement);
                    _UndoActionsCollection.Push(ChangeRepresentationObjectForResize);

                }
                else if (Undostruct.Action == ActionType.Move)
                {

                    Canvas.SetLeft(Undostruct.UiElement, Undostruct.Margin.X);
                    Undostruct.UiElement.Width = Undostruct.Width;
                    Undostruct.Start = ((AnnoTierLabel)Undostruct.UiElement).Item.Start;
                    Undostruct.Stop = ((AnnoTierLabel)Undostruct.UiElement).Item.Stop;
                    Undostruct.Duration = ((AnnoTierLabel)Undostruct.UiElement).Item.Duration;

                    ChangeRepresentationObject ChangeRepresentationObjectForMove = this.MakeChangeRepresentationObjectForMove(Undostruct.Margin.X, Undostruct.UiElement);
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

        public ChangeRepresentationObject MakeChangeRepresentationObjectForMove(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin.X = pos;
            MoveStruct.Width = UIelement.Width;
            MoveStruct.Start = ((AnnoTierLabel)UIelement).Item.Start;
            MoveStruct.Stop = ((AnnoTierLabel)UIelement).Item.Stop;
            MoveStruct.Duration = ((AnnoTierLabel) UIelement).Item.Duration;
            MoveStruct.UiElement = UIelement;
            return MoveStruct;
        }

        public ChangeRepresentationObject MakeChangeRepresentationObjectForResize(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Action = ActionType.Resize;
            ResizeStruct.Margin.X = pos;
            ResizeStruct.Width = UIelement.Width;
            ResizeStruct.Start = ((AnnoTierLabel)UIelement).Item.Start;
            ResizeStruct.Stop = ((AnnoTierLabel)UIelement).Item.Stop;
            ResizeStruct.Duration = ((AnnoTierLabel)UIelement).Item.Duration;
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

        public void RedoPushInUnDoForMove(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject MoveStruct = new ChangeRepresentationObject();
            MoveStruct.Action = ActionType.Move;
            MoveStruct.Margin.X = pos;
            MoveStruct.Width = UIelement.Width;
            MoveStruct.Start = ((AnnoTierLabel)UIelement).Item.Start;
            MoveStruct.Stop = ((AnnoTierLabel)UIelement).Item.Stop;
            MoveStruct.Duration = ((AnnoTierLabel)UIelement).Item.Duration;
            MoveStruct.UiElement = UIelement;
            _RedoActionsCollection.Push(MoveStruct);
        }

        public void RedoPushInUnDoForResize(double pos, FrameworkElement UIelement)
        {
            ChangeRepresentationObject ResizeStruct = new ChangeRepresentationObject();
            ResizeStruct.Action = ActionType.Resize;
            ResizeStruct.Margin.X = pos;
            ResizeStruct.Width = UIelement.Width;
            ResizeStruct.Start = ((AnnoTierLabel)UIelement).Item.Start;
            ResizeStruct.Stop = ((AnnoTierLabel)UIelement).Item.Stop;
            ResizeStruct.Duration = ((AnnoTierLabel)UIelement).Item.Duration;
            ResizeStruct.UiElement = UIelement;
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
        public Point Margin;
        public double Width;
        public double Start;
        public double Stop;
        public double Duration;
        public FrameworkElement UiElement;
    }

    #endregion datastructures
}