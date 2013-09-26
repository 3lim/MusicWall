/****************************************************************************
*                                                                           *
*  OpenNI 1.x Alpha                                                         *
*  Copyright (C) 2011 PrimeSense Ltd.                                       *
*                                                                           *
*  This file is part of OpenNI.                                             *
*                                                                           *
*  OpenNI is free software: you can redistribute it and/or modify           *
*  it under the terms of the GNU Lesser General Public License as published *
*  by the Free Software Foundation, either version 3 of the License, or     *
*  (at your option) any later version.                                      *
*                                                                           *
*  OpenNI is distributed in the hope that it will be useful,                *
*  but WITHOUT ANY WARRANTY; without even the implied warranty of           *
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the             *
*  GNU Lesser General Public License for more details.                      *
*                                                                           *
*  You should have received a copy of the GNU Lesser General Public License *
*  along with OpenNI. If not, see <http://www.gnu.org/licenses/>.           *
*                                                                           *
****************************************************************************/


    ﻿using System;
    using System.Collections.Generic;
    using System.Text;
    using OpenNI;
    using System.Windows.Media.Media3D;
    using System.Windows.Media;
    using System.Windows;

    public class KinectData
	{

        public System.Windows.Media.Media3D.Point3D xYDepth;

		public KinectData()
		{
            string SAMPLE_XML_FILE = "C:/Program Files/OpenNI/Samples/SimpleRead.net/SamplesConfig.xml";
             
            ScriptNode scriptNode;
            Context context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            SceneAnalyzer scene = new SceneAnalyzer(context);
            OpenNI.Point3D wallNorm;
            OpenNI.Point3D wallPoint;
            double angleBetween = 0;
            double angleBetweenY = 0;
            int centerpointDepth = 0;
            int loadRuns = 50;

            // DepthGenerator
            DepthGenerator depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
			if (depth == null)
			{
				Console.WriteLine("Sample must have a depth generator!");
				return;
			}
            MapOutputMode mapMode = depth.MapOutputMode;
            DepthMetaData depthMD = new DepthMetaData();            
            SceneMetaData sceneMeta = new SceneMetaData();
            

            int xRes = (int) mapMode.XRes;
            int yRes = (int) mapMode.YRes;

            int[,] backgroundDepthMD = new int[xRes,yRes];

            Console.WriteLine("Press any key to stop...");

			while (!Console.KeyAvailable)
            {
                context.WaitOneUpdateAll(depth);               
                depth.GetMetaData(depthMD);

                // SceneAnalyzer

                if (depthMD.FrameID <= loadRuns)
                {
                    Background(depthMD, backgroundDepthMD, depthMD.FullXRes, depthMD.FullYRes, loadRuns);
                    
                    if (depthMD.FrameID == 1)
                        Console.WriteLine("loading background data... ", depthMD.FrameID * 10);
                        Console.Write(" .  ",(double) depthMD.FrameID / loadRuns * 100);
                        centerpointDepth = centerpointDepth + depthMD[(int)mapMode.XRes / 2, (int)mapMode.YRes / 2];
                    if (depthMD.FrameID == loadRuns)
                        centerpointDepth = centerpointDepth / loadRuns;
                }

                if (depthMD.FrameID == loadRuns)
                {
                    scene = new SceneAnalyzer(context);

                    if (scene == null)
                    {
                        Console.WriteLine("Retry!");
                    }
                    else
                    {
                        wallNorm = scene.Floor.Normal;
                        wallPoint = scene.Floor.Point;

                        Console.WriteLine("\n Wall Normal : {0}, {1}, {2}", wallNorm.X, wallNorm.Y, wallNorm.Z);
                        Console.WriteLine(" Wall Point: {0}, {1}, {2}", wallPoint.X, wallPoint.Y, wallPoint.Z);
                        Vector n1 = new Vector(0, 1);
                        Vector n2 = new Vector(wallNorm.Y, wallNorm.Z);
                        Vector n3 = new Vector(wallNorm.X, wallNorm.Z);

                        angleBetween =  (double)Vector.AngleBetween(n1, n2); // need to get the angle for the last axis in degrees
                        angleBetweenY = (double)Vector.AngleBetween(n1, n3); 

                        Console.WriteLine(" Angle: {0}", angleBetween);
                    }

                }
                if (depthMD.FrameID >= loadRuns + 1)
                {
                    

                    System.Windows.Media.Media3D.Point3D u_new,u_real, f, c;
                    c = new System.Windows.Media.Media3D.Point3D(0, 0, centerpointDepth);

                    OpenNI.Point3D newF;
                    newF = Change(depthMD, backgroundDepthMD); //returns the pixal pos 

                    if (newF.X != 0 && newF.Z != 0 && newF.Z != 0){

                        newF = depth.ConvertProjectiveToRealWorld(newF);

                        f = new System.Windows.Media.Media3D.Point3D(newF.X, newF.Y, newF.Z);        // the point of intress
                        u_new = Translation(f, c); //A translation that brings point 1 to the origin

                        float d = Distance(scene.Floor, newF);

                        double angleInRadians = (Math.PI / 180) * angleBetween; ;
                        double angleInRadiansY = (Math.PI / 180) * angleBetweenY; ;
                        var matrix = NewRotateAroundX(angleInRadians);      // Rotation around the origin by the required angle
                        matrix = NewRotateAroundY(angleInRadiansY);
                        u_real = Multiplication(matrix, u_new);


                        //Console.WriteLine("new U   {0}, {1}, {2} ", (int)u_new.X, (int)u_new.Y, (int)u_new.Z);
                        Console.WriteLine("frameID =  {0} u_real , {1}, {2} and {3} distance {4}", depthMD.FrameID, (int)u_real.X, (int)u_real.Y, (int)u_real.Z, d);
                        xYDepth = new System.Windows.Media.Media3D.Point3D(u_real.X, u_real.Y, d);

                    }
                }
            }
		}


        static private float Distance(Plane3D wall, OpenNI.Point3D newF)
        {
            float d,d_u,d_d;
            float a = wall.Normal.X;    float b = wall.Normal.Y;    float c = wall.Normal.Z;
            float x_p = wall.Point.X;   float y_p =  wall.Point.Y;  float z_p = wall.Point.Z;
            float x_0 = (float)newF.X;  float y_0 = (float)newF.Y;  float z_0 = (float)newF.Z;



            d_u = (float) Math.Abs( a * (x_0 - x_p) + b * (y_0 - y_p) + c * (z_0 - z_p));
            d_d = (float) Math.Sqrt(a * a + b * b + c * c);
            
            return d = d_u/d_d;
        }

        static private OpenNI.Point3D Change(DepthMetaData depthMD, int[,] backgroundDepthMD)
        {
            OpenNI.Point3D newF = new OpenNI.Point3D(0, 0, 0);
            bool skip = false;
            int checkUp = 0;

            for (int j = depthMD.FullYRes - 10; 10 < j; j--)
            {
                for (int i = 0; depthMD.FullXRes - 40 > i; i++)
                {
                    if (Math.Abs(depthMD[i, j] - backgroundDepthMD[i, j]) > 50)
                    {
                        checkUp = j - 50;
                        if (checkUp >= depthMD.FullYRes || checkUp <= 0)
                        {
                            skip = true;
                        }

                        if (skip == false && (Math.Abs(depthMD[i, checkUp] - backgroundDepthMD[i, checkUp]) > 50))
                        {
                            //Console.WriteLine("change at {0},{1} size {2}", i, j, Math.Abs(depthMD[i,  checkUp] - backgroundDepthMD[i, checkUp]));
                            newF = new OpenNI.Point3D(i, j, depthMD[i, j]);
                            j = 9; i = depthMD.FullXRes;
                        }
                        skip = false;                    }
                }
            }
            return newF;
        }

        static private void Background(DepthMetaData depthMD, int[,] backgroundDepthMD, int XRes, int YRes, int loadRuns)
        {
            for (int i = 0; XRes > i; i++)
            {
                for (int j = 0; YRes > j; j++)
                {

                    backgroundDepthMD[i, j] = depthMD[i, j] + backgroundDepthMD[i, j];
                }
            }
            if (depthMD.FrameID == loadRuns)
            {
                for (int i = 0; XRes > i; i++)
                {
                    for (int j = 0; YRes > j; j++)
                    {

                        backgroundDepthMD[i, j] = backgroundDepthMD[i, j] / loadRuns;
                    }
                }
            }

        }

        public static System.Windows.Media.Media3D.Point3D Multiplication(Matrix3D matrix1, System.Windows.Media.Media3D.Point3D point3D)
        {

            var x = point3D.X * matrix1.M11 +
                    point3D.Y * matrix1.M12 +
                    point3D.Z * matrix1.M13;

            var y = point3D.X * matrix1.M21 +
                    point3D.Y * matrix1.M22 +
                    point3D.Z * matrix1.M23;

            var z = point3D.X * matrix1.M31 +
                    point3D.Y * matrix1.M32 +
                    point3D.Z * matrix1.M33;

            point3D.X = x;
            point3D.Y = y;
            point3D.Z = z;
            return point3D;

        }

        public static Matrix3D NewRotate(double radiansX, double radiansY, double radiansZ)
        {

            var matrix = NewRotateAroundX(radiansX);

            matrix = matrix * NewRotateAroundY(radiansY);

            matrix = matrix * NewRotateAroundZ(radiansZ);

            return matrix;

        }

        public static Matrix3D NewRotateAroundX(double radians)
        {
            var matrix = new Matrix3D();
            matrix.M22 = Math.Cos(radians);
            matrix.M23 = Math.Sin(radians);
            matrix.M32 = -(Math.Sin(radians));
            matrix.M33 = Math.Cos(radians);
            return matrix;
        }

        public static Matrix3D NewRotateAroundY(double radians)
        {
            var matrix = new Matrix3D();
            matrix.M11 = Math.Cos(radians);
            matrix.M13 = -(Math.Sin(radians));
            matrix.M31 = Math.Sin(radians);
            matrix.M33 = Math.Cos(radians);
            return matrix;
        }

        public static Matrix3D NewRotateAroundZ(double radians)
        {
            var matrix = new Matrix3D();
            matrix.M11 = Math.Cos(radians);
            matrix.M12 = Math.Sin(radians);
            matrix.M21 = -(Math.Sin(radians));
            matrix.M22 = Math.Cos(radians);
            return matrix;
        }

        public static System.Windows.Media.Media3D.Point3D Translation(System.Windows.Media.Media3D.Point3D first, System.Windows.Media.Media3D.Point3D second)
        {
            
            System.Windows.Media.Media3D.Point3D ans = new System.Windows.Media.Media3D.Point3D(first.X - second.X,first.Y - second.Y,first.Z - second.Z);

            return ans;
        }

       

        
}

