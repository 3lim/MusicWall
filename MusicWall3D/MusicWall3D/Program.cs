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
            {
                program.Run();
            }

        }
    }
}