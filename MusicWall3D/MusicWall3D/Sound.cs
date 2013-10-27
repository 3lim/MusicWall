﻿using System;
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
       
            public Sound(IntPtr handle)
            {
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

                for(int i = 0; i < sounds.Length; i++){
                    sounds[i] = 0;
                }
            }
            //Takes a mouseState.Y (float between 0 and 1) and playes a tune.
            public void Play(float Y)
            {
                Y = Y * 100;
                if (Y > 90)
                    Kick();
                else if (Y > 80)
                    Snare();
                //else
                    //Note(10 * (100 - (int)Y));
            }

            public void addCurve(List<Vector2> points)
            {
                int i = 0;
                int freq = -1;
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
                        if (p.X <= 1)
                        {
                            while (i < p.X * numberOfSamples)
                            {
                                if (freq != -1)
                                {
                                    short value = smoothCurve(freq, i, ref waveInfo);
                                    tmpSound[i] = value;
                                }
                                i++;
                            }
//                            freq = (int)(880 - p.Y * 880);
                            double a = Math.Pow(2.0, 1.0 / 12.0);
                            double power = -p.Y * 3*12;
                            freq = (int)(880 * Math.Pow(a, power));

                        }
                    }
                    backPass(ref tmpSound, i-1);
                    for(int j = 0; j<sounds.Length;j++)
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
            }

            public void Clear() {
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

            //TODO makt some sort of object.
            private short smoothCurve(int freq, int i, ref Wave info) {
                if (info.lastFreq == -1)
                {
                    info.lastFreq = freq;
                    //Debug.WriteLine("HEJ");
                }
                short value = (short)(Math.Sin(2 * Math.PI * (info.lastFreq) * (i - info.offset) / waveFormat.SampleRate) * 1000); // Not too loud
                if (freq != info.lastFreq)
                {
                    if (info.lastVal < 0 && value >= 0)
                    {
                        info.lastFreq = freq;
                        info.offset = i;
                        value = (short)(Math.Sin(2 * Math.PI * (info.lastFreq) * (i - info.offset) / waveFormat.SampleRate) * 1000); // Not too loud
                    }
                }
                info.lastVal = value;
                return value;
            }
            private short squareCurve(int freq, int i, ref Wave info) {
                short value = smoothCurve(freq, i, ref info);
                if (value > 0)
                    value = 1000;
                else
                    value = -1000;
                return value;
            }

            private void backPass(ref short[] tmpSound,int i) {
                short lastVal = tmpSound[i];
                short value = 0;
                i--;
                for (; i > 0; i--) {
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

            private void Kick()
            {
            }
            private void Snare()
            {
            }
            private void Note(Vector2 point)
            {
                double a = Math.Pow(2.0, 1.0 / 12.0);
                double power = -point.Y*3*12;
                int freq = (int)(880 * Math.Pow(a,power));
                Debug.WriteLine(freq);
                lastFreq = -1;
                lastVal = -1;
                int numberOfSamples = capabilities.BufferBytes / waveFormat.BlockAlign;
                int i = (int)(point.X * numberOfSamples);
                Wave waveInfo1 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
                Wave waveInfo2 = new Wave { lastVal = -1, lastFreq = -1, offset = i };
                for (int j = 0; j < 10000; j++)
                {
                    short value = (short)(smoothCurve(freq, i, ref waveInfo1)*2);
                    value += smoothCurve(freq*2, i, ref waveInfo1);
                    value = (short)(value * (float)(10000 - j) / 10000.0f);
                    sounds[i] = (short)((int)sounds[i] + (int)value - (int)(sounds[i] * value) / short.MaxValue);
                    i++;
                    if (i == sounds.Length)
                        break;
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
