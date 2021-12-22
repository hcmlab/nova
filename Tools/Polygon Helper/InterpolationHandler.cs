using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    internal class InterpolationHandler
    {
        private MainControl control;
       

        public InterpolationHandler(MainControl control)
        {
            this.control = control;
        }

        public void handle2DInterpolation(ComboboxItem targetFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel, PolygonLabel targetLabel, double xStepPerFrame, double yStepPerFrame, int framesBetween)
        {
            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            list[sourceFrame.FrameIndex].PolygonList.removeExplicitPolygon(sourceLabel);
            list[targetFrame.FrameIndex].PolygonList.removeExplicitPolygon(targetLabel);
            list[sourceFrame.FrameIndex].PolygonList.removeExplicitPolygon(targetLabel);
            list[targetFrame.FrameIndex].PolygonList.removeExplicitPolygon(sourceLabel);


            int newLabelCounter = -1;
            foreach (AnnoListItem listItem in list)
            {
                if (listItem.Equals(list[sourceFrame.FrameIndex]))
                {
                    newLabelCounter++;
                }

                if (newLabelCounter >= 0)
                {
                    List<PolygonPoint> newPolygon = new List<PolygonPoint>();

                    foreach (PolygonPoint point in sourceLabel.Polygon)
                    {
                        newPolygon.Add(new PolygonPoint(point.X + (newLabelCounter * xStepPerFrame), point.Y + (newLabelCounter * yStepPerFrame)));
                    }

                    PolygonLabel newLabel = new PolygonLabel(newPolygon, sourceLabel.Label, sourceLabel.Color);
                    listItem.PolygonList.addPolygonLabel(newLabel);
                    listItem.updateLabelCount();
                    newLabelCounter++;

                    if (newLabelCounter == (framesBetween + 1))
                    {
                        listItem.PolygonList.removeExplicitPolygon(targetLabel);
                        listItem.updateLabelCount();
                        break;
                    }
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }
        }

        public void handle3DInterpolation(List<double> xStepsPerFrame, List<double> yStepsPerFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel,
                                          PolygonLabel targetLabel, List<PointAngleTuple> sourcePointsAnglesTuple, List<PointAngleTuple> targetPointsAnglesTuple,
                                          int stepsFromSourceToTarget)
        {

            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            int newLabelCounter = 0;

            foreach (AnnoListItem listItem in list)
            {
                // We don't change the selected frame
                if (listItem.Equals(list[sourceFrame.FrameIndex]))
                {
                    newLabelCounter++;
                    continue;
                }

                if (newLabelCounter > 0)
                {
                    List<PolygonPoint> newPolygon = new List<PolygonPoint>();

                    int pointCounter = 0;
                    foreach (PointAngleTuple tuple in sourcePointsAnglesTuple)
                    {
                        newPolygon.Add(new PolygonPoint(tuple.point.X + (newLabelCounter * xStepsPerFrame[pointCounter]),
                                                        tuple.point.Y + (newLabelCounter * yStepsPerFrame[pointCounter])));
                        pointCounter++;
                    }

                    PolygonLabel newLabel = new PolygonLabel(newPolygon, sourceLabel.Label, sourceLabel.Color);
                    listItem.PolygonList.addPolygonLabel(newLabel);
                    listItem.updateLabelCount();
                    newLabelCounter++;

                    if (newLabelCounter == (stepsFromSourceToTarget))
                    {
                        break;
                    }
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }
        }
    }
}
