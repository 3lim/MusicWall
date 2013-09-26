using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

//using SharpDX.Direct3D11;
//using SharpDX.D3DCompiler;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
namespace ParticleLibrary
{
    class ParticleSystem
    {
        public List<Particle> pSystem = new List<Particle>();
        private GraphicsDeviceManager graphicsDeviceManager;
        private Random ran = new Random();
        

        public ParticleSystem(GraphicsDeviceManager gdm)
        {

            graphicsDeviceManager = gdm;

        }

        public void addParticle(int x, int y)//, ParticleType p, int life)
        {
            int i = ran.Next(1, 100);
            pSystem.Add(new Glitter(x, y, 200, graphicsDeviceManager, i));
        }

        public List<Particle> getList()
        {
            return pSystem;
        }

        public void removeParticle(Particle p)
        {
            pSystem.Remove(p);
        }     

    }
}
