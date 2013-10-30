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
        private Random ran = new Random();
        

        public ParticleSystem()
        {

        }

        public void addParticle(float x, float y, int type, int cType)//, ParticleType p, int life)
        {
            int i = ran.Next(1, 100);
            if (type != 4)
            {
                pSystem.Add(new Glitter(x, y, 400, i, type, cType));
            }

            else
            {

                pSystem.Add(new Fireworks(x, y, 1400, i, type, cType));

            }
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
