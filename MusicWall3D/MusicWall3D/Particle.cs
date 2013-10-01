using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace ParticleLibrary
{

	public class Particle : Game
	{

        public Vector2 acceleration;
        public Vector2 velocity;
        public Color4 color;
        //public Vector3[] shape; 
        public GeometricPrimitive shape;
        public float xpos;
        public float ypos;
        public int lifespan;
        public int sizeX;
        public int sizeY;
        public Random ran;

        public GraphicsDeviceManager graphicsDeviceManager;

		public Particle (float x, float y, int life, GraphicsDeviceManager gdm, int seed)
		{
            ran = new Random(seed);
            graphicsDeviceManager = gdm;
            this.xpos = x + ran.NextFloat(-0.002f, 0.002f);
            this.ypos = y + ran.NextFloat(-0.05f, 0.05f);
            lifespan = life;            
		}

		public void updatePos()
		{
            this.xpos = this.xpos + this.velocity.X;
            this.ypos = this.ypos + this.velocity.Y;
            this.velocity.X = this.velocity.X * 0.99999f + this.acceleration.X;
            this.velocity.Y = this.velocity.Y + this.acceleration.Y;            
		}

        public virtual void setColor()
		{
			//Set the colour of the particle
		}

        public virtual Color4 getColor()
        {
            color.Red = ran.NextFloat(0.2f, 1.0f);
            color.Green = ran.NextFloat(0.0f, 0.2f);
            color.Blue = ran.NextFloat(0.2f, 1.0f);
            color.Alpha = 1;

            return color;

        }

        public virtual void setShape()
		{
			//Set the shape of the particle
		}

        public virtual GeometricPrimitive getShape()
        {
            return this.shape;
        }

        public virtual Vector2 getVelocity()
        {
            this.velocity = this.velocity + this.acceleration;
            return this.velocity;
        }

        public void setVelocity()
        {
            this.velocity = this.velocity + this.acceleration;         
        }

        public virtual void setSizeX(int s)
        {
            this.sizeX = s;
        }

        public virtual void setSizeY(int s)
        {
            this.sizeY = s;
        }

        public virtual int getLife()
        {
            return this.lifespan;
        }

        public virtual float getX()
		{
            return this.xpos;
		}

        public virtual float getY()
		{
            return this.ypos;
		}

        public virtual void updateLife()
        {
            this.lifespan = this.lifespan - 1;
        }
	}
}

