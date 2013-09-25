using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicWall
{
    class DataManager
    {
        // TODO: constants below are arbitrary !!! Test and adapt


        // FROM SCREEN TO DATA
        private const int SCREEN_HEIGHT = 640; // screen resolution
        private const int SCREEN_LENGTH = 640;
        private const int PIX_LENGTH = 1; // reformatting the grid to reduce noise
        private const int PIX_HEIGHT = 1;

        // UPDATE LIMITS
        private const long DELTA_TIME = 100; // limit time (ticks) between 2 inputs beyond what the points will be considered
        // as being part of 2 different lines
        private const long DELTA_DIST = 200; // limit distance in pixel between 2 inputs beyond what the points will be considered
        // as being part of 2 different lines

        // HANDLING DIRECTION OF THE MOVEMENT
        private const int NO_DIRECTION = 0;
        private const int LEFT_TO_RIGHT = 1;
        private const int RIGHT_TO_LEFT = 2;

        // CURRENT POSITION OF THE HAND
        private double curX;
        private double curY;
        private double distToWall;
        private const int LIMIT_DIST_FOR_DRAWING = 200;
        private Boolean isDrawing;

        // THE CURVES
        private Queue<Wip> curves = new Queue<Wip>();
        private const double LOESS_BANDWIDTH = 0.6;
        private const int LOESS_ITER = 2;

        public DataManager()
        {
        }

        /// <summary>
        /// Stores the point specified by (screenX, screenY) coordinates in the data structure.<br/>
        /// Determines the curve it belongs to according to the nearest last point of the curves existing.<br/>
        /// </summary>
        /// <param name="screenX"></param>
        /// <param name="screenY"></param>
        /// <param name="time">time in ms</param>
        public void AddInput(double screenX, double screenY, double distToWall, long time)
        {
            // transform screen coordinates
            double x;
            double y;
            adaptCoordinates(screenX, screenY, out x, out y);

            // update current position
            this.curX = x;
            this.curY = y;
            this.distToWall = distToWall;
            Boolean wasDrawing = this.isDrawing;
            this.isDrawing = distToWall < LIMIT_DIST_FOR_DRAWING;

            // check if there is a line to continue
            if (wasDrawing)
            {
                Wip w = curves.ElementAt(curves.Count - 1);
                if (time - w.time < DELTA_TIME && isPointWithinRange(x, y, w.lastPoint()))
                {
                    completeLine(w, x, y, time);
                    return;
                }
            }

            // new line
            insertNewLine(x, y, time);

        }

        /// <summary>
        /// FOR DIRECT DISPLAY: get the current position of the hand.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="dist">distance to the wall</param>
        /// <returns>TRUE if the user is currently drawing, FALSE otherwise</returns>
        public Boolean GetPosition(out double x, out double y, out double dist)
        {
            x = this.curX;
            y = this.curY;
            dist = this.distToWall;
            return isDrawing;
        }

        /// <summary>
        /// FOR STORAGE: get the finished curves. We smooth the curve with LOESS algorithm.
        /// </summary>
        /// <param name="curve">a finished curve. Points are ordered by X coordinate</param>
        /// <returns>TRUE if there is another finished curve ready right now, FALSE otherwise</returns>
        public Boolean GetInput(out List<Point> curve)
        {
            curve = null;
            if (curves.Count == 0) { return false; }

            if (DateTime.Now.Ticks - curves.Peek().time > DELTA_TIME)
            {
                curve = curves.Dequeue().line;

                // smoothing the curve with LOESS
                int n = curve.Count;
                double[] xVal = new double[n];
                double[] yVal = new double[n];
                for(int i=0; i<n; i++)
                {
                    Point p = curve.ElementAt(i);
                    xVal[i] = p.x;
                    yVal[i] = p.y;
                }
                Loess l = new Loess(LOESS_BANDWIDTH, LOESS_ITER);
                double[] ySmooth = l.smooth(xVal, yVal);

                for(int i=0; i<n; i++)
                {
                    Point p = curve.ElementAt(i);
                    p.y = ySmooth[i];
                }
            }
            return curves.Count == 0 || curves.Peek().time < DELTA_TIME ? false : true;
        }

        public String ToString()
        {
            String toString = "";
            foreach (Wip w in curves)
            {
                foreach (Point p in w.line)
                {
                    toString += p.ToString() + " ";
                }
                toString += "\n";
            }
            return toString;
        }

        /// <summary>
        /// Reverses coordinates from screen (screen from starting from top left corner => data from bottom left).<br/>
        /// Lowers the resolution to reduce noise.
        /// </summary>
        /// <param name="screenX"></param>
        /// <param name="screenY"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void adaptCoordinates(double screenX, double screenY, out double x, out double y)
        {
            x = screenX / PIX_LENGTH;
            y = (SCREEN_HEIGHT - screenY) / PIX_LENGTH;
        }


        private Boolean isPointWithinRange(double x, double y, Point p)
        {
            return (p.x - x) * (p.x - x) + (p.y - y) * (p.y - y) < DELTA_DIST * DELTA_DIST;
        }



        private void insertNewLine(double x, double y, long time)
        {
            List<Point> line = new List<Point>();
            line.Add(new Point(x, y));
            Wip w = new Wip(line, time);
            curves.Enqueue(w);
        }

        private void completeLine(Wip w, double x, double y, long time)
        {
            // TODO: move w at the top of the window if handling drawing several lines at the same time

            Point lastPoint = w.lastPoint();
            // --- check that this is not the same point as the previous one
            if (lastPoint.x == x && lastPoint.y == y)
            {
                lastPoint.IncreaseSize();
                return;
            }

            // --- check that X coordinate is not a change of direction

            if (w.direction == NO_DIRECTION)
            {
                if (x > lastPoint.x) { w.direction = LEFT_TO_RIGHT; }
                else { w.direction = RIGHT_TO_LEFT; }
            }
            else if ((w.direction == LEFT_TO_RIGHT && x < lastPoint.x) || (w.direction == RIGHT_TO_LEFT && x > lastPoint.x))
            {
                // if change of direction, begin a new line
                insertNewLine(x, y, time);
                return;
            }

            // add the new point ...
            // --- if the three last points are aligned: remove the current last point before adding the new one
            Point lastBoPoint = w.lastBoPoint();
            double coef1 = (double)(y - lastBoPoint.y) / (double)(x - lastBoPoint.x);
            double coef2 = (double)(lastPoint.y - lastBoPoint.y) / (double)(lastPoint.x - lastBoPoint.x);
            if (Math.Abs(1 - coef1 / coef2) < 0.05) { w.line.Remove(lastPoint); }

            Point newPoint = new Point(x, y);
            if (w.direction == LEFT_TO_RIGHT || w.direction == NO_DIRECTION) { w.line.Add(newPoint); }
            else { w.line.Insert(0, newPoint); }
        }

        /// <summary>
        /// Wip: class handling a curve in progress. Stores the points, the time of the last update, and the direction.
        /// </summary>
        private class Wip
        {
            public List<Point> line;
            public long time;
            public int direction = NO_DIRECTION; // a line can be drawn in only one direction (left to right OR right to left, but
            // no change allowed when drawing)

            public Wip(List<Point> line, long time)
            {
                this.line = line;
                this.time = time;
            }

            public Point lastPoint()
            {
                return direction == RIGHT_TO_LEFT ? line.First() : line.Last();
            }

            public Point lastBoPoint()
            {
                return direction == RIGHT_TO_LEFT ? line.ElementAt(1) : line.ElementAt(line.Count - 2);
            }
        }

    }



}
