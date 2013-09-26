using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bespoke.Common;
using Bespoke.Common.Osc;
using System.Threading;
using System.Net;

namespace MusicWall3D
{

        public class Sound
        {
            private static readonly IPEndPoint Destination = new IPEndPoint(0X0100007f, 57120);
            private Thread mTransmitterThread;
            private OscPacket mPacket;

            //Function for playing the right notes at the right time.
            public void play(int time)
            {
                Kick();
            }


            public void Kick()
            {
                OscMessage message = new OscMessage(Destination, "/soundWall/kick");
                //message.Append(333);
                sendOsc(message);
            }

            public void sendOsc(OscMessage message)
            {
                OscBundle bundle = new OscBundle(Destination); 
                bundle.Append(message);
                OscPacket packet = bundle;

                Assert.ParamIsNotNull(bundle);
                mPacket = packet;
                mTransmitterThread = new Thread(RunWorker);
                mTransmitterThread.Start();

            }

            public void Stop()
            {
                mTransmitterThread.Join();
            }

            private void RunWorker()
            {
                try
                {
                    mPacket.Send(Destination);
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }
    
}
