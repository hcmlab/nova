using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Tools.Polygon_Helper
{
    class PolygonRevisor
    {
        public PolygonRevisor()
        {
            
        }

        public bool isPolygonComplex(List<PolygonPoint> polygon)
        {
            PolygonPoint interval1Point1 = new PolygonPoint(0, 0);
            PolygonPoint interval1Point2 = new PolygonPoint(0, 0);
            PolygonPoint interval2Point1 = new PolygonPoint(0, 0);
            PolygonPoint interval2Point2 = new PolygonPoint(0, 0);

            PolygonPoint interval1veryFirst = new PolygonPoint(0, 0);
            PolygonPoint interval1veryLast = new PolygonPoint(0, 0);
            PolygonPoint interval2veryFirst = new PolygonPoint(0, 0);
            PolygonPoint interval2veryLast = new PolygonPoint(0, 0);

            bool interval1firstSet = false;
            bool interval2firstSet = false;

            foreach (PolygonPoint point1 in polygon)
            {
                if (!interval1firstSet)
                {
                    interval1firstSet = true;
                    interval1Point1 = point1;
                    interval1veryFirst = point1;
                    continue;
                }

                interval1veryLast = point1;
                interval1Point2 = interval1Point1;
                interval1Point1 = point1;

                foreach (PolygonPoint point2 in polygon)
                {
                    if (!interval2firstSet)
                    {
                        interval2firstSet = true;
                        interval2Point1 = point2;
                        interval2veryFirst = point2;
                        continue;
                    }

                    interval2veryLast = point2;
                    interval2Point2 = interval2Point1;
                    interval2Point1 = point2;
                    if (!tupleAreEqual(interval1Point1, interval1Point2, interval2Point1, interval2Point2))
                        if (intervalsAreCutting(interval1Point1, interval1Point2, interval2Point1, interval2Point2))
                        {
                            return true;
                        }
                }

                if (!tupleAreEqual(interval1Point1, interval1Point2, interval2veryFirst, interval2veryLast))
                    if (intervalsAreCutting(interval1Point1, interval1Point2, interval2veryFirst, interval2veryLast))
                    {
                        return true;
                    }

                interval2firstSet = false;
            }

            foreach (PolygonPoint point2 in polygon)
            {
                if (!interval2firstSet)
                {
                    interval2firstSet = true;
                    interval2Point1 = point2;
                    interval2veryFirst = point2;
                    continue;
                }

                interval2veryLast = point2;
                interval2Point2 = interval1Point1;
                interval2Point1 = point2;
                if (!tupleAreEqual(interval1veryFirst, interval1veryLast, interval2Point1, interval2Point2))
                    if (intervalsAreCutting(interval1veryFirst, interval1veryLast, interval2Point1, interval2Point2))
                    {
                        return true;
                    }
            }

            if (!tupleAreEqual(interval1veryFirst, interval1veryLast, interval2veryFirst, interval2veryLast))
                if (intervalsAreCutting(interval1veryFirst, interval1veryLast, interval2veryFirst, interval2veryLast))
                {
                    return true;
                }

            return false;
        }

        private bool intervalsAreCutting(PolygonPoint A, PolygonPoint B, PolygonPoint C, PolygonPoint D)
        {
            if (ccw(A, C, D) != ccw(B, C, D) &&
                ccw(A, B, C) != ccw(A, B, D))
            {
                if (A != C && A != D &&
                   B != C && B != D)
                {
                    return true;
                }
                else if (intervalsHaveSameAngle(A, B, C, D))
                {
                    return true;
                }
            }

            return false;
        }

        private bool intervalsHaveSameAngle(PolygonPoint interval1Point1, PolygonPoint interval1Point2, PolygonPoint interval2Point1, PolygonPoint interval2Point2)
        {
            double xDiff = interval1Point2.X - interval1Point1.X;
            double yDiff = interval1Point2.Y - interval1Point1.Y;
            double a1 = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            xDiff = interval2Point2.X - interval2Point1.X;
            yDiff = interval2Point2.Y - interval2Point1.Y;
            double a2 = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;

            return a2 == a1;
        }

        private bool ccw(PolygonPoint A, PolygonPoint B, PolygonPoint C)
        {
            return (C.Y - A.Y) * (B.X - A.X) > (B.Y - A.Y) * (C.X - A.X);
        }

        private bool tupleAreEqual(PolygonPoint interval1Point1, PolygonPoint interval1Point2, PolygonPoint interval2Point1, PolygonPoint interval2Point2)
        {
            if (interval1Point1 == interval2Point1 && interval1Point2 == interval2Point2)
                return true;

            if (interval1Point1 == interval2Point2 && interval1Point2 == interval2Point1)
                return true;

            return false;
        }
    }
}
