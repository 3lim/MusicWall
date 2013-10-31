using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        static SharpDX.Vector2 InterpolateSpline(float t, SharpDX.Vector2 p0, SharpDX.Vector2 p1, SharpDX.Vector2 p2, SharpDX.Vector2 p3)
        {
            float cx = 3 * (p1.X - p0.X);
            float bx = 3 * (p2.X - p1.X) - cx;
            float ax = p3.X - p0.X - cx - bx;

            float cy = 3 * (p1.Y - p0.Y);
            float by = 3 * (p2.Y - p1.Y) - cy;
            float ay = p3.Y - p0.Y - cy - by;

            float tSquared = t * t;
            float tCubed = tSquared * t;
            float resultX = (ax * tCubed) + (bx * tSquared) + (cx * t) + p0.X;
            float resultY = (ay * tCubed) + (by * tSquared) + (cy * t) + p0.Y;

            return new SharpDX.Vector2(resultX, resultY);
        }

        static SharpDX.Vector2 InterpolateLine(float t, SharpDX.Vector2 p0, SharpDX.Vector2 p1)
        {
            return p0+t*(p1 - p0);
        }

        private struct Segment
        {
            public SharpDX.Vector2? p0;
            public SharpDX.Vector2? p1;
            public SharpDX.Vector2? p2;
            public SharpDX.Vector2? p3;

            public SharpDX.Vector2 sample(float t)
            {
                if (p2 == null) return InterpolateLine(t, (SharpDX.Vector2)p0, (SharpDX.Vector2)p1);
                else return InterpolateSpline(t, (SharpDX.Vector2)p0, (SharpDX.Vector2)p1, (SharpDX.Vector2)p2, (SharpDX.Vector2)p3);
            }
        }

        private List<Segment> segments;

        public List<SharpDX.Vector2> pList
        { get; private set; }

        private float lastFrequency = 0.01f;
        private List<SharpDX.Vector2> lastSampled;
        float lastDist = 0.0f;
        int lastSegmentSampled = 0;

        public Spline(float startX, float startY)
        {
            pList = new List<SharpDX.Vector2>();
            pList.Add(new SharpDX.Vector2(startX, startY));
            segments = new List<Segment>();

            lastSampled = new List<SharpDX.Vector2>();
            lastSampled.Add(new SharpDX.Vector2(startX, startY));
        }

        public void addPoint(float x, float y)
        {
            pList.Add(new SharpDX.Vector2(x,y));
            //pointList.Add(new Point(x, y));

            if ((pList.Count - 1) % 3 == 1)
            {
                segments.Add(new Segment { p0 = pList[pList.Count - 2], p1 = pList[pList.Count - 1] });
                //pGeo.Figures[0].Segments.Add(new LineSegment(new Point(x, y), false));
            }

            if ((pList.Count - 1) % 3 == 2)
            {
                segments.RemoveAt(segments.Count - 1);
                //pGeo.Figures[0].Segments.RemoveAt(pGeo.Figures[0].Segments.Count - 1);

                double x1, y1;
                bez3pts1(pList[pList.Count - 3].X, pList[pList.Count - 3].Y, pList[pList.Count - 2].X, pList[pList.Count - 2].Y, pList[pList.Count - 1].X, pList[pList.Count - 1].Y, out x1, out y1);
                
                SharpDX.Vector2 oldp1 = new SharpDX.Vector2((float)x1,(float)y1);
                SharpDX.Vector2 newp1 = 1.0f/3.0f*pList[pList.Count - 3] + 2.0f/3.0f*oldp1;
                SharpDX.Vector2 newp2 = 2.0f/3.0f*oldp1 + 1.0f/3.0f*pList[pList.Count - 1];

                segments.Add(new Segment { p0 = pList[pList.Count - 3], p1 = newp1, p2 = newp2, p3 = pList[pList.Count - 1] });
                //QuadraticBezierFromIntersection(pGeo.Figures[0], pointList[pointList.Count - 3], pointList[pointList.Count - 2], pointList[pointList.Count - 1]);
            }

            if ((pList.Count - 1) % 3 == 0)
            {
                segments.RemoveAt(segments.Count - 1);
                //pGeo.Figures[0].Segments.RemoveAt(pGeo.Figures[0].Segments.Count - 1);

                double x1, y1, x2, y2;
                bez4pts1(pList[pList.Count - 4].X, pList[pList.Count - 4].Y, pList[pList.Count - 3].X, pList[pList.Count - 3].Y, pList[pList.Count - 2].X, pList[pList.Count - 2].Y, pList[pList.Count - 1].X, pList[pList.Count - 1].Y, out x1, out y1, out x2, out y2);

                segments.Add(new Segment { p0 = pList[pList.Count - 4], p1 = new SharpDX.Vector2((float)x1, (float)y1), p2 = new SharpDX.Vector2((float)x2, (float)y2), p3 = pList[pList.Count - 1] });
                //BezierFromIntersection(pGeo.Figures[0], pointList[pointList.Count - 4], pointList[pointList.Count - 3], pointList[pointList.Count - 2], pointList[pointList.Count - 1]);
            }

        }

        public List<SharpDX.Vector2> sample(float frequency)
        {
            
            List<SharpDX.Vector2> ret = new List<SharpDX.Vector2>(lastSampled);
            
            SharpDX.Vector2 lastPoint = lastSampled[lastSampled.Count-1];

            //ret.Add(lastPoint);

            if (pList.Count == 1) return ret;

            float fraction = lastSegmentSampled / (float)segments.Count;

            int currentSegment = lastSegmentSampled;
            while (fraction + lastFrequency <= 1.0f)
            {
                currentSegment += fraction + lastFrequency > (currentSegment + 1) / (float)segments.Count ? 1 : fraction + lastFrequency < currentSegment / (float)segments.Count ? -1 : 0;
                
                SharpDX.Vector2 nP = segments[currentSegment].sample((fraction + lastFrequency - currentSegment / (float)segments.Count) * (float)segments.Count);
                
                SharpDX.Vector2 d = nP - lastPoint;

                if (Math.Abs(d.Length() - frequency) <= frequency / 4.0f)
                {
                    if(currentSegment != segments.Count-1) lastSampled.Add(new SharpDX.Vector2((float)nP.X, (float)nP.Y));

                    ret.Add(new SharpDX.Vector2((float)nP.X, (float)nP.Y));
                    fraction += lastFrequency;
                    lastDist += (float)d.Length();
                    lastPoint = nP;
                }
                else
                {
                    lastFrequency *= (d.Length() > frequency) ? 0.5f : 1.5f;
                }
            }

            lastSegmentSampled = currentSegment;
            ret.Add(pList[pList.Count - 1]);

            return ret;
        }
    }
}
