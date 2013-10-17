using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using TestMySpline;
//using KinectLibrary;
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

        private List<Spline> splines;

        private Spline currentSpline;
        private List<Vector2> currentPoints = new List<Vector2>();
        private bool drawingStarted = false;
        private float lastEvent = 0.0f;

        private const float frequency = 0.005f;
        private const float pointFrequency = 0.005f;
        private const float minDistance = 0.01f;
        private const float particleFrequency = 0.0005f;

        private Color[] colorList = new Color[] {Color.MediumPurple, Color.Purple, Color.Black};

        private ParticleSystem pSystem;// a particle system
        private GeometricPrimitive g;
        private List<Vector2> remove;

        private GeometricPrimitive timeLine;
        private List<GeometricPrimitive> guideLines;

        //private Kinect kinect = new Kinect();
        private const float kinectUpdateFrequency = 0.5f;
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
            remove = new List<Vector2>();
            objects = new List<List<Vector2>>();
            splines = new List<Spline>();
            //TODO
            stopWatch = new Stopwatch();
            stopWatch.Start();
            lastUpdate = stopWatch.Elapsed;

        }

        protected override void Initialize()
        {
            Window.Title = "ComposIt";
            Window.IsMouseVisible = true;
            Window.AllowUserResizing = true;

            //kinect.Initialize();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = ToDisposeContent(new SpriteBatch(GraphicsDevice));      

            arial16Font = Content.Load<SpriteFont>("Arial16");
            g = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice));//p.getShape();

            timeLine = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice));
            guideLines = new List<GeometricPrimitive>();
            for (int i = 0; i < 9; i++)
            {
                guideLines.Add(ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice)));
            }

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

            primitive = ToDisposeContent(GeometricPrimitive.Cylinder.New(GraphicsDevice));

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

            position = new Vector3(mouseState.X, mouseState.Y, 0.0f);

            // KINECT

            /*position.X = (float)kinect.xYDepth.X * - 1.0f;
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
            }*/

            // KINECT END

            if (mouseState.Right == ButtonState.Pressed)
            {
                remove.Clear();
                objects.Clear();
                splines.Clear();

                currentPoints = new List<Vector2>();
            }

            if (mouseState.Left == ButtonState.Pressed)//position.Z < 50)
            {
                if (gameTime.TotalGameTime.TotalSeconds - lastEvent >= frequency)
                {

                    lastEvent = (float)gameTime.TotalGameTime.TotalSeconds;

                    Vector2 normalizedPos = normalizeVector2((Vector2)position);

                    if (!drawingStarted)
                    {

                        drawingStarted = true;
                        currentSpline = new Spline(normalizedPos.X, normalizedPos.Y);
                        currentPoints = new List<Vector2>();
                        currentPoints.Add(new Vector2(normalizedPos.X, normalizedPos.Y));
                    }

                    //if (currentPoints.Any((Vector2 a) => Math.Abs(normalizedPos.X - a.X) < minDistance && Math.Abs(normalizedPos.Y - a.Y) > minDistance))
                    //{
                    //    objects.Add(currentPoints);
                    //    splines.Add(currentSpline);

                    //    currentPoints = new List<Vector2>();
                    //    currentSpline = new CubicSpline();
                    //}

                    if (!currentPoints.Any((Vector2 a) => (normalizedPos - a).Length() < minDistance))
                    {
                        currentPoints.Add(normalizedPos);
                        currentSpline.addPoint(normalizedPos.X, normalizedPos.Y);
                        currentPoints = currentSpline.sample(pointFrequency);
                    }

                    //currentPoints.Sort((Comparison<Vector2>)delegate(Vector2 a, Vector2 b) { return a.X.CompareTo(b.X); });

                    //float[] xs = new float[currentPoints.Count];
                    //float[] ys = (float[])xs.Clone();

                    //for (int i = 0; i < currentPoints.Count; i++)
                    //{
                    //    xs[i] = currentPoints[i].X;
                    //    ys[i] = currentPoints[i].Y;
                    //}

                    //if(currentPoints.Count > 1) currentSpline.Fit(xs, ys);
                }
            }
            else if(drawingStarted)
            {
                drawingStarted = false;
                if (currentSpline != null)
                {
                    objects.Add(currentPoints);
                    splines.Add(currentSpline);
                }
            }
            if (stopWatch.Elapsed.Seconds % 10 == 0)
            {
                remove.Clear();
            }
            /**PARTICLE UPDATE********************************************************************/

            List<Particle> removeP = new List<Particle>();
            foreach (Particle p in pSystem.getList())
            {

                if (p.getLife() > 0)
                {
                    p.updatePos();
                    p.updateLife();
                }
                else
                {
                    removeP.Add(p);
                }
            }
            foreach (Particle p in removeP)
            {
                pSystem.removeParticle(p);                
            }
            removeP.Clear();

            /**END PARTICLE UPDATE********************************************************************/


            

        }

        //TODO
        private void PlaySounds()
        {
            TimeSpan tmp = stopWatch.Elapsed;

            foreach (List<Vector2> list in objects)
            {

                //**********PARTICLE TEST***//
                foreach (Vector2 l in list)
                {
                    float xTL = (float)((tmp.TotalMilliseconds % 10000) / (float)(10000));
                    if (Math.Abs(l.X - xTL)<= particleFrequency + 0.0005)//(l.X * 10 < tmp.Seconds % 10 && (int)(list.Last()[0] * 10) >= tmp.Seconds % 10)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            pSystem.addParticle(l.X, l.Y);                            
                        }
                        remove.Add(l);
                    }                    
                }
            }
            if (lastUpdate.Seconds != tmp.Seconds)
            {
                Sound sound = new Sound();
                lastUpdate = tmp;
                float last, first;
                foreach (List<Vector2> list in objects)
                {
                    if (list.Count == 0) continue;

                    first = list[0][0] - 0.05f;
                    last = (list.Last()[0]) + 0.05f;
                    if ((first * 10) < tmp.Seconds % 10 && (int)(last * 10) >= tmp.Seconds % 10)
                    {
                        sound.Play(list[0].Y);                      
                        //Debug.WriteLine("X:" + (list[0].X) + " Y:" + (list[0].Y));
                    }
                    //Debug.WriteLine(first * 10 + "<" + tmp.Seconds % 10 + "=" + (first * 10 < tmp.Seconds % 10));
                    //Debug.WriteLine(last * 10 + ">" + tmp.Seconds % 10 + "=" + (last * 10 > tmp.Seconds % 10));
                    //Debug.WriteLine(0.5f);

                }
            }
        }

        private void addParticles()
        {
            TimeSpan tmp = stopWatch.Elapsed;
            if (lastUpdate.Seconds != tmp.Seconds)
            {                
                lastUpdate = tmp;
                float last, first;
                foreach (List<Vector2> list in objects)
                {
                    first = list[0][0] - 0.05f;
                    last = (list.Last()[0]) + 0.05f;
                    if ((first * 10) < tmp.Seconds % 10 && (int)(last * 10) >= tmp.Seconds % 10)
                    {
                        float x = list[0].X;
                        float y = list[0].Y;

                        //pSystem.addParticle(x, y);
                    }
                    //Debug.WriteLine(first * 10 + "<" + tmp.Seconds % 10 + "=" + (first * 10 < tmp.Seconds % 10));
                    //Debug.WriteLine(last * 10 + ">" + tmp.Seconds % 10 + "=" + (last * 10 > tmp.Seconds % 10));
                    //Debug.WriteLine(0.5f);
                }
            }
        }


        protected override void Draw(GameTime gameTime)
        {
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            

            // offline rendering
            GraphicsDevice.SetRenderTargets(GraphicsDevice.DepthStencilBuffer, renderTargetOffScreen);

            GraphicsDevice.Clear(new Color4(0.316f, 0.451f, 0.473f, 1.0f));//Background color 


            float aspectRatio = (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height;
            
            Vector2 normalizedPos = normalizeVector2((Vector2)position);



                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].Count == 0) continue;


                    foreach (Vector2 p in objects[i])
                    {
                        if (remove.Contains(p))
                        {
                            //drawPoint(new Vector3(p, 5.0f), new Color4(255, 255, 255, 255));
                        }
                        else
                        {
                            drawPoint(new Vector3(p, 5.0f), (Vector4)pickColor((float)p.Y));
                        }
                    }

                    //List<float> xs = new List<float>();
                    //List<float> ys = new List<float>();

                    //for (float x = objects[i][0].X; x <= objects[i][objects[i].Count - 1].X; x += pointFrequency)
                    //{
                    //    xs.Add(x);
                    //}

                    //if (xs.Count > 1)
                    //{
                    //    ys.AddRange(splines[i].Eval(xs.ToArray()));
                    //}
                    //else
                    //{
                    //    ys.Add(objects[i][0].Y);
                    //}

                    //for (int j = 0; j < xs.Count; j++)
                    //{
                    //    drawPoint(new Vector3(xs[j], ys[j], 5.0f), (Vector4)pickColor(ys[j]));
                    //}
                }

                for (int i = 0; i < currentPoints.Count; i++)
                {
                    if (remove.Contains(currentPoints[i]))
                    {
                        //drawPoint(new Vector3(currentPoints[i], 5.0f), new Color4(255, 255, 255, 255));
                    }
                    else
                    {
                        drawPoint(new Vector3(currentPoints[i], 5.0f), (Vector4)pickColor(currentPoints[i].Y));
                    }
                }

                //if (currentPoints.Count > 0)
                //{
                //    List<Vector2> points = currentSpline.sample(pointFrequency);

                //    foreach (Vector2 p in points)
                //    {
                //        drawPoint(new Vector3(p, 5.0f), (Vector4)pickColor((float)p.Y));
                //    }

                //    //List<float> xs = new List<float>();
                //    //List<float> ys = new List<float>();

                //    //for (float x = currentPoints[0].X; x <= currentPoints[currentPoints.Count - 1].X; x += pointFrequency)
                //    //{
                //    //    xs.Add(x);
                //    //}

                //    //if (xs.Count > 1)
                //    //{
                //    //    ys.AddRange(currentSpline.Eval(xs.ToArray()));
                //    //}
                //    //else
                //    //{
                //    //    ys.Add(currentPoints[0].Y);
                //    //}

                //    //for (int j = 0; j < xs.Count; j++)
                //    //{
                //    //    drawPoint(new Vector3(xs[j], ys[j], 5.0f), (Vector4)pickColor(ys[j]));
                //    //}
                //}

                if (remove.Contains(normalizedPos))
                {
                   // drawPoint(new Vector3(normalizedPos, 5.0f), new Color4(255, 255, 255, 255));
                }
                else
                {
                    drawPoint(new Vector3(normalizedPos, 5.0f), (Vector4)pickColor(normalizedPos.Y));
                }
            
            /**PARTICLE DRAW********************************************************************/
            foreach (Particle p in pSystem.getList())
            {
                basicEffect.World = Matrix.Scaling(0.1f, 0.1f, 0.1f) *
                    Matrix.RotationX(p.getRotationX()) *
                    Matrix.RotationY(p.getRotationY()) *
                    Matrix.RotationZ(p.getRotationZ()) *
                    Matrix.Translation(screenToWorld( new Vector3(p.getX(), p.getY(), 5.0f),basicEffect.View,basicEffect.Projection,Matrix.Identity,GraphicsDevice.Viewport));

                Color4 color = p.getColor();
                basicEffect.DiffuseColor = color;
                g.Draw(basicEffect);
            }

            /**END PARTICLE DRAW********************************************************************/

            // ----- TIME LINE --------------------
            TimeSpan tmp = stopWatch.Elapsed;
            float xTL = (float)((tmp.TotalMilliseconds % 10000) / (float)(10000));
            basicEffect.World = Matrix.Scaling(0.1f, 0.1f, GraphicsDevice.BackBuffer.Height) *
                Matrix.RotationX(deg2rad(90.0f)) *
                Matrix.RotationY(0) *
                Matrix.RotationZ(0) *
                Matrix.Translation(screenToWorld(new Vector3(xTL, 0.0f, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));            
            basicEffect.DiffuseColor = new Color4(0.466667f, 0.533333f, 0.6f, 1.0f);//new Color4(0.2f, 0.9f, 0.2f, 0.2f);
            timeLine.Draw(basicEffect);

            // ------- END TIME LINE -------------

            // ----- GUIDE LINES ------------------
            int screenWidth = GraphicsDevice.BackBuffer.Width;
            float glY = 0.0f;
            foreach(GeometricPrimitive gl in guideLines)
            {
                glY += 0.1f;
                basicEffect.World = Matrix.Scaling(screenWidth, 0.1f, 0.05f) *
                    Matrix.RotationX(deg2rad(90.0f)) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(screenToWorld(new Vector3(0.0f, glY, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));
                
                basicEffect.DiffuseColor = new Color4(0.2f, 0.2f, 0.2f, 0.2f);
                gl.Draw(basicEffect);
            }

            // ----- END GUIDE LINES --------------


            // --- Trying to add anti-aliasing
            RasterizerState rState = GraphicsDevice.RasterizerStates.Default;
            SharpDX.Direct3D11.RasterizerStateDescription rStateDesc = rState.Description;
            rStateDesc.IsMultisampleEnabled = true;
            rStateDesc.IsAntialiasedLineEnabled = true;
            RasterizerState newRState = RasterizerState.New(GraphicsDevice, rStateDesc);
            GraphicsDevice.SetRasterizerState(newRState);
            // --- End anti-aliasing

            //GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.Default);
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

        private void drawPoint(Vector3 pos, Vector4 col)
        {
            basicEffect.World = Matrix.Scaling(0.4f, 0.05f, 0.4f) *
                                    Matrix.RotationX(deg2rad(90.0f)) *
                                    Matrix.RotationY(0) *
                                    Matrix.RotationZ(0) *
                                    Matrix.Translation(screenToWorld(pos, basicEffect.View, basicEffect.Projection, Matrix.Identity, GraphicsDevice.Viewport));
            basicEffect.DiffuseColor = col;
            primitive.Draw(basicEffect);
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
            return vec;
            if (leftUp == null || rightDown == null) return vec;
            return Vector2.UnitX - (vec - (Vector2)leftUp) / ((Vector2)rightDown - (Vector2)leftUp);
        }
    }
}
