﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class SegmentationListItem : IObservableListItem
    {
        private int[,] mask;
        private string label;
        private double confidence;

        public int[,] getMask()
        {
            return mask;
        }

        public void createMask(int width, int height)
        {
            mask = new int[width, height];
            Random rnd = new Random();
            
            for (int y = 0; y < getHeight(); ++y)
            {
                int data = 0;
                for (int x = 0; x < getWidth(); ++x)
                {
                    //mask[x, y] = rnd.Next(0, 255);
                    //mask[x, y] = 0;
                    mask[x, y] = data;
                    data = (data + 1 > 255) ? 0 : ++data;
                }
            }
            OnPropertyChanged("Mask");
        }

        public void setMask(int[,] m)
        {
            mask = m;
            OnPropertyChanged("Mask");
        }

        public bool setPixel(int col, int row, int value)
        {
            if (col < getWidth() && row < getHeight())
            {
                mask[col, row] = value;
                OnPropertyChanged("Mask");
                return true;
            }
            else return false;
        }

        public int getWidth()
        {
            int x = mask.GetLength(0);
            return x;
        }

        public int getHeight()
        {
            int x = mask.GetLength(1);
            return x;
        }

        public int getSize()
        {
            return getWidth() * getHeight();
        }

        public double Confidence
        {
            get { return confidence; }
            set
            {
                confidence = value;
                OnPropertyChanged("Confidence");
            }
        }

        public string Label
        {
            get { return label; }
            set
            {
                label = value;
                OnPropertyChanged("Label");
            }
        }

        public SegmentationListItem(int width, int height, string label, double confidence)
        {
            createMask(width, height);
            this.label = label;
            this.confidence = confidence;
        }

        public class SegmentationListItemComparer : IComparer<SegmentationListItem>
        {
            int IComparer<SegmentationListItem>.Compare(SegmentationListItem a, SegmentationListItem b)
            {

                if (a.getSize() < b.getSize())
                {
                    return -1;
                }
                else if (a.getSize() > b.getSize())
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}