using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using SharpDX.Direct2D1;

namespace ParticleLibrary
{
	public class ParticleType
	{
		public Vector2 acceleration;
		public Vector2 velocity;
        public Color4 color;        
        //public Vector3[] shape; 
        public GeometricPrimitive shape;
        
        public ParticleType ()
		{
           // velocity = new Vector2();
			velocity.X = 1;
			velocity.Y = -1;
           // acceleration = new Vector2Vector2();
			acceleration.X= 1;
			acceleration.Y = 1;
            color = Color.Fuchsia;          

		}

        public virtual Color4 getColor()
        {
            return color;
        }

        public virtual Vector2 getVelocity()
        {
            return velocity;
        }

        public virtual void setVelocity(Vector2 v)
        {
 
        }

        public virtual Vector2 getAcceleration()
        {
            return acceleration;
        }

        public virtual void setAcceleration(Vector2 v)
        {
 
        }
	}
}

