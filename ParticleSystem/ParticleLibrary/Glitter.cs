using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using SharpDX.Direct2D1;


namespace ParticleLibrary
{
	public class Glitter: ParticleType
	{
		public Glitter ()
		{
			velocity.X = 0.1f;
			velocity.Y = -0.2f;
			acceleration.X = 1.0f;
			acceleration.Y = 1.0f;
            
            //glitter should have randomized colours
            Random ran = new Random();
            color.Red = ran.NextFloat(0.2f, 0.4f);
            color.Green = ran.NextFloat(0.0f, 0.2f);
            color.Blue = ran.NextFloat(0.5f, 1.0f);
            color.Alpha = 1.0f;
            
            

            //glitter is rectangular, 4 corners
            
            /*shape = new Vector3[4];
            

            shape[0] = new Vector3(5.0f, 5.0f, 0.0f);
            shape[1] = new Vector3(5.0f, 5.0f, 0.0f);
            shape[2] = new Vector3(5.0f, 5.0f, 0.0f);
            shape[3] = new Vector3(5.0f, 5.0f, 0.0f);*/


            //2 Triangles


		}

        public override Color4 getColor()
        {
            Random ran = new Random();
            color.Red = ran.NextFloat(0.2f, 1.0f);
            color.Green = ran.NextFloat(0.0f, 0.2f);
            color.Blue = ran.NextFloat(0.2f, 1.0f);
            color.Alpha = 1;

            return color;

        }

        public override Vector2 getVelocity()
        {
            /*Random ran = new Random();
            this.velocity.X = this.velocity.X + this.velocity.X;
            this.velocity.Y = this.velocity.Y + this.velocity.Y;*/
            return this.velocity;
        }

        public override void setVelocity(Vector2 v)
        {

        }

        public override Vector2 getAcceleration()
        {
            return this.acceleration;
        }

        public override void setAcceleration(Vector2 v)
        {

        }
	}
}

