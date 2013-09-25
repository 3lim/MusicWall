using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicWall
{
    /// <summary>
    /// A point on the screen, in the plan starting from bottom left corner.<br/>
    /// A point is meant to be part of a curve.<br/>
    /// The size is used to note the intensity of the point wanted by the user.
    /// </summary>
    class Point
    {
        public double x;
        public double y;
        public int size { get; private set; }

        public Point(double x, double y)
       {
           this.x = x;
           this.y = y;
           this.size = 1;
       }

       public void IncreaseSize()
       {
           this.size++;
       }

       public String ToString()
       {
           return "[" + x + "; " + y + "]";
       }
    }
}
