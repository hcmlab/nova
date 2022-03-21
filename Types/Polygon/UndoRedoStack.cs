using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Types
{
    public class UndoRedoStack
    {
        private MainControl control;
        private Stack<ICommand> _Undo;
        private Stack<ICommand> _Redo;

        public MainControl Control { get => control; set => control = value; }

        public UndoRedoStack()
        {
            initStacks();
        }

        public void initStacks()
        {
            _Undo = new Stack<ICommand>();
            _Redo = new Stack<ICommand>();
        }

        public PolygonLabel[] Do(ICommand cmd)
        {
            PolygonLabel[] labels = cmd.Do();
            _Undo.Push(cmd);
            _Redo.Clear();
            updateMenuItems();
            return labels;
        }

        public PolygonLabel[] Undo()
        {
            if (_Undo.Count > 0)
            {
                ICommand cmd = _Undo.Pop();
                PolygonLabel[] label = cmd.Undo();
                _Redo.Push(cmd);
                updateMenuItems();
                return label;
            }

            return null;
        }

        public PolygonLabel[] Redo()
        {
            if (_Redo.Count > 0)
            {
                ICommand cmd = _Redo.Pop();
                PolygonLabel[] label = cmd.Do();
                _Undo.Push(cmd);
                updateMenuItems();
                return label;
            }

            return null;
        }

        public void updateMenuItems(AnnoListItem item = null)
        {
            if(control.annoListControl.annoDataGrid.SelectedItems.Count > 0)
            {
                if(item == null)
                    item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

                if (item.UndoRedoStack.getRedoSize() > 0)
                    control.redo.IsEnabled = true;
                else
                    control.redo.IsEnabled = false;

                if (item.UndoRedoStack.getUndoSize() > 0)
                    control.undo.IsEnabled = true;
                else
                    control.undo.IsEnabled = false;
            }
        }

        public int getUndoSize()
        {
            return this._Undo.Count();
        }

        public int getRedoSize()
        {
            return this._Redo.Count();
        }
    }
}
