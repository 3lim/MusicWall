using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using TestMySpline;
using KinectLibrary;
using System.Diagnostics;
using ParticleLibrary;


namespace MusicWall3D
{
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    public class MusicWall3D : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch spriteBatch;
        private SpriteFont arial16Font;

        private Matrix view;
        private Matrix projection;

        private Effect bloomEffect;
        private RenderTarget2D renderTargetOffScreen;
        private RenderTarget2D[] renderTargetDownScales;
        private RenderTarget2D renderTargetBlurTemp;

        private BasicEffect basicEffect;
        private GeometricPrimitive primitive;

        private KeyboardManager keyboard;
        private KeyboardState keyboardState;

        private MouseManager mouse;
        private MouseState mouseState;

        private List<List<Vector2>> objects;

        private List<CubicSpline> splines;

        private CubicSpline currentSpline = new CubicSpline();
        private List<Vector2> currentPoints = new List<Vector2>();
        private bool drawingStarted = false;
        private float lastEvent = 0.0f;

        private const float frequency = 0.05f;
        private const float pointFrequency = 0.005f;
        private const float minDistance = 0.002f;

        private Color[] colorList = new Color[] {Color.Green,Color.Goldenrod,Color.Red };

        private ParticleSystem pSystem;// a particle system

        private Kinect kinect = new Kinect();
        private const float kinectUpdateFrequency = 0.2f;
        private float lastKinectDataTime = 0.0f;

        private float leftCalibrationTime = 0.0f;
        private float rightCalibrationTime = 0.0f;

        private Vector3? leftUp = null;
        private Vector3? rightDown = null;

        private Vector3 position;

        //TODO
        private Stopwatch stopWatch;
        private TimeSpan lastUpdate;

        public MusicWall3D()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            keyboard = new KeyboardManager(this);
            mouse = new MouseManager(this);

            /*ADD PARTICLES, THIS SHOULD BE DONE WHEN WE WANT PARTICLES******************************/
            pSystem = new ParticleSystem(graphicsDeviceManager);
            for (int i = 0; i < 30; i++)
            {
                pSystem.addParticle(0, 0);
            }

            objects = new List<List<Vector2>>();
            splines = new List<CubicSpline>();
            //TODO
            stopWatch = new Stopwatch();
            stopWatch.Start();
            lastUpdate = stopWatch.Elapsed;

        }

        protected override void Initialize()
        {
            Window.Title = "MusicWall3D";
            Window.IsMouseVisible = true;
            Window.AllowUserResizing = true;

            kinect.Initialize();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = ToDisposeContent(new SpriteBatch(GraphicsDevice));

            arial16Font = Content.Load<SpriteFont>("Arial16");

            // Bloom Effect
            bloomEffect = Content.Load<Effect>("Bloom");

            // render targets for bloom effect
            renderTargetDownScales = new RenderTarget2D[5];
            var backDesc = GraphicsDevice.BackBuffer.Description;
            renderTargetOffScreen = ToDisposeContent(RenderTarget2D.New(GraphicsDevice, backDesc.Width, backDesc.Height, 1, backDesc.Format));
            for (int i = 0; i < renderTargetDownScales.Length; i++)
            {
                renderTargetDownScales[i] = ToDisposeContent(RenderTarget2D.New(GraphicsDevice, backDesc.Width >> i, backDesc.Height >> i, 1, backDesc.Format));
            }
            renderTargetBlurTemp = ToDisposeContent((RenderTarget2D)renderTargetDownScales[renderTargetDownScales.Length - 1].Clone());

            basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));
            basicEffect.PreferPerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

            basicEffect.FogColor = (Vector3)Color.SeaGreen;
            basicEffect.FogStart = 0.1f;
            basicEffect.FogEnd = 100.0f;
            basicEffect.FogEnabled = true;

            position = new Vector3();

            primitive = ToDisposeContent(GeometricPrimitive.Sphere.New(GraphicsDevice));

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            //TODO
            PlaySounds();

            view = Matrix.LookAtLH(new Vector3(0.0f, 0.0f, -7.0f), new Vector3(0, 0.0f, 0), Vector3.UnitY);
            projection = Matrix.PerspectiveFovLH(0.9f, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f);

            basicEffect.View = view;
            basicEffect.Projection = projection;

            keyboardState = keyboard.GetState();
            mouseState = mouse.GetState();

            position.X = (float)kinect.xYDepth.X * - 1.0f;
            position.Y = (float)kinect.xYDepth.Y;
            position.Z = (float)kinect.xYDepth.Z;

           

            if (gameTime.TotalGameTime.TotalSeconds - lastKinectDataTime >= kinectUpdateFrequency)
            {
                kinect.getData();
                lastKinectDataTime = (float)gameTime.TotalGameTime.TotalSeconds;
                if(leftUp!=null&&rightDown!=null) Console.WriteLine(normalizeVector2(new Vector2((float)kinect.xYDepth.X, (float)kinect.xYDepth.Y)));
            }

            if (leftCalibrationTime < 5.0f)
            {
                leftCalibrationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (rightCalibrationTime < 5.0f)
            {
                rightCalibrationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (leftCalibrationTime >= 5.0f && leftUp == null)
            {
                leftUp = new Vector3((float)kinect.xYDepth.X, (float)kinect.xYDepth.Y, (float)kinect.xYDepth.Z);
                Console.WriteLine(leftUp);
            }

            if (rightCalibrationTime >= 5.0f && rightDown == null)
            {
                rightDown = new Vector3((float)kinect.xYDepth.X, (float)kinect.xYDepth.Y, (float)kinect.xYDepth.Z);
                Console.WriteLine(rightDown);
            }

            if (mouseState.Right == ButtonState.Pressed)
            {
                objects.Clear();
                splines.Clear();
            }

            if (position.Z < 50)
            {
                if (gameTime.TotalGameTime.TotalSeconds - lastEvent >= frequency)
                {
                    if (!drawingStarted)
                    {
                        drawingStarted = true;
                    }

                    lastEvent = (float)gameTime.TotalGameTime.TotalSeconds;

                    Vector2 normalizedPos = normalizeVector2((Vector2)position);

                    currentPoints.RemoveAll((Vector2 a) => Math.Abs(normalizedPos.X - a.X) < minDistance);

                    currentPoints.Add(normalizedPos);

                    currentPoints.Sort((Comparison<Vector2>)delegate(Vector2 a, Vector2 b) { return a.X.CompareTo(b.X); });

                    float[] xs = new float[currentPoints.Count];
                    float[] ys = (float[])xs.Clone();

                    for (int i = 0; i < currentPoints.Count; i++)
                    {
                        xs[i] = currentPoints[i].X;
                        ys[i] = currentPoints[i].Y;
                    }

                    if(currentPoints.Count > 1) currentSpline.Fit(xs, ys);
                }
            }
            else if(drawingStarted)
            {
                drawingStarted = false;

                objects.Add(currentPoints);
                splines.Add(currentSpline);

                currentPoints = new List<Vector2>();
                currentSpline = new CubicSpline();

            }

            /**PARTICLE UPDATE********************************************************************/
            List<Particle> remove = new List<Particle>();
            foreach (Particle p in pSystem.getList())
            {

                if (p.getLife() > 0)
                {
                    p.updatePos();
                    p.updateLife();
                }
                else
                {
                    remove.Add(p);
                }
            }
            foreach (Particle p in remove)
            {
                pSystem.removeParticle(p);
                pSystem.addParticle(0, 0);
            }
            remove.Clear();

            /**END PARTICLE UPDATE********************************************************************/
        }

        //TODO
        private void PlaySounds()
        {
            TimeSpan tmp = stopWatch.Elapsed;
            if (lastUpdate.Seconds != tmp.Seconds)
            {
                Sound sound = new Sound();
                lastUpdate = tmp;
                float last, first;
                foreach (List<Vector2> list in objects)
                {
                    first = list[0][0] - 0.05f;
                    last = (list.Last()[0]) + 0.05f;
                    if ((first * 10) < tmp.Seconds % 10 && (int)(last * 10) >= tmp.Seconds % 10)
                        sound.Play(list[0][1]);
                    Debug.WriteLine(first * 10 + "<" + tmp.Seconds % 10 + "=" + (first * 10 < tmp.Seconds % 10));
                    Debug.WriteLine(last * 10 + ">" + tmp.Seconds % 10 + "=" + (last * 10 > tmp.Seconds % 10));
                    Debug.WriteLine(0.5f);

                }
            }
        }


        protected override void Draw(GameTime gameTime)
        {
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // offline rendering
            GraphicsDevice.SetRenderTargets(GraphicsDevice.DepthStencilBuffer, renderTargetOffScreen);
            GraphicsDevice.Clear(Color.BlanchedAlmond);

            float aspectRatio = (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height;

            Matrix viewProjInverse = (basicEffect.Projection * basicEffect.View);
            viewProjInverse.Invert();

            /**PARTICLE DRAW********************************************************************/
            foreach (Particle p in pSystem.getList())
            {
                GeometricPrimitive g = ToDisposeContent(GeometricPrimitive.Sphere.New(GraphicsDevice));//p.getShape();

                basicEffect.World = Matrix.Scaling(0.5f, 0.5f, 0.5f) *
                    Matrix.RotationX(deg2rad(90.0f)) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(new Vector3(p.getX(), p.getY() + (0.05f), 0));

                Color4 color = p.getColor();
                basicEffect.DiffuseColor = color;
                g.Draw(basicEffect);
            }

            /**END PARTICLE DRAW********************************************************************/


            Vector2 normalizedPos = normalizeVector2((Vector2)position);


                for (int i = 0; i < splines.Count; i++)
                {
                    List<float> xs = new List<float>();
                    List<float> ys = new List<float>();

                    for (float x = objects[i][0].X; x <= objects[i][objects[i].Count - 1].X; x += pointFrequency)
                    {
                        xs.Add(x);
                    }

                    if (xs.Count > 1)
                    {
                        ys.AddRange(splines[i].Eval(xs.ToArray()));
                    }
                    else
                    {
                        ys.Add(objects[i][0].Y);
                    }

                    for (int j = 0; j < xs.Count; j++)
                    {
                        basicEffect.World = Matrix.Scaling(2.0f, 0.5f, 2.0f) *
                                    Matrix.RotationX(deg2rad(90.0f)) *
                                    Matrix.RotationY(0) *
                                    Matrix.RotationZ(0) *
                                    Matrix.Translation(screenToWorld(new Vector3(xs[j], ys[j], 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));
                        basicEffect.DiffuseColor = (Vector4)pickColor(ys[j]);
                        primitive.Draw(basicEffect);
                    }
                }

                for (int i = 0; i < currentPoints.Count; i++)
                {
                    basicEffect.World = Matrix.Scaling(2.0f, 0.5f, 2.0f) *
                                Matrix.RotationX(deg2rad(90.0f)) *
                                Matrix.RotationY(0) *
                                Matrix.RotationZ(0) *
                                Matrix.Translation(screenToWorld(new Vector3(currentPoints[i], 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));
                    basicEffect.DiffuseColor = (Vector4)pickColor(currentPoints[i].Y);
                    primitive.Draw(basicEffect);
                }

                if (currentPoints.Count > 0)
                {
                    List<float> xs = new List<float>();
                    List<float> ys = new List<float>();

                    for (float x = currentPoints[0].X; x <= currentPoints[currentPoints.Count - 1].X; x += pointFrequency)
                    {
                        xs.Add(x);
                    }

                    if (xs.Count > 1)
                    {
                        ys.AddRange(currentSpline.Eval(xs.ToArray()));
                    }
                    else
                    {
                        ys.Add(currentPoints[0].Y);
                    }

                    for (int j = 0; j < xs.Count; j++)
                    {
                        basicEffect.World = Matrix.Scaling(2.0f, 0.5f, 2.0f) *
                                    Matrix.RotationX(deg2rad(90.0f)) *
                                    Matrix.RotationY(0) *
                                    Matrix.RotationZ(0) *
                                    Matrix.Translation(screenToWorld(new Vector3(xs[j], ys[j], 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));
                        basicEffect.DiffuseColor = (Vector4)pickColor(ys[j]);
                        primitive.Draw(basicEffect);
                    }
                }

            basicEffect.World = Matrix.Scaling(2.0f, 0.5f, 2.0f) *
                                Matrix.RotationX(deg2rad(90.0f)) *
                                Matrix.RotationY(0) *
                                Matrix.RotationZ(0) *
                                Matrix.Translation(screenToWorld(new Vector3(normalizedPos,5.0f),basicEffect.View,basicEffect.Projection,Matrix.Identity,GraphicsDevice.Viewport));
            basicEffect.DiffuseColor = (Vector4)pickColor(normalizedPos.Y);
            primitive.Draw(basicEffect);

            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.Default);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            GraphicsDevice.SetDepthStencilState(GraphicsDevice.DepthStencilStates.None);

            const float brightPassThreshold = 0.5f;
            GraphicsDevice.SetRenderTargets(renderTargetDownScales[0]);
            bloomEffect.CurrentTechnique = bloomEffect.Techniques["BrightPassTechnique"];
            bloomEffect.Parameters["Texture"].SetResource(renderTargetOffScreen);
            bloomEffect.Parameters["PointSampler"].SetResource(GraphicsDevice.SamplerStates.PointClamp);
            bloomEffect.Parameters["BrightPassThreshold"].SetValue(brightPassThreshold);
            GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[0]);

            // Down scale passes
            for (int i = 1; i < renderTargetDownScales.Length; i++)
            {
                GraphicsDevice.SetRenderTargets(renderTargetDownScales[i]);
                GraphicsDevice.DrawQuad(renderTargetDownScales[0]);
            }

            // Horizontal blur pass
            var renderTargetBlur = renderTargetDownScales[renderTargetDownScales.Length - 1];
            GraphicsDevice.SetRenderTargets(renderTargetBlurTemp);
            bloomEffect.CurrentTechnique = bloomEffect.Techniques["BlurPassTechnique"];
            bloomEffect.Parameters["Texture"].SetResource(renderTargetBlur);
            bloomEffect.Parameters["LinearSampler"].SetResource(GraphicsDevice.SamplerStates.LinearClamp);
            bloomEffect.Parameters["TextureTexelSize"].SetValue(new Vector2(1.0f / renderTargetBlurTemp.Width, 1.0f / renderTargetBlurTemp.Height));
            GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[0]);

            // Vertical blur pass
            GraphicsDevice.SetRenderTargets(renderTargetBlur);
            bloomEffect.Parameters["Texture"].SetResource(renderTargetBlurTemp);
            GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[1]);

            // Render to screen
            GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
            GraphicsDevice.DrawQuad(renderTargetOffScreen);

            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);
            GraphicsDevice.DrawQuad(renderTargetBlur);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);

            //Console.WriteLine(kinect.xYDepth);

            base.Draw(gameTime);
        }

        private float deg2rad(float angle)
        {
            return angle / 180.0f * (float)Math.PI;
        }

        private Color pickColor(float ratio)
        {
            if (colorList == null) return Color.Black;

            if (float.IsNaN(ratio)) return colorList[0];

            if (ratio >= 1.0f) return colorList[colorList.Count() - 1];

            if (ratio < 0.0f) return colorList[0];

            float scaled = ratio * (colorList.Count()-1);
            int firstIndex = (int)Math.Floor(scaled);
            int secondIndex = (int)Math.Ceiling(scaled);
            float mix = scaled - firstIndex;

            return Color.SmoothStep(colorList[firstIndex], colorList[secondIndex], mix);
        }

        private Vector3 screenToWorld(Vector3 coord, Matrix view, Matrix proj, Matrix world, ViewportF screen)
        {
            Vector3 near = screen.Unproject(new Vector3(coord.X * screen.Width, coord.Y * screen.Height, 0.0f), proj, view, world);
            Vector3 far = screen.Unproject(new Vector3(coord.X * screen.Width, coord.Y * screen.Height, 1.0f), proj, view, world);
            
            return near + (far-near)*0.1f;
        }

        private Vector2 normalizeVector2(Vector2 vec)
        {
            if (leftUp == null || rightDown == null) return vec;
            return Vector2.UnitX - (vec - (Vector2)leftUp) / ((Vector2)rightDown - (Vector2)leftUp);
        }
    }
}
