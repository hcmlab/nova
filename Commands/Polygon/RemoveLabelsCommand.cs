using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    class RemoveLabelsCommand : ICommand
    {
        private PolygonLabel[] labelsToRemove;
        private AnnoListItem item;
        private readonly TYPE type = TYPE.REMOVE;


        public RemoveLabelsCommand(PolygonLabel[] labelsToRemove, AnnoListItem item)
        {
            this.labelsToRemove = labelsToRemove;
            this.item = item;
        }

        public PolygonLabel[] Do()
        {
            foreach(PolygonLabel label in labelsToRemove)
            {
                this.item.PolygonList.removeExplicitPolygon(label);
                label.Informations = new LabelInformations(this.type);
            }

            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            item.updateLabelCount();

            return this.labelsToRemove;
        }

        public PolygonLabel[] Undo()
        {
            foreach (PolygonLabel label in labelsToRemove)
            {
                this.item.PolygonList.addPolygonLabel(label);
                label.Informations = new LabelInformations(this.type);
            }

            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            item.updateLabelCount();


            return this.labelsToRemove;
        }
    }
}
