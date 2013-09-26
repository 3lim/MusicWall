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

            //Takes a mouseState.Y (float between 0 and 1) and playes a tune.
            public void Play(float Y)
            {
                Y = Y * 100;
                if (Y > 90)
                    Kick();
                else if (Y > 80)
                    Snare();
                else
                    Note(10 * (100 - (int)Y));
            }


            private void Kick()
            {
                OscMessage message = new OscMessage(Destination, "/soundWall/kick");
                //message.Append(333);
                sendOsc(message);
            }
            private void Snare()
            {
                OscMessage message = new OscMessage(Destination, "/soundWall/snare");
                sendOsc(message);
            }
            private void Note(int freq)
            {
                OscMessage message = new OscMessage(Destination, "/soundWall/note");
                message.Append(freq);
                sendOsc(message);
            }

            private void sendOsc(OscMessage message)
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
