using System;
using System.Collections.Generic;
using System.Windows;

namespace MusicWall3D
{
    /// <summary>
    /// Simple MusicWall3D application using SharpDX.Toolkit.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
#if NETFX_CORE
        [MTAThread]
#else
        [STAThread]
#endif
        static void Main()
        {
            using (var program = new MusicWall3D())
                program.Run();

            //Spline s = new Spline(0.1f, 0.2f);
            //s.addPoint(0.5f, 0.7f);
            //s.addPoint(0.3f, 0.1f);
            //s.addPoint(0.37f, 0.5f);
            //s.addPoint(0.32f, 0.2f);
            //s.addPoint(0.8f, 0.3f);
            //s.addPoint(0.1f, 0.5f);

            //List<Point> lp = s.sample(0.02f);

            //foreach (Point p in lp)
            //{
            //    Console.WriteLine(p);
            //}
        }
    }
}