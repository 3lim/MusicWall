using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using SharpDX.Direct2D1;
namespace ParticleLibrary
{
	public class Star : ParticleType
	{
		public Star ()
		{
			velocity [0] = 5;
			velocity [1] = 5;
			acceleration [0] = 5;
			acceleration [1] = 5;

            //shape = new Vector3[10];
		}

        public override Color4 getColor()
        {
            Random ran = new Random();
            color.Red = ran.NextFloat(0.0f, 1.0f);
            color.Green = ran.NextFloat(0.0f, 1.0f);
            color.Blue = ran.NextFloat(0.0f, 1.0f);
            color.Alpha = 1.0f;

            return color;

        }

	}
}

