using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    // Adds a point to a finished label
    class AddCopiedLabelsCommand : ICommand
    {
        private PolygonLabel[] copyLabels;
        private AnnoListItem item;
        private readonly TYPE type = TYPE.COPY;

        public AddCopiedLabelsCommand(PolygonLabel[] copyLabels, AnnoListItem item)
        {
            this.copyLabels = copyLabels;
            this.item = item;
        }

        public PolygonLabel[] Do()
        {
            List<PolygonLabel> newCopyList = new List<PolygonLabel>();
            foreach (PolygonLabel label in copyLabels)
            {
                PolygonLabel newLabel = new PolygonLabel(label.getPolygonAsCopy(), label.Label, label.Color, label.Confidence);
                newLabel.Informations = new LabelInformations(this.type);
                newCopyList.Add(newLabel);
                this.item.PolygonList.addPolygonLabel(newLabel);
            }

            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            item.updateLabelCount();
            copyLabels = newCopyList.ToArray();
            return this.copyLabels;
        }

        public PolygonLabel[] Undo()
        {
            foreach (PolygonLabel label in copyLabels)
            {
                this.item.PolygonList.removeExplicitPolygon(label);
            }

            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            item.updateLabelCount();

            return this.copyLabels;
        }
    }
}
