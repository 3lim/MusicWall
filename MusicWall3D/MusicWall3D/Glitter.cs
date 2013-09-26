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
        public Glitter(int x, int y, int life, GraphicsDeviceManager gdm, int seed): base(x, y, life, gdm, seed)
		{            
            velocity.X = ran.NextFloat(-0.02f, 0.02f); ;
			velocity.Y = 0.06f;
			acceleration.X = -0.0001f;
			acceleration.Y = -0.001f;

            color.Red = ran.NextFloat(0.2f, 0.6f);
            color.Green = ran.NextFloat(0.0f, 0.4f);
            color.Blue = ran.NextFloat(0.4f, 1.0f);
            color.Alpha = 1.0f;        
    
		}

        public override Color4 getColor()
        {
            /*color.Red = ran.NextFloat(0.2f, 1.0f);
            color.Green = ran.NextFloat(0.0f, 0.2f);
            color.Blue = ran.NextFloat(0.2f, 1.0f);
            color.Alpha = 1;*/

            return color;

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

