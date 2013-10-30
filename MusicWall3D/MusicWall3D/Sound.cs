using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Bespoke.Common;
//using System.Threading;
//using System.Net;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.DirectSound;
using System.Collections.Generic;
using System.Collections;

using System.Diagnostics;




namespace MusicWall3D
{
    public struct Wave
    {
        public short lastVal;
        public int lastFreq;
        public int offset;
    }

    public class Sound
    {
        private SecondarySoundBuffer secondarySoundBuffer;
        private BufferCapabilities capabilities;
        private int soundLength = 10;
        WaveFormat waveFormat;
        short[] sounds;
        int lastFreq = -1;
        short lastVal = -1;
        int offset = 0;
        Stack<short[]> soundStack = new Stack<short[]>();
        private Random random = new Random();
        private int volume = 500;
                private int notes = 7;
        int[] test = new int[7];


        public Sound(IntPtr handle)
        {

                        test[1] = test[0] + 2;
            test[2] = test[1] + 2;
            test[3] = test[2] + 1;
            test[4] = test[3] + 2;
            test[5] = test[4] + 2;
            test[6] = test[5] + 2;


            DirectSound directSound = new DirectSound();
            // Set Cooperative Level to PRIORITY (priority level can call the SetFormat and Compact methods)
            //
            directSound.SetCooperativeLevel(handle, CooperativeLevel.Priority);

            // Create PrimarySoundBuffer
            var primaryBufferDesc = new SoundBufferDescription();
            primaryBufferDesc.Flags = BufferFlags.PrimaryBuffer;
            primaryBufferDesc.AlgorithmFor3D = Guid.Empty;

            var primarySoundBuffer = new PrimarySoundBuffer(directSound, primaryBufferDesc);

            // Play the PrimarySound Buffer
            primarySoundBuffer.Play(0, PlayFlags.Looping);

            // Default WaveFormat Stereo 44100 16 bit
            waveFormat = new WaveFormat();

            // Create SecondarySoundBuffer
            var secondaryBufferDesc = new SoundBufferDescription();
            secondaryBufferDesc.BufferBytes = waveFormat.ConvertLatencyToByteSize(10000);
            secondaryBufferDesc.Format = waveFormat;
            secondaryBufferDesc.Flags = BufferFlags.GetCurrentPosition2 | BufferFlags.ControlPositionNotify | BufferFlags.GlobalFocus |
                                        BufferFlags.ControlVolume | BufferFlags.StickyFocus;
            secondaryBufferDesc.AlgorithmFor3D = Guid.Empty;
            secondarySoundBuffer = new SecondarySoundBuffer(directSound, secondaryBufferDesc);

            // Get Capabilties from secondary sound buffer
            capabilities = secondarySoundBuffer.Capabilities;
            sounds = new short[capabilities.BufferBytes / waveFormat.BlockAlign];

            // Play the song
            secondarySoundBuffer.Play(0, PlayFlags.Looping);

            for (int i = 0; i < sounds.Length; i++)
            {
                sounds[i] = 0;
            }
        }
        //Takes a mouseState.Y (float between 0 and 1) and playes a tune.
/*        public void Play(float Y)
        {
            Y = Y * 100;
 //           if (Y > 90)
 //               Kick();
            else if (Y > 80)
                Snare();
            //else
            //Note(10 * (100 - (int)Y));
        }
*/
        public void addCurve(List<Vector2> points,int color)
        {
            if (points.Count == 0) return;
            switch (color)
            {
                case 3:
                    addDrums(points);
                    break;
                case 2:
                    addSquare(points);
                    break;
                default:
                    addSine(points);
                    break;
            }
        }
        public void addSine(List<Vector2> points)
        {
                    if (points.Count == 0) return;

                    int i = 0;
                    int freq = -1;
                    double amp = 0;
                    lastFreq = -1;
                    lastVal = -1;
                    int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
                    var tmpSound = new short[sounds.Length];

                    //Debug.WriteLine(points.Count);
                    if (points.Count == 1)
                    {
                        Note(points[0]);
                    }
                    else
                    {
                        Wave waveInfo = new Wave { lastVal = -1, lastFreq = -1, offset = 1 };
                        waveInfo.offset = (int)(points[0].X * numberOfSamples);
                        foreach (Vector2 p in points)
                        {
                            if (p.X <= 1 && p.X >= 0)
                            {
                                if (i < p.X * numberOfSamples)
                                {
                                    while (i < p.X * numberOfSamples)
                                    {
                                        if (freq != -1)
                                        {
                                            short value = (short)(smoothCurve(freq, i, true, ref waveInfo) * amp);
                                            tmpSound[i] = (short)((int)value + (int)tmpSound[i] - (int)(value * tmpSound[i]) / short.MaxValue);
                                        }
                                        i++;
                                    }
                                }
                                else if (i > p.X * numberOfSamples)
                                {
                                    while (i > p.X * numberOfSamples)
                                    {
                                        if (freq != -1)
                                        {
                                            short value = (short)(smoothCurve(freq, i, false, ref waveInfo) * amp);
                                            tmpSound[i] = (short)((int)value + (int)tmpSound[i] - (int)(value * tmpSound[i]) / short.MaxValue);
                                        }
                                        i--;
                                    }

                                }
                                //                            freq = (int)(880 - p.Y * 880);
                                double a = Math.Pow(2.0, 1.0 / 12.0);
                                //double power = (-p.Y * 3 * 12);
                                double power = 13 - (int)(p.Y * 14);
                                //int power = (int)(p.Y * 7);
                                //power = test[power];
                                freq = (int)(440 * Math.Pow(a, power));
                                amp = Math.Pow(2.0, 1 + p.Y);

                            }
                        }
                        backPass(ref tmpSound, i - 1);
                        tmpSound = lowPass(ref tmpSound, 1000);
                        SetBuffer(ref tmpSound);
                    }
            
        }

        public void addSquare(List<Vector2> points)
        {
            if (points.Count == 0) return;

            int i = 0;
            int freq = -1;
            double amp = 0;
            lastFreq = -1;
            lastVal = -1;
            int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
            var tmpSound = new short[sounds.Length];

            //Debug.WriteLine(points.Count);
            if (points.Count == 1)
            {
                squareNote(points[0]);
            }
            else
            {
                Wave waveInfo = new Wave { lastVal = -1, lastFreq = -1, offset = 1 };
                waveInfo.offset = (int)(points[0].X * numberOfSamples);
                foreach (Vector2 p in points)
                {
                    if (p.X <= 1 && p.X >= 0)
                    {
                        if (i < p.X * numberOfSamples)
                        {
                            while (i < p.X * numberOfSamples)
                            {
                                if (freq != -1)
                                {
                                    short value = (short)(squareCurve(freq, i, true, ref waveInfo) * amp);
                                    tmpSound[i] = (short)((int)value + (int)tmpSound[i] - (int)(value * tmpSound[i]) / short.MaxValue);
                                }
                                i++;
                            }
                        }
                        else if (i > p.X * numberOfSamples)
                        {
                            while (i > p.X * numberOfSamples)
                            {
                                if (freq != -1)
                                {
                                    short value = (short)(squareCurve(freq, i, false, ref waveInfo) * amp);
                                    tmpSound[i] = (short)((int)value + (int)tmpSound[i] - (int)(value * tmpSound[i]) / short.MaxValue);
                                }
                                i--;
                            }

                        }
                        double a = Math.Pow(2.0, 1.0 / 12.0);
                        double power = (-p.Y * 3 * 12);
                        freq = (int)(880 * Math.Pow(a, power));
                        amp = Math.Pow(2.0, 1 + p.Y);

                    }
                }
                backPass(ref tmpSound, i - 1);
                tmpSound = lowPass(ref tmpSound, 1000);
                SetBuffer(ref tmpSound);
            }

        }


        public void addDrums(List<Vector2> points)
        {
            var tmpSound = new short[sounds.Length];
            int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
            if (points.Count < 7)
            {
                var p = points[0];
                int test = (int)(p.Y * 2);
                        Kick((int)(p.X * numberOfSamples), ref tmpSound);

            }
            else
            {
                int i = 0;  
                int freq = 0;
                bool notFirst = false;

                foreach (Vector2 p in points)
                {
                    if (p.X <= 1 && p.X >= 0)
                    {
                        if (i < p.X * numberOfSamples)
                        {
                            while (i < p.X * numberOfSamples)
                            {
                                if (notFirst)
                                {
                                    if (((i+5346) % freq) == 0)
                                        Klick(i, ref tmpSound);
                                }
                                i++;
                            }
                        }
                        else if (i > p.X * numberOfSamples)
                        {
                            while (i > p.X * numberOfSamples)
                            {
                                if (notFirst)
                                {
                                    if ((i % freq) == 0)
                                        Klick(i, ref tmpSound);
                                }
                                i--;
                            }

                        }
                    }
                    notFirst = true;
                    freq = 4-(int)(p.Y * 4 + 1);
                    freq = (int)(Math.Pow(2, freq));
                    freq = (int)(waveFormat.SampleRate/freq);
                }
            }
            SetBuffer(ref tmpSound);
        }
        private void Kick(int start, ref short[] tmpSound)
        {
            Wave waveInfo = new Wave { lastVal = -1, lastFreq = -1, offset = start };
            short value = 0;
            short[] click = new short[tmpSound.Length];
            double volume = 40.0;
            for (int i = start; i < start + waveFormat.SampleRate; i++)
            {
                value = smoothCurve(60, i, true, ref waveInfo);
                value = (short)(value * (volume*line(i-start,waveFormat.SampleRate/2)));
                tmpSound[i%tmpSound.Length] = value;
                click[i % tmpSound.Length] = whiteNoise();
                click[i % tmpSound.Length] = (short)(click[i % tmpSound.Length] * ((volume / 4) * line(i - start, waveFormat.SampleRate / 10)));
            }
            click = lowPass(ref click, 1500);
            for(int i = 0; i<click.Length; i++)
            {
                tmpSound[i] += click[i];
            }
        }

        private void Klick(int start, ref short[] tmpSound)
        {
            Wave waveInfo = new Wave { lastVal = -1, lastFreq = -1, offset = start };
            for (int i = start; i < start + 100; i++)
            {
                tmpSound[i%tmpSound.Length] = (short)(smoothCurve(60, i, true, ref waveInfo)*20 + whiteNoise());
            }
        }

        private double line(int pos, int length)
        {
            if (pos < length)
                return (double)(length - pos) / (double)(length);
            else
                return 0;
        }

        private void SetBuffer(ref short[] tmpSound)
        {
            soundStack.Push((short[])sounds.Clone());
            //tmpSound = lowPass(ref tmpSound, 1000);
            for (int j = 0; j < sounds.Length; j++)
            {
                sounds[j] = (short)((int)sounds[j] + (int)tmpSound[j] - (int)(sounds[j] * tmpSound[j]) / short.MaxValue);
            }
            // Lock the buffer
            DataStream dataPart2;
            var dataPart1 = secondarySoundBuffer.Lock(0, capabilities.BufferBytes, LockFlags.EntireBuffer, out dataPart2);
            foreach (short value in sounds)
            {
                dataPart1.Write(value);
                dataPart1.Write(value);
            }
            // Unlock the buffer
            secondarySoundBuffer.Unlock(dataPart1, dataPart2);
        }

        public void Clear()
        {
            sounds = new short[sounds.Length];
            // Lock the buffer
            DataStream dataPart2;
            var dataPart1 = secondarySoundBuffer.Lock(0, capabilities.BufferBytes, LockFlags.EntireBuffer, out dataPart2);
            foreach (short value in sounds)
            {
                dataPart1.Write(value);
                dataPart1.Write(value);
            }
            // Unlock the buffer
            secondarySoundBuffer.Unlock(dataPart1, dataPart2);

        }

        public void Undo()
        {
            if (soundStack.Count > 0)
                sounds = soundStack.Pop();
            // Lock the buffer
            DataStream dataPart2;
            var dataPart1 = secondarySoundBuffer.Lock(0, capabilities.BufferBytes, LockFlags.EntireBuffer, out dataPart2);
            foreach (short value in sounds)
            {
                dataPart1.Write(value);
                dataPart1.Write(value);
            }
            // Unlock the buffer
            secondarySoundBuffer.Unlock(dataPart1, dataPart2);

        }

        //TODO makt some sort of object.
        private short smoothCurve(int freq, int i, bool left2right, ref Wave info)
        {
            if (info.lastFreq == -1)
            {
                info.lastFreq = freq;
                //Debug.WriteLine("HEJ");
            }
            short value = (short)(Math.Sin(2 * Math.PI * (info.lastFreq) * (i - info.offset) / waveFormat.SampleRate) * 500); // Not too loud
            if (freq != info.lastFreq)
            {
                if (left2right)
                {
                    if (info.lastVal < 0 && value >= 0)
                    {
                        info.lastFreq = freq;
                        info.offset = i;
                        value = (short)(Math.Sin(2 * Math.PI * (info.lastFreq) * (i - info.offset) / waveFormat.SampleRate) * 500); // Not too loud
                    }
                }
                else
                {
                    if (info.lastVal > 0 && value <= 0)
                    {
                        info.lastFreq = freq;
                        info.offset = i;
                        value = (short)(Math.Sin(2 * Math.PI * (info.lastFreq) * (i - info.offset) / waveFormat.SampleRate) * 500); // Not too loud
                    }

                }

            }
            info.lastVal = value;
            return value;
        }
        private short squareCurve(int freq, int i, bool left2right, ref Wave info)
        {
            short value = smoothCurve(freq, i, left2right, ref info);
            if (value > 0)
                value = 1000;
            else
                value = -1000;
            return value;
        }

        private short whiteNoise()
        {
            short value = (short)(random.Next(-volume, volume));
            return value;
        }

        private void backPass(ref short[] tmpSound, int i)
        {
            short lastVal = tmpSound[i];
            short value = 0;
            i--;
            for (; i > 0; i--)
            {
                value = tmpSound[i];
                if (lastVal < 0 && value >= 0)
                    break;
                else
                {
                    tmpSound[i] = 0;
                    lastVal = value;
                }
            }
        }
        //TODO
        private short[] lowPass(ref short[] oldSound, int freq)
        {
            double RC = 1.0 / (2.0 * Math.PI * freq);
            double dt = 1.0 / (sounds.Length / 10.0);
            double a = dt / (RC + dt);
            double[] newSound = new double[oldSound.Length];
            newSound[0] = oldSound[0];
            for (int i = 1; i < oldSound.Length; i++)
                newSound[i] = newSound[i - 1] + a * (oldSound[i] - newSound[i - 1]);
            short[] result = new short[newSound.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = (short)newSound[i];
            return result;
        }
        private short[] highPass(ref short[] oldSound, int freq)
        {
            double RC = 1.0 / (2.0 * Math.PI * (double)freq);
            double dt = 1.0 / waveFormat.SampleRate;
            double a = dt / (RC + dt);
            double[] newSound = new double[oldSound.Length];
            newSound[0] = oldSound[0];
            for (int i = 1; i < oldSound.Length; i++)
                newSound[i] = a * (newSound[i - 1] + oldSound[i] - oldSound[i - 1]);
            short[] result = new short[newSound.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = (short)newSound[i];
            return result;
        }


        private void Snare()
        {
        }



        private void Note(Vector2 point)
        {

            var tmpSound = new short[sounds.Length];
            double a = Math.Pow(2.0, 1.0 / 12.0);
            //            double power = (int)(-point.Y * 3 * 12);
            double power = 13 - (int)(point.Y * 14);
            if( power < 7)
                power = test[(int)power%7];
            else
                power = test[(int)power % 7] + 12;
            int freq = (int)(440 * Math.Pow(a, power));
            Debug.WriteLine(freq);
            lastFreq = -1;
            lastVal = -1;
            int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
            int i = (int)(point.X * numberOfSamples);
            Wave waveInfo1 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
            Wave waveInfo2 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
            for (int j = 0; j < 10000; j++)
            {
                short value = (short)(smoothCurve(freq, i, true, ref waveInfo1) * 2);
                value += smoothCurve(freq * 2, i, true, ref waveInfo2);
                value = (short)(value * Math.Pow(2, 1 + point.Y));
                value = (short)(value * (float)(10000 - j) / 10000.0f);
                tmpSound[i] = value;
                i++;
                if (i == sounds.Length)
                    break;
            }
            SetBuffer(ref tmpSound);
        }

        private void squareNote(Vector2 point)
        {
            var tmpSound = new short[sounds.Length];
            double a = Math.Pow(2.0, 1.0 / 12.0);
            double power = (int)(-point.Y * 3 * 12);
//            double power = notes - point.Y * notes;
            int freq = (int)(880 * Math.Pow(a, power));
            Debug.WriteLine(freq);
            lastFreq = -1;
            lastVal = -1;
            int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
            int i = (int)(point.X * numberOfSamples);
            Wave waveInfo1 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
            Wave waveInfo2 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
            for (int j = 0; j < 10000; j++)
            {
                short value = (short)(squareCurve(freq, i, true, ref waveInfo1) * 2);
                value += smoothCurve(freq * 2, i, true, ref waveInfo1);
                value = (short)(value * Math.Pow(2, 1 + point.Y)/2);
                value = (short)(value * (float)(10000 - j) / 10000.0f);
                tmpSound[i] = value;
                i++;
                if (i == sounds.Length)
                    break;
            }
            SetBuffer(ref tmpSound);
        }

        private void sendOsc()
        {

        }

        public void Stop()
        {
        }

        private void RunWorker()
        {
        }

    }
}
