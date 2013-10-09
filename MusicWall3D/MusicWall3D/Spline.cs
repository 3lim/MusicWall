using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Drawing;

namespace MusicWall3D
{
    class Spline
    {
        static void solvexy(double a, double b, double c, double d, double e, double f, out double i, out double j)
        {
            j = (c - a / d * f) / (b - a * e / d);
            i = (c - (b * j)) / a;
        }

        static double b0(double t) { return Math.Pow(1 - t, 3); }
        static double b1(double t) { return t * (1 - t) * (1 - t) * 3; }
        static double b2(double t) { return (1 - t) * t * t * 3; }
        static double b3(double t) { return Math.Pow(t, 3); }

        static void bez4pts1(double x0, double y0, double x4, double y4, double x5, double y5, double x3, double y3, out double x1, out double y1, out double x2, out double y2)
        {
            // find chord lengths
            double c1 = Math.Sqrt((x4 - x0) * (x4 - x0) + (y4 - y0) * (y4 - y0));
            double c2 = Math.Sqrt((x5 - x4) * (x5 - x4) + (y5 - y4) * (y5 - y4));
            double c3 = Math.Sqrt((x3 - x5) * (x3 - x5) + (y3 - y5) * (y3 - y5));
            // guess "best" t
            double t1 = c1 / (c1 + c2 + c3);
            double t2 = (c1 + c2) / (c1 + c2 + c3);
            // transform x1 and x2
            solvexy(b1(t1), b2(t1), x4 - (x0 * b0(t1)) - (x3 * b3(t1)), b1(t2), b2(t2), x5 - (x0 * b0(t2)) - (x3 * b3(t2)), out x1, out x2);
            // transform y1 and y2
            solvexy(b1(t1), b2(t1), y4 - (y0 * b0(t1)) - (y3 * b3(t1)), b1(t2), b2(t2), y5 - (y0 * b0(t2)) - (y3 * b3(t2)), out y1, out y2);
        }

        static void bez3pts1(double x0, double y0, double x3, double y3, double x2, double y2, out double x1, out double y1)
        {
            // find chord lengths
            double c1 = Math.Sqrt((x3 - x0) * (x3 - x0) + (y3 - y0) * (y3 - y0));
            double c2 = Math.Sqrt((x3 - x2) * (x3 - x2) + (y3 - y2) * (y3 - y2));
            // guess "best" t
            double t = c1 / (c1 + c2);
            // quadratic Bezier is B(t) = (1-t)^2*P0 + 2*t*(1-t)*P1 + t^2*P2
            // solving gives P1 = [B(t) - (1-t)^2*P0 - t^2*P2] / [2*t*(1-t)] where P3 is B(t)
            x1 = (x3 - (1 - t) * (1 - t) * x0 - t * t * x2) / (2 * t * (1 - t));
            y1 = (y3 - (1 - t) * (1 - t) * y0 - t * t * y2) / (2 * t * (1 - t));
        }

        static public void BezierFromIntersection(PathFigure p, Point startPt, Point int1, Point int2, Point endPt)
        {
            double x1, y1, x2, y2;
            bez4pts1(startPt.X, startPt.Y, int1.X, int1.Y, int2.X, int2.Y, endPt.X, endPt.Y, out x1, out y1, out x2, out y2);
            p.Segments.Add(new BezierSegment { Point1 = new Point(x1, y1), Point2 = new Point(x2, y2), Point3 = endPt });
        }

        static public void QuadraticBezierFromIntersection(PathFigure p, Point startPt, Point int1, Point endPt)
        {
            double x1, y1;
            bez3pts1(startPt.X, startPt.Y, int1.X, int1.Y, endPt.X, endPt.Y, out x1, out y1);
            p.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(x1, y1), Point2 = endPt });
        }

        private PathGeometry pGeo;
        private List<Point> pointList;
        private float lastFrequency = 0.01f;

        public Spline(float startX, float startY)
        {
            pointList = new List<Point>();
            pointList.Add(new Point(startX, startY));
            pGeo = new PathGeometry();
            pGeo.Figures.Add(new PathFigure { StartPoint = new Point(startX, startY) });
        }

        public void addPoint(float x,float y)
        {
            pointList.Add(new Point(x, y));

            if ((pointList.Count-1) % 3 == 1)
            {
                pGeo.Figures[0].Segments.Add(new LineSegment(new Point(x, y), false));
            }

            if ((pointList.Count - 1) % 3 == 2)
            {
                pGeo.Figures[0].Segments.RemoveAt(pGeo.Figures[0].Segments.Count - 1);

                QuadraticBezierFromIntersection(pGeo.Figures[0], pointList[pointList.Count - 3], pointList[pointList.Count - 2], pointList[pointList.Count - 1]);
            }

            if ((pointList.Count - 1) % 3 == 0)
            {
                pGeo.Figures[0].Segments.RemoveAt(pGeo.Figures[0].Segments.Count - 1);

                BezierFromIntersection(pGeo.Figures[0], pointList[pointList.Count - 4], pointList[pointList.Count - 3], pointList[pointList.Count - 2], pointList[pointList.Count - 1]);
            }

        }

        public List<SharpDX.Vector2> sample(float frequency)
        {
            List<SharpDX.Vector2> ret = new List<SharpDX.Vector2>();
            Point lastPoint = pGeo.Figures[0].StartPoint;
            ret.Add(new SharpDX.Vector2((float)lastPoint.X,(float)lastPoint.Y));

            if (pointList.Count == 1) return ret;

            float fraction = 0.0f;

            while (fraction + lastFrequency <= 1.0f)
            {
                Point currentP,currentT;

                pGeo.GetPointAtFractionLength((double)(fraction + lastFrequency), out currentP, out currentT);

                Vector dist = currentP - lastPoint;

                if (Math.Abs(dist.Length - frequency) <= frequency / 4)
                {
                    ret.Add(new SharpDX.Vector2((float)currentP.X, (float)currentP.Y));
                    fraction += lastFrequency;
                    lastPoint = currentP;
                }
                else
                {
                    lastFrequency *= (dist.Length > frequency) ? 0.5f : 1.5f;
                }
            }

            Point t;
            pGeo.GetPointAtFractionLength(1.0, out lastPoint, out t);
            ret.Add(new SharpDX.Vector2((float)lastPoint.X, (float)lastPoint.Y));

            return ret;
        }
    }
}
