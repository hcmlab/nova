﻿using ssi.Interfaces;
using ssi.Tools.Polygon_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CMS = System.Windows.Forms.ContextMenuStrip;
using CMSItem = System.Windows.Forms.ToolStripMenuItem;

namespace ssi.Types.Polygon
{
    internal class PolygonUtilities
    {
        private MainHandler handler;
        private MainControl control;
        private IDrawUnit polygonDrawUnit;
        private Window interpolationWindow;
        private PolygonInformations polygonInformations;
        private DataGridChecker dataGridChecker;
        private MousePositionInformation mousePositionInformation;

        private bool controlInStacksSet = false;

        public Window InterpolationWindow { get => interpolationWindow; set => interpolationWindow = value; }
        public bool ControlInStacksSet { set => controlInStacksSet = value; }

        public PolygonUtilities()
        {
 
        }

        public void setObjects(MainControl control, MainHandler handler, PolygonInformations polygonInformations, DataGridChecker dataGridChecker, IDrawUnit polygonDrawUnit)
        {
            this.handler = handler;
            this.control = control;
            this.polygonInformations = polygonInformations;
            this.dataGridChecker = dataGridChecker;
            this.polygonDrawUnit = polygonDrawUnit;
            this.mousePositionInformation = new MousePositionInformation(control, polygonInformations);

        }

        public void setPossiblySelectedPolygonAndSelectedPoint(double x, double y)
        {
            setPossiblySelectedPoint(x, y);
            // we did not set it  in the method above
            if (polygonInformations.SelectedPolygon == null)
                setPossiblySelectedPolygon(x, y);
        }

        private void setPossiblySelectedPolygon(double x, double y)
        {
            PolygonLabel[] polygonLabels = control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>().ToArray();
            if (polygonLabels.Length == 0)
                return;

            foreach (PolygonLabel polygonLabel in polygonLabels)
            {
                if (arePointsInPolygonArea(new Point[] { new Point(x, y) }, polygonLabel.Polygon))
                {
                    polygonInformations.SelectedPolygon = polygonLabel;
                    polygonInformations.StartPosition = new Point(x, y);
                    return;
                }
            }
        }

        public bool selectOrUnselectDependentOnMousePos(double x, double y)
        {
            PolygonLabel[] polygonLabels = control.polygonListControl.polygonDataGrid.Items.Cast<PolygonLabel>().ToArray();
            if (polygonLabels.Length == 0)
                return true;

            PolygonLabel labelToChange = null;
            int counter = 0;
            int labelIndex = 0;
            foreach (PolygonLabel polygonLabel in polygonLabels)
            {                
                if (arePointsInPolygonArea(new Point[] { new Point(x, y) }, polygonLabel.Polygon))
                {
                    if(labelToChange == null)
                    {
                        labelToChange = polygonLabel;
                        labelIndex = counter;
                    }

                    if (control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>().Contains(polygonLabel))
                    {
                        labelToChange = polygonLabel;
                        labelIndex = counter;
                        break;
                    }
                }
                counter++;
            }

            if (labelToChange == null)
                return true;

            bool returnValue = true;
            if (!control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>().Contains(labelToChange))
            {
                if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                    control.polygonListControl.polygonDataGrid.SelectedItems.Clear();
                control.polygonListControl.polygonDataGrid.SelectedItems.Add(control.polygonListControl.polygonDataGrid.Items[labelIndex]);
                setPossiblySelectedPolygonAndSelectedPoint(x, y);
            }
            else
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    control.polygonListControl.polygonDataGrid.SelectedItems.Remove(control.polygonListControl.polygonDataGrid.Items[labelIndex]);
                    polygonInformations.restetInfos();
                    returnValue = false;
                }
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);

            return returnValue;
        }

        private bool arePointsInPolygonArea(Point[] points, List<PolygonPoint> polygon)
        {
            if (polygon != null)
            {
                using (var gp = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    System.Drawing.Point[] polygonPoints = new System.Drawing.Point[polygon.Count];

                    for (int i = 0; i < polygon.Count; i++)
                    {
                        PolygonPoint polygonPoint = polygon.ElementAt(i);
                        polygonPoints[i] = new System.Drawing.Point((int)polygonPoint.X, (int)polygonPoint.Y);
                    }

                    gp.AddPolygon(polygonPoints);

                    foreach (Point point in points)
                    {
                        if (!gp.IsVisible(new System.Drawing.Point((int)point.X, (int)point.Y)))
                            return false;
                    }
                }
            }

            return true;
        }

        private void setPossiblySelectedPoint(double x, double y)
        {
            PolygonLabel[] polygonLabels = control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>().ToArray();

            if (polygonLabels.Length > 0)
            {
                foreach (PolygonLabel polygonLabel in polygonLabels)
                {
                    foreach (PolygonPoint polygonPoint in polygonLabel.Polygon)
                    {
                        if (mousePositionInformation.isMouseAbovePoint(x, y, polygonPoint.X, polygonPoint.Y))
                        {
                            polygonInformations.StartPosition = new Point(polygonPoint.X, polygonPoint.Y);
                            polygonInformations.SelectedPoint = polygonPoint;
                            polygonInformations.SelectedPolygon = polygonLabel;
                            return;
                        }
                    }
                }
            }
        }

        public void updatePoint(double x, double y, AnnoListItem item)
        {
            foreach (PolygonPoint polygonPoint in polygonInformations.SelectedPolygon.Polygon)
            {
                if (polygonPoint.Equals(polygonInformations.SelectedPoint))
                {
                    if (x > polygonInformations.ImageWidth)
                        polygonPoint.X = polygonInformations.ImageWidth;
                    else if (x < 0)
                        polygonPoint.X = 0;
                    else
                        polygonPoint.X = x;

                    if (y > polygonInformations.ImageHeight)
                        polygonPoint.Y = polygonInformations.ImageHeight;
                    else if (y < 0)
                        polygonPoint.Y = 0;
                    else
                        polygonPoint.Y = y;

                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }
        }

        public void updateImageSize()
        {
            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
            {
                Tuple<int, int> imageSize = video.GetImageSize();
                polygonInformations.ImageWidth = (double)imageSize.Item1;
                polygonInformations.ImageHeight = (double)imageSize.Item2 - 5;
            }
        }

        public bool isPos5pxFromBottomAway(double y_val)
        {
            return polygonInformations.ImageHeight > y_val;
        }

        public bool controlPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        public void selectLabelsInSelectionRectangle(double xStart, double yStart, double xDestiny, double yDestiny)
        {
            List<PolygonPoint> rectangle = rectangleAsList(xStart, yStart, xDestiny, yDestiny);
            List<PolygonLabel> labelsToSelect = new List<PolygonLabel>();

            for (int i = 0; i < control.polygonListControl.polygonDataGrid.Items.Count; i++)
            {
                PolygonLabel label = (PolygonLabel)control.polygonListControl.polygonDataGrid.Items[i];
                Point[] currentPolygonAsArray = label.Polygon.Select(point => new Point(point.X, point.Y)).ToArray();
                if (arePointsInPolygonArea(currentPolygonAsArray, rectangle))
                    control.polygonListControl.polygonDataGrid.SelectedItems.Add(control.polygonListControl.polygonDataGrid.Items[i]);
            }
        }

        private List<PolygonPoint> rectangleAsList(double xStart, double yStart, double xDestiny, double yDestiny)
        {
            List<PolygonPoint> rectangle = new List<PolygonPoint>();

            rectangle.Add(new PolygonPoint(xStart, yStart));
            rectangle.Add(new PolygonPoint(xStart, yDestiny));
            rectangle.Add(new PolygonPoint(xDestiny, yDestiny));
            rectangle.Add(new PolygonPoint(xDestiny, yStart));

            return rectangle;
        }



        public void startSelectionRectangle(double x, double y)
        {
            polygonInformations.StartPosition = new Point(x, y);
            control.polygonListControl.polygonDataGrid.SelectedItem = null;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }


        public void polygonSelectItem(AnnoListItem item)
        {
            if (dataGridChecker.annoDGCountsZero())
                item = (AnnoListItem)control.annoListControl.annoDataGrid.Items[0];

            if (!controlInStacksSet)
            {
                controlInStacksSet = true;
                control.annoListControl.annoDataGrid.Items.Cast<AnnoListItem>()
                                                          .ToList()
                                                          .ForEach(element => element.UndoRedoStack.Control = control);
            }

            item.UndoRedoStack.updateMenuItems(item);
            control.polygonListControl.polygonDataGrid.ItemsSource = item.PolygonList.getRealList();
            this.polygonTableUpdate();
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void polygonTableUpdate()
        {
            control.polygonListControl.polygonDataGrid.Items.Refresh();
        }

        public void refreshAnnoDataGrid()
        {
            control.annoListControl.annoDataGrid.Items.Refresh();
        }

        public bool labelIsNotSelected(PolygonLabel pl)
        {
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                if (pl.Equals(control.polygonListControl.polygonDataGrid.SelectedItems[i]))
                {
                    return false;
                }
            }

            return true;
        }

        

        public void changeSelection(int newSelectedAnnoListItemIndex, AnnoListItem newSelectedAnnoListItem, PolygonLabel newSelectedPolygonLabel)
        {
            control.annoListControl.annoDataGrid.SelectedItem = newSelectedAnnoListItem;
            handler.jumpToGeometric(newSelectedAnnoListItemIndex);
            control.polygonListControl.polygonDataGrid.SelectedItem = newSelectedPolygonLabel;
        }

        public int getCurrentAnnoListIndex()
        {
            return control.annoListControl.annoDataGrid.SelectedIndex;
        }

        public int getCurrentLabelIndex()
        {
            return control.polygonListControl.polygonDataGrid.SelectedIndex;
        }

        public AnnoList getCurrentAnnoList()
        {
            return (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
        }


        public void selectSpecificLabel(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsNotZero() && this.labelIsNotSelected(pl))
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = pl;
            }
        }

        public bool polygonPositionsEqual(List<Point> start, List<Point> target)
        {
            if (start is null || target is null || start.Count != target.Count)
                return true;

            for (int i = 0; i < start.Count; i++)
            {
                if (start[i].X != target[i].X || start[i].Y != target[i].Y)
                    return false;
            }
            return true;
        }
    }
}
