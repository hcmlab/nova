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
        private Stack<ICommand> _Undo;
        private Stack<ICommand> _Redo;

        public UndoRedoStack()
        {
            initStacks();
        }

        public void initStacks()
        {
            _Undo = new Stack<ICommand>();
            _Redo = new Stack<ICommand>();
        }

        public void Do(ICommand cmd)
        {
            cmd.Do();
            _Undo.Push(cmd);
            _Redo.Clear();
        }

        public PolygonLabel Undo()
        {
            if (_Undo.Count > 0)
            {
                ICommand cmd = _Undo.Pop();
                PolygonLabel label = cmd.Undo();
                _Redo.Push(cmd);
                return label;
            }

            return null;
        }

        public PolygonLabel Redo()
        {
            if (_Redo.Count > 0)
            {
                ICommand cmd = _Redo.Pop();
                PolygonLabel label = cmd.Do();
                _Undo.Push(cmd);
                return label;
            }

            return null;
        }
    }
}
