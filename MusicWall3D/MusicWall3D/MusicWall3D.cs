using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TestMySpline;
//using KinectLibrary;
using ParticleLibrary;
using MusicWall3D.Properties;

using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;


namespace MusicWall3D
{
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    public class MusicWall3D : Component
    {
        public struct mState {
            public float X;
            public float Y;
            public bool left;
            public bool right;
            public bool middle;
        };

        private Matrix view;
        private Matrix projection;

        private Effect bloomEffect;
        private RenderTarget2D renderTargetOffScreen;
        private RenderTarget2D[] renderTargetDownScales;
        private RenderTarget2D renderTargetBlurTemp;

        private BasicEffect basicEffect;
        private GeometricPrimitive primitive;

        private KeyboardState keyboardState;

        public mState mouseState;

        private List<DrawObject> objects;
        private List<int> objectColor;

        private List<Spline> splines;

        private Spline currentSpline;
        private List<Vector2> currentPoints = new List<Vector2>();
        private bool drawingStarted = false;
        private bool undoStarted = false;
        private float lastEvent = 0.0f;
        private const float frequency = 0.005f;
        private const float pointFrequency = 0.003f;
        private const float minDistance = 0.015f;
        private const float particleFrequency = 0.0001f;

        private Color[] colorList = new Color[] {Color.MediumPurple, Color.Purple, Color.Black};

        private ParticleSystem pSystem;// a particle system
        private GeometricPrimitive g;
        private GeometricPrimitive fireWork;
        private GeometricPrimitive paletteCube;

        private InstancedGeometricPrimitive.Primitive cylinder;
        private InstancedGeometricPrimitive instancedGeo;        
        private Color[] green = new Color[] {new Color(0.165f, 1.8f, 0.502f)};//, Color.GhostWhite, Color.FloralWhite };//new Color4(1.0f, 1.0f, 1.0f, 1.0f); //1
        private Color[] pink = new Color[] { new Color(1.0f, 0.0f, 1.0f) };//, Color.Blue, Color.Navy};//new Color4(0.165f, 0.875f, 0.902f, 1.0f);//2
        private Color[] lilac = new Color[] {new Color(0.4f, 0.0f, 0.6f)};//, Color.HotPink, Color.HotPink};//new Color4(0.914f, 0.392f, 0.475f, 1.0f);//3
        private Color[] purple = new Color[] {new Color(0.1f, 0.0f, 3.0f)};//, Color.Purple, Color.Black};//new Color4(0.49f, 0.392f, 0.851f, 1.0f); //0
        private Color[][] palette = new Color[4][];

        private Color4[] palette2 = new Color4[4];

        private int paletteColor = 3; //which color to draw with

        // GRADIENT BACKGROUND
        private GeometricPrimitive backgroundElt;
         // bottom color: RGB (/100) = (32, 30, 38)
         // top color: RGB (/100) = (62, 85, 84)
        private float[] bckgdTopColor = new float[] { 0.62f, 0.85f, 0.84f };
        private float[] gradientBckgdColor = new float[] { 0.30f, 0.55f, 0.46f };

        private GeometricPrimitive timeLine;
        private GeometricPrimitive guideLine;

        //private Kinect kinect = new Kinect();
        private const float kinectUpdateFrequency = 0.5f;
        private float lastKinectDataTime = 0.0f;

        private float leftCalibrationTime = 0.0f;
        private float rightCalibrationTime = 0.0f;

        private Vector3? leftUp = null;
        private Vector3? rightDown = null;

        private Vector3 position;

        private Vector2 lastPosition;
        private float lastRemoveEvent = 0.0f;

        private const int NumberOfThreads = 16;

        private Device device;
        private GraphicsDevice graphicsDevice;
        private SwapChain swapChain;
        private SharpDX.Direct3D11.Texture2D backBuffer;
        private RenderTargetView renderView;

        private RenderForm form;

        //TODO
        private Stopwatch stopWatch;
        private TimeSpan lastUpdate;
        private Sound sound;

        struct DrawObject
        {
            public Vector2 Position;
            public int InstanceId;
            public int ObjectColor;
            public int SplineId;
        }

        public MusicWall3D()
        {

            /*ADD PARTICLES, THIS SHOULD BE DONE WHEN WE WANT PARTICLES******************************/
            pSystem = new ParticleSystem();
            objects = new List<DrawObject>();
            objectColor = new List<int>();
            splines = new List<Spline>();
            //TODO
            stopWatch = new Stopwatch();
            stopWatch.Start();
            lastUpdate = stopWatch.Elapsed;


        }

        public void Run()
        {
            form = new RenderForm("ComposIt");
            form.ClientSize = new Size(1920, 1080);
            //TODO
            sound = new Sound(form.Handle);

            form.KeyDown += (target, arg) =>
            {

            };

            form.MouseMove += (target, arg) =>
            {
                mouseState.X = arg.X / (float)form.ClientSize.Width;
                mouseState.Y = arg.Y / (float)form.ClientSize.Height;
            };

            form.MouseDown += (target, arg) =>
            {
                mouseState.left |= arg.Button == MouseButtons.Left;
                mouseState.right |= arg.Button == MouseButtons.Right;
                mouseState.middle |= arg.Button == MouseButtons.Middle;
            }; 
            
            form.MouseUp += (target, arg) =>
            {
                mouseState.left ^= arg.Button == MouseButtons.Left;
                mouseState.right ^= arg.Button == MouseButtons.Right;
                mouseState.middle ^= arg.Button == MouseButtons.Middle;
            };

            var desc = new SwapChainDescription()
            {
                BufferCount = 2,
                ModeDescription =
                    new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(8, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            //Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            //int quality = device.CheckMultisampleQualityLevels(Format.R8G8B8A8_UNorm, 8);
            //Console.WriteLine(quality);
            //desc.SampleDescription.Quality = quality > 0 ? quality - 1 : 0;
            //device.Dispose();
            //swapChain.Dispose();

            //Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            //device = ToDispose(device);

            graphicsDevice = ToDispose(GraphicsDevice.New(DriverType.Hardware));
            Cursor.Hide();//Hides cursor

            PresentationParameters pp = new PresentationParameters(desc.ModeDescription.Width,desc.ModeDescription.Height,desc.OutputHandle,desc.ModeDescription.Format);
            pp.MultiSampleCount = MSAALevel.X8;
            pp.IsFullScreen = true;
            
            graphicsDevice.Presenter = new SwapChainGraphicsPresenter(graphicsDevice,pp);

            //Factory factory = ToDispose(swapChain.GetParent<Factory>());
            //factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            //backBuffer = ToDispose(SharpDX.Direct3D11.Texture2D.FromSwapChain<SharpDX.Direct3D11.Texture2D>(swapChain, 0));
            //renderView = ToDispose(new RenderTargetView(device, backBuffer));

            LoadContent();

            Stopwatch totalTime = new Stopwatch();
            totalTime.Start();
            Stopwatch elapsedTime = new Stopwatch();
            RenderLoop.Run(form, () =>
            {
                Update(new GameTime(totalTime.Elapsed, elapsedTime.Elapsed));
                TimeSpan old = elapsedTime.Elapsed;
                elapsedTime.Restart();
                Draw(new GameTime(totalTime.Elapsed, old));
                graphicsDevice.Present();
            });

            Dispose();
            //kinect.Initialize();
        }

        protected void LoadContent()
        {
            //bloomEffect = ToDispose(new Effect(graphicsDevice, ShaderBytecode.Compile(Resources.Bloom, "fx_4_0").Bytecode));
            g = ToDispose(GeometricPrimitive.Cube.New(graphicsDevice));//p.getShape();
            fireWork = ToDispose(GeometricPrimitive.Sphere.New(graphicsDevice));
            paletteCube = ToDispose(GeometricPrimitive.Cube.New(graphicsDevice));

            cylinder = InstancedGeometricPrimitive.CreateCylinder(graphicsDevice);
            cylinder.IndexBuffer = ToDispose(cylinder.IndexBuffer);
            cylinder.VertexBuffer = ToDispose(cylinder.VertexBuffer);

            instancedGeo = ToDispose(new InstancedGeometricPrimitive(graphicsDevice));
            
            timeLine = ToDispose(GeometricPrimitive.Cube.New(graphicsDevice));
            guideLine = ToDispose(GeometricPrimitive.Cube.New(graphicsDevice));
            backgroundElt = ToDispose(GeometricPrimitive.Cube.New(graphicsDevice));

            //Palette colours
            palette[0] = green;
            palette[1] = pink;
            palette[2] = lilac;
            palette[3] = purple;

            palette2[0] = new Color4(0.165f, 1.8f, 0.502f, 1.0f);
            palette2[1] = new Color4(1.0f, 0.0f, 1.0f, 1.0f);
            palette2[2] = new Color4(0.4f, 0.0f, 0.6f, 1.0f);
            palette2[3] = new Color4(0.1f, 0.0f, 3.0f, 1.0f);
            


            //// Bloom Effect
            //    //BackBuffer = ToDisposeContent(RenderTarget2D.New(GraphicsDevice,1280,720,MSAALevel.X8,GraphicsDevice.Presenter.BackBuffer.Description.Format));
            //// render targets for bloom effect
            //renderTargetDownScales = new RenderTarget2D[5];
            //var backDesc = GraphicsDevice.BackBuffer.Description;
            //renderTargetOffScreen = ToDisposeContent(RenderTarget2D.New(GraphicsDevice, backDesc.Width, backDesc.Height, 1, backDesc.Format));
            ////renderTargetOffScreen = ToDisposeContent(RenderTarget2D.New(GraphicsDevice, backDesc.Width, backDesc.Height, MSAALevel.X8,backDesc.Format));
            //for (int i = 0; i < renderTargetDownScales.Length; i++)
            //{
            //    renderTargetDownScales[i] = ToDisposeContent(RenderTarget2D.New(GraphicsDevice, backDesc.Width >> i, backDesc.Height >> i, 1, backDesc.Format));
            //}
            //renderTargetBlurTemp = ToDisposeContent((RenderTarget2D)renderTargetDownScales[renderTargetDownScales.Length - 1].Clone());

            basicEffect = ToDispose(new BasicEffect(graphicsDevice));
            basicEffect.PreferPerPixelLighting = true;
            basicEffect.EnableDefaultLighting();

           // basicEffect.FogColor = (Vector3)Color.Pink;
            basicEffect.FogStart = -100.0f;
            basicEffect.FogEnd = 100.0f;
            basicEffect.FogEnabled = false;
            


           // instancedGeo.FogColor = (Vector3)Color.SeaGreen;
            instancedGeo.FogStart = -100.0f;
            instancedGeo.FogEnd = 100.0f;

            position = new Vector3();

            view = Matrix.LookAtLH(new Vector3(0.0f, 0.0f, -7.0f), new Vector3(0, 0.0f, 0), Vector3.UnitY);
            projection = Matrix.PerspectiveFovLH(0.9f, (float)graphicsDevice.BackBuffer.Width / graphicsDevice.BackBuffer.Height, 0.1f, 100.0f);
            Matrix projInv = Matrix.Invert(projection);
            projInv.Transpose();

            instancedGeo.ProjInv = projInv;

            primitive = ToDispose(GeometricPrimitive.Cylinder.New(graphicsDevice,1f,1f,16));

        }

        protected void Update(GameTime gameTime)
        {
            //TODO
            PlaySounds();            


            basicEffect.View = view;
            basicEffect.Projection = projection;

            instancedGeo.View = view;
            instancedGeo.ViewProj = view * projection;

            instancedGeo.LightColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            instancedGeo.LightView = new Vector4(Vector3.TransformCoordinate(new Vector3(-1.0f, 1.0f, -1.0f), view), 1.0f);

            //keyboardState = keyboard.GetState();
            //mouseState = mouse.GetState();

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

            if (mouseState.right) //Undo the last curve.
            {
                /*                if (gameTime.TotalGameTime.TotalSeconds - lastRemoveEvent >= frequency && (new Vector2(mouseState.X,mouseState.Y)-lastPosition).Length() >= minDistance)
                                {
                                    lastPosition = new Vector2(mouseState.X, mouseState.Y);
                                    lastRemoveEvent = (float)gameTime.TotalGameTime.TotalSeconds;
                                objectColor.Clear();
                               // currentColor.Clear();
                                sound.Clear();

                                    for(int i=0;i<objects.Count;i++)
                                    {
                                        if ((lastPosition - objects[i].Position).Length() < minDistance * 2.0f)
                                        {

                                            for (int j = 0; j < objects.Count; j++)
                                            {
                                                if (objects[j].InstanceId > objects[i].InstanceId)
                                                {
                                                    objects[j] = new DrawObject() { Position = objects[j].Position, InstanceId = objects[j].InstanceId - 1, ObjectColor=objects[j].ObjectColor,SplineId = objects[j].SplineId };
                                                }
                                            }

                                            instancedGeo.RemoveFromRenderPass(cylinder, objects[i].InstanceId);
                                            objects.RemoveAt(i);
                                            i--;
                                        }
                                    }


                                }
                                //objects.Clear();
                                //splines.Clear();

                                //currentPoints = new List<Vector2>();*/
                if (!undoStarted)
                {
                    sound.Undo();
                    undoStarted = true;
                    if (objects.Count > 0)
                    {
                        int splineId = objects[objects.Count - 1].SplineId;
                        for (int i = objects.Count - 1; i >= 0; i--)
                        {
                            if (objects[i].SplineId != splineId)
                                break;
                            instancedGeo.RemoveFromRenderPass(cylinder, objects[i].InstanceId);
                            objects.RemoveAt(i);
                        }
                    }
                }
            }
            else //if mouseState.right
            {
                undoStarted = false;
            }
           
            if (mouseState.X >= 0.69 && mouseState.X <= 0.725 && mouseState.left && 0.91f <= mouseState.Y && mouseState.Y <= 0.97f)
            {
                paletteColor = 0;
            }

            else if (mouseState.X >= 0.77 && mouseState.X <= 0.805 && mouseState.left && 0.91f <= mouseState.Y && mouseState.Y <= 0.97f)
            {
                paletteColor = 1;
            }

            else if (mouseState.X >= 0.85 && mouseState.X <= 0.885 && mouseState.left && 0.91f <= mouseState.Y && mouseState.Y <= 0.97f)
            {
                paletteColor = 2;
            }

            else if (mouseState.X >= 0.93 && mouseState.X <= 0.965 && mouseState.left && 0.91f <= mouseState.Y && mouseState.Y <= 0.97f)
            {
                paletteColor = 3;
            }
            
            
            else if (mouseState.left)//position.Z < 50)
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
           
            else if (drawingStarted)
            {
                drawingStarted = false;

                if (currentSpline != null)
                {

                    foreach (var p in currentPoints)
                    {
                        InstancedGeometricPrimitive.InstanceType point;
                        DrawObject o;

                        computePointData(new Vector3(p,5.0f),out point.World);
                        point.Color = (Vector4)pickColor(p.Y,paletteColor);

                        o.InstanceId = instancedGeo.AddToRenderPass(cylinder, point);
                        o.Position = p;
                        o.ObjectColor = paletteColor;
                        o.SplineId = splines.Count;

                        objects.Add(o);
                    }
                    sound.addCurve(currentPoints, paletteColor);

                    currentPoints.Clear();
                    objectColor.Add(paletteColor);
                    splines.Add(currentSpline);
                }
     
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
            PlaySounds();

            

        }

        //TODO
        private void PlaySounds()
        {
            TimeSpan tmp = stopWatch.Elapsed;


                //**********PARTICLE TEST***//
                foreach (var o in objects)
                {
                    Vector2 l = o.Position;
                    float xTL = (float)((tmp.TotalMilliseconds % 10000) / (float)(10000));
                    if (Math.Abs(l.X - xTL) <= pointFrequency)
                    {
                        if (splines[o.SplineId].pointList.Count < 2)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                pSystem.addParticle(l.X, l.Y, 4, o.ObjectColor);
                            }
                        }
                        else if (Math.Abs(l.X - xTL) <= pointFrequency/10)
                        {
                            for (int j = 0; j < 1; j++)
                            {
                                pSystem.addParticle(l.X, l.Y, 0, o.ObjectColor);
                            }
                        }
                    }
                }
 
/*            if (lastUpdate.Seconds != tmp.Seconds)
            {
                
                lastUpdate = tmp;
                float last, first;
                foreach (var o in objects)
                {
                    Vector2 l = o.Position;

                    first = l.X - 0.05f;
                    last = (l.X) + 0.05f;
                    if ((first * 10) < tmp.Seconds % 10 && (int)(last * 10) >= tmp.Seconds % 10)
                    {
                        sound.Play(l.Y);                      
                    }
                    //Debug.WriteLine(first * 10 + "<" + tmp.Seconds % 10 + "=" + (first * 10 < tmp.Seconds % 10));
                    //Debug.WriteLine(last * 10 + ">" + tmp.Seconds % 10 + "=" + (last * 10 > tmp.Seconds % 10));
                    //Debug.WriteLine(0.5f);

                }
            }*/
        } 


        protected void Draw(GameTime gameTime)
        {
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            TimeSpan tmp = stopWatch.Elapsed;
            float xTL = (float)((tmp.TotalMilliseconds % 10000) / (float)(10000));

            // offline rendering
            graphicsDevice.SetRenderTargets(graphicsDevice.DepthStencilBuffer, graphicsDevice.BackBuffer);

            graphicsDevice.Clear(new Color4(0.316f, 0.451f, 0.473f, 1.0f));//Background color

            float aspectRatio = (float)graphicsDevice.BackBuffer.Width / graphicsDevice.BackBuffer.Height;
            
            Vector2 normalizedPos = normalizeVector2((Vector2)position);


                for (int i = 0; i < objects.Count; i++)
                {

                        if (objects[i].Position.X < xTL - particleFrequency)
                        {
                            instancedGeo.ModifyInstance(cylinder,objects[i].InstanceId,null,new Color4(255, 255, 255, 255));
                        }
                        else
                        {
                            instancedGeo.ModifyInstance(cylinder, objects[i].InstanceId, null, (Vector4)pickColor((float)objects[i].Position.Y,objects[i].ObjectColor));
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

                    for (int l = 0; l < currentPoints.Count; l++)
                    {

                        drawPoint(new Vector3(currentPoints[l], 5.0f), (Vector4)pickColor(currentPoints[l].Y, paletteColor));

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

                    /*if (mouseState.left)//If we want to show that we are drawing
                        //(this is more suitable for kinect interaction I think)
                    {
                        basicEffect.World = Matrix.Scaling(0.5f, 0.05f, 0.5f) *
                            Matrix.RotationX(deg2rad(90.0f)) *
                            Matrix.RotationY(0) *
                            Matrix.RotationZ(0) *
                            Matrix.Translation(screenToWorld(new Vector3(normalizedPos, 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport) + new Vector3(0, 0, -0.200f));
                        basicEffect.DiffuseColor = new Color4(255,255,255,255);
                        primitive.Draw(basicEffect);
                    }*/

                    drawPoint(new Vector3(normalizedPos, 5.0f), (Vector4)pickColor(normalizedPos.Y, paletteColor));


                    //So that you always see the drawing circle
                    /*basicEffect.World = Matrix.Scaling(0.4f, 0.05f, 0.4f) *
                        Matrix.RotationX(deg2rad(90.0f)) *
                        Matrix.RotationY(0) *
                        Matrix.RotationZ(0) *
                        Matrix.Translation(screenToWorld(new Vector3(normalizedPos, 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport) + new Vector3(0, 0, -0.201f));
                    basicEffect.DiffuseColor = (Vector4)pickColor(normalizedPos.Y, paletteColor);
                    primitive.Draw(basicEffect);*/

            instancedGeo.Draw();
            
            /**PARTICLE DRAW********************************************************************/
            foreach (Particle p in pSystem.getList())
            {
                if (p.type == 4)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        basicEffect.World = Matrix.Scaling((0.08f - k*0.01f), (0.08f - k*0.01f), (0.08f - k*0.01f)) *
                            Matrix.RotationX(p.getRotationX()) *
                            Matrix.RotationY(p.getRotationY()) *
                            Matrix.RotationZ(p.getRotationZ()) *
                            Matrix.Translation(screenToWorld(new Vector3(p.getX() - (p.velocity.X * (k*2)), p.getY() - (p.velocity.Y * (k*2)), 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport));

                        // Color4 color = p.getColor();
                        basicEffect.DiffuseColor = p.getColor(); //color;
                      //  basicEffect.Alpha = p.lifespan;
                        fireWork.Draw(basicEffect);
                        basicEffect.Alpha = 1.0f;
                    }
                }
                else
                {
                    basicEffect.World = Matrix.Scaling(0.08f, 0.08f, 0.08f) *
                        Matrix.RotationX(p.getRotationX()) *
                        Matrix.RotationY(p.getRotationY()) *
                        Matrix.RotationZ(p.getRotationZ()) *
                        Matrix.Translation(screenToWorld(new Vector3(p.getX(), p.getY(), 5.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport));

                    // Color4 color = p.getColor();
                    basicEffect.DiffuseColor = p.getColor(); //color;
                    //basicEffect.Alpha = p.lifespan / 400;
                    g.Draw(basicEffect);
                    basicEffect.Alpha = 1.0f;
                }
            }

            /**END PARTICLE DRAW********************************************************************/

            // ----- TIME LINE --------------------
           /* TimeSpan tmp = stopWatch.Elapsed;
            float xTL = (float)((tmp.TotalMilliseconds % 10000) / (float)(10000));*/
            basicEffect.World = Matrix.Scaling(0.1f, 0.1f, graphicsDevice.BackBuffer.Height) *
                Matrix.RotationX(deg2rad(90.0f)) *
                Matrix.RotationY(0) *
                Matrix.RotationZ(0) *
                Matrix.Translation(screenToWorld(new Vector3(xTL, 0.0f, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport));            
            basicEffect.DiffuseColor = new Color4(0.466667f, 0.533333f, 0.6f, 1.0f);//new Color4(0.2f, 0.9f, 0.2f, 0.2f);
            timeLine.Draw(basicEffect);

            // ------- END TIME LINE -------------

            // ----- GUIDE LINES ------------------
            drawGuideLines(15);
            // ----- END GUIDE LINES --------------
           
            
            //-------PALETTE------------------------------
            float xPs = 0.7f;

            for(int i = 0; i < 4; i++)//foreach (Color4 pColor in palette2)
            {
                if (i == paletteColor)
                {
                    basicEffect.World = Matrix.Scaling(0.7f, 0.001f, 0.7f) *
                        Matrix.RotationX(deg2rad(90.0f)) *
                        Matrix.RotationY(0) *
                        Matrix.RotationZ(0) *
                        Matrix.Translation(screenToWorld(new Vector3(xPs, 0.93f, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport) + new Vector3(0, 0, -0.199f));

                    basicEffect.DiffuseColor = new Color4(255,255,255,255);
                    paletteCube.Draw(basicEffect);
                }

                    basicEffect.World = Matrix.Scaling(0.6f, 0.001f, 0.6f) *
                        Matrix.RotationX(deg2rad(90.0f)) *
                        Matrix.RotationY(0) *
                        Matrix.RotationZ(0) *
                        Matrix.Translation(screenToWorld(new Vector3(xPs, 0.93f, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport) + new Vector3(0, 0, -0.2f));

                    basicEffect.DiffuseColor = palette2[i];
                    paletteCube.Draw(basicEffect);
          
                xPs = xPs + 0.08f;
            }

            
            //----------END PALETTE-------------------------


            // -------- GRADIENT BACKGROUND ----------------
            float eltY = -0.02f;
      /*      float redY = bckgdTopColor[0];
            float greenY = bckgdTopColor[1];
            float blueY = bckgdTopColor[2];
            float deltaRed = gradientBckgdColor[0] / 55;
            float deltaGreen = gradientBckgdColor[1] / 55;
            float deltaBlue = gradientBckgdColor[2] / 55; */

            float redY = 1.0f;
            float greenY = 1.0f;
            float blueY = 1.0f;
        
            basicEffect.LightingEnabled = false;
            for (int i=0; i<55; i++)
            {
                eltY += 0.02f;
                redY -= 0.020f;
                greenY -= 0.020f;//22
                blueY -= 0.020f;//18
              //  redY -= deltaRed;
              //  greenY -= deltaGreen;
              //  blueY -= deltaBlue;
                basicEffect.World = Matrix.Scaling(graphicsDevice.BackBuffer.Width, 0.05f, 0.2f) *
                    Matrix.RotationX(deg2rad(90.0f)) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(screenToWorld(new Vector3(0.0f, eltY, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport)
                                                    + new Vector3(0, 0, 0.2f));

                basicEffect.DiffuseColor = new Color4(redY, greenY, blueY, 0.0f);
                backgroundElt.Draw(basicEffect);
            }
            basicEffect.LightingEnabled = true;
            // --------- END GRADIENT BACKGROUND -----------




            // --- Trying to add anti-aliasing
            //RasterizerState rState = GraphicsDevice.RasterizerStates.Default;
            //SharpDX.Direct3D11.RasterizerStateDescription rStateDesc = rState.Description;
            //rStateDesc.IsMultisampleEnabled = true;
            //rStateDesc.IsAntialiasedLineEnabled = true;
            //RasterizerState newRState = RasterizerState.New(GraphicsDevice, rStateDesc);
            //GraphicsDevice.SetRasterizerState(newRState);
            // --- End anti-aliasing

            //GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.Default);
            //GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            //GraphicsDevice.SetDepthStencilState(GraphicsDevice.DepthStencilStates.None);

            //const float brightPassThreshold = 0.5f;
            //GraphicsDevice.SetRenderTargets(renderTargetDownScales[0]);
            //bloomEffect.CurrentTechnique = bloomEffect.Techniques["BrightPassTechnique"];
            //bloomEffect.Parameters["Texture"].SetResource(renderTargetOffScreen);
            //bloomEffect.Parameters["PointSampler"].SetResource(GraphicsDevice.SamplerStates.PointClamp);
            //bloomEffect.Parameters["BrightPassThreshold"].SetValue(brightPassThreshold);
            //GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[0]);

            //// Down scale passes
            //for (int i = 1; i < renderTargetDownScales.Length; i++)
            //{
            //    GraphicsDevice.SetRenderTargets(renderTargetDownScales[i]);
            //    GraphicsDevice.DrawQuad(renderTargetDownScales[0]);
            //}

            //// Horizontal blur pass
            //var renderTargetBlur = renderTargetDownScales[renderTargetDownScales.Length - 1];
            //GraphicsDevice.SetRenderTargets(renderTargetBlurTemp);
            //bloomEffect.CurrentTechnique = bloomEffect.Techniques["BlurPassTechnique"];
            //bloomEffect.Parameters["Texture"].SetResource(renderTargetBlur);
            //bloomEffect.Parameters["LinearSampler"].SetResource(GraphicsDevice.SamplerStates.LinearClamp);
            //bloomEffect.Parameters["TextureTexelSize"].SetValue(new Vector2(1.0f / renderTargetBlurTemp.Width, 1.0f / renderTargetBlurTemp.Height));
            //GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[0]);

            //// Vertical blur pass
            //GraphicsDevice.SetRenderTargets(renderTargetBlur);
            //bloomEffect.Parameters["Texture"].SetResource(renderTargetBlurTemp);
            //GraphicsDevice.DrawQuad(bloomEffect.CurrentTechnique.Passes[1]);

            // Render to screen
            //GraphicsDevice.SetRenderTargets(GraphicsDevice.BackBuffer);
            //GraphicsDevice.DrawQuad(renderTargetOffScreen);

            //GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);
            //GraphicsDevice.DrawQuad(renderTargetBlur);

            //GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);

            //Console.WriteLine(kinect.xYDepth);

        }

        private void drawPoint(Vector3 pos, Vector4 col)
        {
            basicEffect.World = Matrix.Scaling(0.3f, 0.02f, 0.3f) *
                                    Matrix.RotationX(deg2rad(90.0f)) *
                                    Matrix.RotationY(0) *
                                    Matrix.RotationZ(0) *
                                    Matrix.Translation(screenToWorld(pos, basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport));
            basicEffect.DiffuseColor = col;
            primitive.Draw(basicEffect);
        }

        private void drawGuideLines(int number)
        {
            if (number < 3) { number = 3; }

            float y = 0.0f;
            float deltaY = (float)1 / (number - 1);

            for(int i=0; i<number; i++)
            {
                basicEffect.World = Matrix.Scaling(graphicsDevice.BackBuffer.Width, 0.0f, 0.022f) *
                    Matrix.RotationX(deg2rad(90.0f)) *
                    Matrix.RotationY(0) *
                    Matrix.RotationZ(0) *
                    Matrix.Translation(screenToWorld(new Vector3(0.0f, y, 0.0f), basicEffect.View, basicEffect.Projection, Matrix.Identity, graphicsDevice.Viewport)
                                                    + new Vector3(0, 0, 0.001f));

                basicEffect.DiffuseColor = new Color4(0.2f, 0.2f, 0.2f, 0.2f);
                guideLine.Draw(basicEffect);

                y += deltaY;
            }
        }

        private void computePointData(Vector3 pos, out Matrix world)
        {
            world = Matrix.Scaling(0.3f, 0.02f, 0.3f) *
                                    Matrix.RotationX(deg2rad(90.0f)) *
                                    Matrix.RotationY(0) *
                                    Matrix.RotationZ(0) *
                                    Matrix.Translation(screenToWorld(pos, view,projection, Matrix.Identity, graphicsDevice.Viewport));
        }

        private float deg2rad(float angle)
        {
            return angle / 180.0f * (float)Math.PI;
        }

        private Color pickColor(float ratio, int color)
        {
            colorList = palette[color];
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
