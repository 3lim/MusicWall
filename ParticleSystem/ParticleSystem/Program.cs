using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Toolkit;
//using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using ParticleLibrary;


namespace ParticleSystem
{

    using SharpDX.Toolkit.Graphics;

	class MainClass
	{
		public MainClass()
		{
            //Text = "My Particle System";
            
		}

		public static void Main (string[] args)
		{

           /* MainClass f = new MainClass();
            f.InitializeGraphics();
            f.Show();
            
            f.BackColor = System.Drawing.Color.PeachPuff;*/
            ParticleType glitter = new Glitter();
            ParticleType star = new Star();
           
            /* Particle need x and y coordinate depending on 
             * where the particles should spawn,           
             * a particle type with maybe dependancy
             * and lifespan maybe with dependency            
             * (Dependency = perhaps what kind of 
             * note or location or layer)*/
           
            Particle p = new Particle(0, 5, glitter, 300);
            Particle p1 = new Particle(10, 10, star, 150);

            
            /*while(f.Created)
            {
                
                f.Render();
                Application.DoEvents();*/
            
            p.Run();          

                
                /*if (p.getLife() > 0)
                {
                    Console.Write("Life = " + p.getLife());  
                    Console.Write(" Xpos = " + p.getX() + " Ypos = " + p.getY());
                    Console.WriteLine();

                    p.draw();
                }

                if (p1.getLife() > 0)
                {
                    Console.Write("Life = " + p1.getLife());   
                    Console.Write(" Xpos = " + p1.getX() + " Ypos = " + p1.getY());
                    Console.WriteLine();

                    p1.draw();
                }*/
            //}
            
            
            //Application.Run(f);        //Or Application.Run(new MainClass());  or f.Run();<- Tho this seems bad..   
            Console.ReadLine();
            //f.Close();

		}

       /*public void InitializeGraphics()
        {
           
        }

        private void Render()
        {

        }

        protected override void OnPaint(PaintEventArgs pea)
        {
            pea.Graphics.DrawString("Mohahaha", this.Font,
                     Brushes.CornflowerBlue, 100, 100);
        }*/
        
	}
}
