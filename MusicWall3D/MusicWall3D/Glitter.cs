using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;


namespace ParticleLibrary
{
	public class Glitter: Particle
	{   
        public Glitter(float x, float y, int life, int seed, int type, int cT): base(x, y, life, seed, type, cT)
		{
            velocity.X = ran.NextFloat(-0.0004f, 0.0002f);
            velocity.Y = -0.0003f;            			
			acceleration.X = -0.000005f;
			acceleration.Y = 0.00001f;
            setColor(cT);

            float f = ran.NextFloat(0.0f, 0.3f);
      
    
		}

        public override Color4 getColor()
        {
            /*color.Red = ran.NextFloat(0.2f, 1.0f);
            color.Green = ran.NextFloat(0.0f, 0.2f);
            color.Blue = ran.NextFloat(0.2f, 1.0f);
            color.Alpha = 1;*/

            return color;

        }
        public void setColor(int c)
        {
            if (c == 0)
            {
                color.Red = ran.NextFloat(0.2f, 0.6f);
                color.Green = ran.NextFloat(0.4f, 1.4f);
                color.Blue = ran.NextFloat(0.2f, 0.6f);
                color.Alpha = 1.0f;

            }
            else if (c == 1)
            {
                color.Red = ran.NextFloat(0.6f, 1.6f);
                color.Green = ran.NextFloat(0.0f, 0.2f);
                color.Blue = ran.NextFloat(0.4f, 0.8f);
                color.Alpha = 1.0f;
            }
            else if (c == 2)
            {
                color.Red = ran.NextFloat(0.2f, 0.5f);
                color.Green = 0.0f;//ran.NextFloat(0.0f, 0.0f);
                color.Blue = ran.NextFloat(0.2f, 0.8f);
                color.Alpha = 1.0f;
            }
            else if (c == 3)
            {
                color.Red = ran.NextFloat(0.0f, 0.2f);
                color.Green = ran.NextFloat(0.0f, 0.2f);
                color.Blue = ran.NextFloat(0.4f, 1.0f);
                color.Alpha = 1.0f;
            }

            else
            {
                float f = ran.NextFloat(0.0f, 0.3f);
                color.Red = ran.NextFloat(0.2f, 0.6f);
                color.Green = ran.NextFloat(0.0f, 0.4f);
                color.Blue = ran.NextFloat(0.4f, 1.0f);
                color.Alpha = 1.0f;
            }
        }

        public Vector2 getAcceleration()
        {
            return this.acceleration;
        }

        public override GeometricPrimitive getShape()
        {
            return this.shape;
        }

        public void setAcceleration(Vector2 v)
        {

        }
	}
}

