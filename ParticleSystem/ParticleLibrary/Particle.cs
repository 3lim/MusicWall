using System;
//using System.Linq;
//using System.Drawing;
//using System.Windows.Forms;
using System.Collections.Generic;

using SharpDX;
//using SharpDX.DXGI;
//using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace ParticleLibrary
{

	public class Particle : Game
	{
		private float xpos;
		private float ypos;
        private int lifespan;
		private Vector2 velocity;
		private Vector2 acceleration;
		private ParticleType ptype;
        private int sizeX;
        private int sizeY; 
        
        //from Elias:

        private GraphicsDeviceManager graphicsDeviceManager;


        private Matrix view;
        private Matrix projection;

        private BasicEffect basicEffect;
        private GeometricPrimitive primitive;
        private GeometricPrimitive primitive2;
        private GeometricPrimitive primitive3;
        private GeometricPrimitive tmp;

		public Particle (int x, int y, ParticleType p, int life)
		{
            graphicsDeviceManager = new GraphicsDeviceManager(this);
			this.xpos = x;
			this.ypos = y;
            lifespan = life;

			ptype = p;
            velocity = this.ptype.velocity;
			acceleration = this.ptype.acceleration;
		}
        /*
         * Initialize function for Game
         */
       
        protected override void Initialize()
        {
            Window.Title = "Particle";
            Window.IsMouseVisible = true;
            Window.AllowUserResizing = true;

            base.Initialize();            
        }

        /*
         * LoadContent function for Game
         */ 
        protected override void LoadContent()
        {
            var backDesc = GraphicsDevice.BackBuffer.Description;           
  
            basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));
            basicEffect.PreferPerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

            basicEffect.FogColor = new Vector3((Color.Azure.R), (Color.Azure.G), (Color.Azure.B));
            basicEffect.FogStart = 0.1f;
            basicEffect.FogEnd = 100.0f;
            basicEffect.FogEnabled = true;

            primitive = ToDisposeContent(GeometricPrimitive.Sphere.New(GraphicsDevice));
            primitive2 = ToDisposeContent(GeometricPrimitive.Sphere.New(GraphicsDevice));
            primitive3 = ToDisposeContent(GeometricPrimitive.Sphere.New(GraphicsDevice));

            base.LoadContent();
        }

        /*
         * 
         * 
         */

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            view = Matrix.LookAtLH(new Vector3(0.0f, 0.0f, -7.0f), new Vector3(0, 0.0f, 0), Vector3.UnitY);
            projection = Matrix.PerspectiveFovLH(0.9f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f);

            basicEffect.View = view;
            basicEffect.Projection = projection;
            if (lifespan < 2)
            {

            }
            else
            {
                this.updateLife();
                
            }
            this.updatePos();
        }


        /*
         * Draw
         */ 

        protected override void Draw(GameTime gameTime)
        {
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            //GraphicsDevice.SetRenderTargets(GraphicsDevice.DepthStencilBuffer, renderTargetOffScreen);
            GraphicsDevice.Clear(Color.White);

            Matrix viewProjInverse = (basicEffect.Projection * basicEffect.View);
            viewProjInverse.Invert();
            
            /*basicEffect.World = Matrix.Scaling(6.0f, 6.0f, 1.0f) *
                    Matrix.RotationX(0.0f) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(new Vector3(0.0f, 0.0f, 0.0f));            

            basicEffect.DiffuseColor = (Vector4)Color.SmoothStep(Color.Azure, Color.Orchid, 0.6f);
            primitive2.Draw(basicEffect);

            basicEffect.World = Matrix.Scaling(1.0f, 1.0f, 1.0f) *
                    Matrix.RotationX(0.0f) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(new Vector3(3.0f, 3.0f, 0.0f));
            basicEffect.DiffuseColor = (Vector4)Color.SmoothStep(Color.Green, Color.RoyalBlue, 0.7f);
            primitive.Draw(basicEffect);


            basicEffect.World = Matrix.Scaling(1.0f, 1.0f, 1.0f) *
                    Matrix.RotationX(0.0f) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(new Vector3(-3.0f, -3.0f, 0.0f));
            basicEffect.DiffuseColor = (Vector4)Color.SmoothStep(Color.Silver, Color.YellowGreen, 0.5f);
            primitive3.Draw(basicEffect);*/

            for (int i = 1; i < 20; i++)
            {
                tmp = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice));
                Color4 color;
                Random ran = new Random();
                /*color.Red = ran.NextFloat(0.0f, 1.0f);
                color.Green = ran.NextFloat(0.0f, 0.5f);
                color.Blue = ran.NextFloat(0.0f, 1.0f);
                color.Alpha = 1.0f;*/

                color = this.ptype.getColor();
               
                basicEffect.World = Matrix.Scaling(0.3f, 0.3f, 0.1f) *
                        Matrix.RotationX(0.0f) *
                        Matrix.RotationY(0) *
                        Matrix.RotationZ(0) *
                        Matrix.Translation(new Vector3(this.xpos, this.ypos + (0.5f * -i), 0));
                basicEffect.DiffuseColor = color;
                tmp.Draw(basicEffect);

            }


     
            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.Default);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            GraphicsDevice.SetDepthStencilState(GraphicsDevice.DepthStencilStates.None);
         
            // Render to screen
            GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
  
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);

            base.Draw(gameTime);            
        } 

		public void updatePos()
		{
            this.xpos = this.xpos + 0.01f;//+ this.ptype.getAcceleration().X * this.ptype.getVelocity().X;
            this.ypos = this.ypos - 0.02f;//+ this.ptype.getAcceleration().Y * this.ptype.getVelocity().Y;
            
		}

		public void setColor()
		{
			//Set the colour of the particle
		}

		public void setShape()
		{
			//Set the shape of the particle
		}

        public void setSizeX(int s)
        {
            this.sizeX = s;
        }

        public void setSizeY(int s)
        {
            this.sizeY = s;
        }

        public int getLife()
        {
            return this.lifespan;
        }

		public float getX()
		{
            return this.xpos;
		}

		public float getY()
		{
            return this.ypos;
		}

        public ParticleType getPType()
        {
            return this.ptype;
        }

		/*public void setSize (int s)
		{
			size = s; 
		}*/

        public void updateLife()
        {
            this.lifespan = this.lifespan - 1;
        }

        public void draw()
        {

            //Draw

            //Draw triangles, all shapes are made of them.
            
            updateLife();
            updatePos();

        }

	}
}

