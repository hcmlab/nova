﻿using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    class ChangeLabelCommand : ICommand
    {   
        private PolygonLabel polygonToChange;
        private List<Point> oldPoints;
        private List<Point> newPoints;
        private string oldConf;
        private readonly TYPE type = TYPE.EDIT;

        public ChangeLabelCommand(List<Point> oldPoints, List<Point> newPoints, PolygonLabel polygonToChange)
        {
            this.polygonToChange = polygonToChange;
            this.oldConf = polygonToChange.Confidence;
            this.oldPoints = oldPoints;
            this.newPoints = newPoints;
        }

        public PolygonLabel[] Do()
        {
            int counter = 0;
            foreach (PolygonPoint point in polygonToChange.Polygon)
            {
                point.X = newPoints[counter].X;
                point.Y = newPoints[counter].Y;
                counter++;
            }

            polygonToChange.Informations = new LabelInformations(this.type);
            polygonToChange.Confidence = "100";
            return new PolygonLabel[] { polygonToChange };
        }

        public PolygonLabel[] Undo()
        {
            int counter = 0;
            foreach (PolygonPoint point in polygonToChange.Polygon)
            {
                point.X = oldPoints[counter].X;
                point.Y = oldPoints[counter].Y;
                counter++;
            }

            polygonToChange.Informations = new LabelInformations(this.type);
            polygonToChange.Confidence = this.oldConf;
            return new PolygonLabel[] { polygonToChange };
        }
    }
}
