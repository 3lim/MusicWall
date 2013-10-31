using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.D3DCompiler;
using SharpDX.Toolkit.Graphics;
using MusicWall3D.Properties;

namespace MusicWall3D
{
    class InstancedGeometricPrimitive : Component
    {
        public const int MAX_INSTANCES_PER_BUFFER = 10000;

        [StructLayout(LayoutKind.Sequential)]
        struct VertexIn
        {
            public Vector3 Position;
            public Vector3 Normal;
            //public Vector2 Tex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InstanceType
        {
            public Matrix World;
            public Vector4 Color;

            public static bool operator ==(InstanceType a, InstanceType b)
            {
                return a.World == b.World && a.Color == b.Color;
            }

            public static bool operator !=(InstanceType a, InstanceType b)
            {
                return !(a == b);
            }
        }

        public struct Primitive
        {
            public SharpDX.Toolkit.Graphics.Buffer VertexBuffer;
            public SharpDX.Toolkit.Graphics.Buffer IndexBuffer;
        }

        public class InstanceData
        {
            public SharpDX.Toolkit.Graphics.Buffer<InstanceType> InstanceBuffer;
            public List<InstanceType> Instances;
            public int InstanceCount;
            public List<int> DeletedInstances;

            public int GetInstanceId(int instanceId)
            {
                DeletedInstances.Sort();

                int i = 0;
                for (; i < DeletedInstances.Count; i++)
                {
                    if (DeletedInstances[i] >= instanceId) break;
                }

                return instanceId - i;
            }
        }

        #region Primitives
        private const int CubeFaceCount = 6;

        private static readonly Vector3[] faceNormals = new Vector3[CubeFaceCount]
                {
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, -1, 0),
                };

        public static Primitive CreateSphere(GraphicsDevice device)
        {
            Primitive p = new Primitive();
            float diameter = 1.0f;
            int tessellation = 16;

            int verticalSegments = tessellation;
            int horizontalSegments = tessellation * 2;

            var vertices = new VertexIn[(verticalSegments + 1) * (horizontalSegments + 1)];
            var indices = new int[(verticalSegments) * (horizontalSegments + 1) * 6];

            float radius = diameter / 2;

            int vertexCount = 0;
            // Create rings of vertices at progressively higher latitudes.
            for (int i = 0; i <= verticalSegments; i++)
            {
                float v = 1.0f - (float)i / verticalSegments;

                var latitude = (float)((i * Math.PI / verticalSegments) - Math.PI / 2.0);
                var dy = (float)Math.Sin(latitude);
                var dxz = (float)Math.Cos(latitude);

                // Create a single ring of vertices at this latitude.
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    float u = (float)j / horizontalSegments;

                    var longitude = (float)(j * 2.0 * Math.PI / horizontalSegments);
                    var dx = (float)Math.Sin(longitude);
                    var dz = (float)Math.Cos(longitude);

                    dx *= dxz;
                    dz *= dxz;

                    var normal = new Vector3(dx, dy, dz);
                    var textureCoordinate = new Vector2(u, v);

                    vertices[vertexCount++] = new VertexIn() { Position = normal * radius, Normal = normal };
                }
            }

            // Fill the index buffer with triangles joining each pair of latitude rings.
            int stride = horizontalSegments + 1;

            int indexCount = 0;
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j <= horizontalSegments; j++)
                {
                    int nextI = i + 1;
                    int nextJ = (j + 1) % stride;

                    indices[indexCount++] = (i * stride + j);
                    indices[indexCount++] = (nextI * stride + j);
                    indices[indexCount++] = (i * stride + nextJ);

                    indices[indexCount++] = (i * stride + nextJ);
                    indices[indexCount++] = (nextI * stride + j);
                    indices[indexCount++] = (nextI * stride + nextJ);
                }
            }

            for (int i = 0; i < indices.Length; i += 3)
            {
                Utilities.Swap(ref indices[i], ref indices[i + 2]);
            }

            p.IndexBuffer = SharpDX.Toolkit.Graphics.Buffer.Index.New(device, indices);
            p.VertexBuffer = SharpDX.Toolkit.Graphics.Buffer.Vertex.New(device, vertices);

            return p;
        }

        public static Primitive CreateCube(GraphicsDevice device)
        {
            Primitive p = new Primitive();
            float size = 1.0f;

            var vertices = new VertexIn[CubeFaceCount * 4];
            var indices = new int[CubeFaceCount * 6];

            size /= 2.0f;

            int vertexCount = 0;
            int indexCount = 0;
            // Create each face in turn.
            for (int i = 0; i < CubeFaceCount; i++)
            {
                Vector3 normal = faceNormals[i];

                // Get two vectors perpendicular both to the face normal and to each other.
                Vector3 basis = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;

                Vector3 side1;
                Vector3.Cross(ref normal, ref basis, out side1);

                Vector3 side2;
                Vector3.Cross(ref normal, ref side1, out side2);

                // Six indices (two triangles) per face.
                int vbase = i * 4;
                indices[indexCount++] = (vbase + 0);
                indices[indexCount++] = (vbase + 1);
                indices[indexCount++] = (vbase + 2);

                indices[indexCount++] = (vbase + 0);
                indices[indexCount++] = (vbase + 2);
                indices[indexCount++] = (vbase + 3);

                // Four vertices per face.
                vertices[vertexCount++] = new VertexIn() { Position = (normal - side1 - side2) * size, Normal = normal };
                vertices[vertexCount++] = new VertexIn() { Position = (normal - side1 + side2) * size, Normal = normal };
                vertices[vertexCount++] = new VertexIn() { Position = (normal + side1 + side2) * size, Normal = normal };
                vertices[vertexCount++] = new VertexIn() { Position = (normal + side1 - side2) * size, Normal = normal };

            }

            for (int i = 0; i < indices.Length; i += 3)
            {
                Utilities.Swap(ref indices[i], ref indices[i + 2]);
            }

            p.IndexBuffer = SharpDX.Toolkit.Graphics.Buffer.Index.New(device, indices);
            p.VertexBuffer = SharpDX.Toolkit.Graphics.Buffer.Vertex.New(device, vertices);

            return p;
        }

        public static Primitive CreateCylinder(GraphicsDevice device)
        {
            Primitive p = new Primitive();

            float height = 1.0f, diameter = 1.0f;
            int tessellation = 32;

            var vertices = new List<VertexIn>();
            var indices = new List<int>();

            height /= 2;

            var topOffset = Vector3.UnitY * height;

            float radius = diameter / 2;
            int stride = tessellation + 1;

            // Create a ring of triangles around the outside of the cylinder.
            for (int i = 0; i <= tessellation; i++)
            {
                var normal = GetCircleVector(i, tessellation);

                var sideOffset = normal * radius;

                var textureCoordinate = new Vector2((float)i / tessellation, 0);

                vertices.Add(new VertexIn() { Position = sideOffset + topOffset, Normal = normal });
                vertices.Add(new VertexIn() { Position = sideOffset - topOffset, Normal = normal });

                indices.Add(i * 2);
                indices.Add((i * 2 + 2) % (stride * 2));
                indices.Add(i * 2 + 1);

                indices.Add(i * 2 + 1);
                indices.Add((i * 2 + 2) % (stride * 2));
                indices.Add((i * 2 + 3) % (stride * 2));
            }

            // Create flat triangle fan caps to seal the top and bottom.
            CreateCylinderCap(vertices, indices, tessellation, height, radius, true);
            CreateCylinderCap(vertices, indices, tessellation, height, radius, false);

            VertexIn[] aVertices = vertices.ToArray();
            int[] aIndices = indices.ToArray();

            for (int i = 0; i < aIndices.Length; i += 3)
            {
                Utilities.Swap(ref aIndices[i], ref aIndices[i + 2]);
            }

            p.IndexBuffer = SharpDX.Toolkit.Graphics.Buffer.Index.New(device, aIndices);
            p.VertexBuffer = SharpDX.Toolkit.Graphics.Buffer.Vertex.New(device, aVertices);

            return p;
        }

        private static Vector3 GetCircleVector(int i, int tessellation)
        {
            var angle = (float)(i * 2.0 * Math.PI / tessellation);
            var dx = (float)Math.Sin(angle);
            var dz = (float)Math.Cos(angle);

            return new Vector3(dx, 0, dz);
        }

        private static void CreateCylinderCap(List<VertexIn> vertices, List<int> indices, int tessellation, float height, float radius, bool isTop)
        {
            // Create cap indices.
            for (int i = 0; i < tessellation - 2; i++)
            {
                int i1 = (i + 1) % tessellation;
                int i2 = (i + 2) % tessellation;

                if (isTop)
                {
                    Utilities.Swap(ref i1, ref i2);
                }

                int vbase = vertices.Count;
                indices.Add(vbase);
                indices.Add(vbase + i1);
                indices.Add(vbase + i2);
            }

            // Which end of the cylinder is this?
            var normal = Vector3.UnitY;
            var textureScale = new Vector2(-0.5f);

            if (!isTop)
            {
                normal = -normal;
                textureScale.X = -textureScale.X;
            }

            // Create cap vertices.
            for (int i = 0; i < tessellation; i++)
            {
                var circleVector = GetCircleVector(i, tessellation);
                var position = (circleVector * radius) + (normal * height);
                //var textureCoordinate = new Vector2(circleVector.X * textureScale.X + 0.5f, circleVector.Z * textureScale.Y + 0.5f);

                vertices.Add(new VertexIn() { Position = position, Normal = normal });
            }
        }
        #endregion

        public Matrix ViewProj;
        public Matrix View;
        public Vector4 LightView;
        public Vector4 LightColor;
        public Matrix ProjInv;

        private Dictionary<Primitive, InstanceData> instanceData = new Dictionary<Primitive,InstanceData>();
        private GraphicsDevice device;
        private SharpDX.Direct3D11.Effect effect;
        private InputLayout layout;

        private Dictionary<Primitive, List<int>> toAdd = new Dictionary<Primitive, List<int>>();
        private Dictionary<Primitive, List<int>> toModify = new Dictionary<Primitive, List<int>>();
        private Dictionary<Primitive, List<int>> toRemove = new Dictionary<Primitive, List<int>>();

        private bool invalidateData = true;

        public Vector3 FogColor;
        public float FogStart;
        public float FogEnd;

        public InstancedGeometricPrimitive(GraphicsDevice d)
        {
            device = d;
            byte[] bCode = ShaderBytecode.Compile(Resources.BasicEffect, "fx_5_0");
            effect = new SharpDX.Direct3D11.Effect(d, bCode);
            bCode = ShaderBytecode.Compile(Resources.BasicEffect, "BasicVS", "vs_5_0");

            Device nativeDevice = (Device)device;
            ShaderSignature inputSignature = ShaderSignature.GetInputSignature(bCode);
            
            layout = ToDispose(new InputLayout(nativeDevice, inputSignature.Data, new InputElement[] {
                new InputElement("SV_Position",0,SharpDX.DXGI.Format.R32G32B32_Float,InputElement.AppendAligned,0,InputClassification.PerVertexData,0),
                new InputElement("NORMAL",0,SharpDX.DXGI.Format.R32G32B32_Float,InputElement.AppendAligned,0,InputClassification.PerVertexData,0),

                new InputElement("WORLD",0,SharpDX.DXGI.Format.R32G32B32A32_Float,InputElement.AppendAligned,1,InputClassification.PerInstanceData,1),
                new InputElement("WORLD",1,SharpDX.DXGI.Format.R32G32B32A32_Float,InputElement.AppendAligned,1,InputClassification.PerInstanceData,1),
                new InputElement("WORLD",2,SharpDX.DXGI.Format.R32G32B32A32_Float,InputElement.AppendAligned,1,InputClassification.PerInstanceData,1),
                new InputElement("WORLD",3,SharpDX.DXGI.Format.R32G32B32A32_Float,InputElement.AppendAligned,1,InputClassification.PerInstanceData,1),
                new InputElement("COLOR",0,SharpDX.DXGI.Format.R32G32B32A32_Float,InputElement.AppendAligned,1,InputClassification.PerInstanceData,1)
       
            }));

        }

        public void ResetInstances(Primitive p)
        {
            if (!instanceData.ContainsKey(p)) return;

            //instanceData[p].Instances.Clear();
            instanceData[p].InstanceCount = 0;
        }

        public int AddToRenderPass(Primitive p, InstanceType t)
        {
            if (!instanceData.ContainsKey(p)) InitializeBuffers(p);

            int instanceId = instanceData[p].InstanceCount;// +instanceData[p].DeletedInstances.Count;

            t.World.Transpose();
            
            instanceData[p].Instances[instanceId] = t;
            instanceData[p].InstanceCount++;

            toAdd[p].Add(instanceId);//instanceData[p].GetInstanceId(instanceId));

            invalidateData = true;

            return instanceId;
        }

        public void ModifyInstance(Primitive p, int instanceId, InstanceType t)
        {
            //instanceId = instanceData[p].GetInstanceId(instanceId);

            instanceData[p].Instances[instanceId] = t;

            toModify[p].Add(instanceId);

            invalidateData = true;
        }

        public void ModifyInstance(Primitive p, int instanceId, Matrix? world = null, Vector4? color = null)
        {
            //instanceId = instanceData[p].GetInstanceId(instanceId);

            InstanceType t = instanceData[p].Instances[instanceId];
            if (world != null)
            {
                t.World = (Matrix)world;
            }
            if (color != null)
            {
                t.Color = (Vector4)color;
            }

            if (t != instanceData[p].Instances[instanceId])
            {
                instanceData[p].Instances[instanceId] = t;

                toModify[p].Add(instanceId);

                invalidateData = true;
            }
        }

        public void RemoveFromRenderPass(Primitive p, int instanceId)
        {
            //int rInstanceId = instanceData[p].GetInstanceId(instanceId);
            //instanceData[p].DeletedInstances.Add(instanceId);

            instanceData[p].Instances.RemoveAt(instanceId);
            instanceData[p].Instances.Add(new InstanceType());
            instanceData[p].InstanceCount--;

            toRemove[p].Add(instanceId);

            invalidateData = true;
        }

        private void InitializeBuffers(Primitive p)
        {
            instanceData[p] = new InstanceData();

            InstanceType[] a = new InstanceType[MAX_INSTANCES_PER_BUFFER];
            a.Initialize();
            instanceData[p].Instances = a.ToList();
            instanceData[p].InstanceCount = 0;
            instanceData[p].DeletedInstances = new List<int>();

            instanceData[p].InstanceBuffer = ToDispose(SharpDX.Toolkit.Graphics.Buffer.New(device,a,BufferFlags.VertexBuffer,ResourceUsage.Dynamic));

            toAdd[p] = new List<int>();
            toModify[p] = new List<int>();
            toRemove[p] = new List<int>();
        }

        public void Draw()
        {
            if (instanceData.Count == 0) return;

            DeviceContext context = (DeviceContext)device;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = layout;

            effect.GetVariableByName("view").AsMatrix().SetMatrix(View);
            effect.GetVariableByName("viewProj").AsMatrix().SetMatrix(ViewProj);
            effect.GetVariableByName("projInv").AsMatrix().SetMatrix(ProjInv);

            effect.GetVariableByName("DiffuseColor").AsVector().Set(Vector3.One);
            effect.GetVariableByName("SpecularColor").AsVector().Set(Vector3.One);
            effect.GetVariableByName("EmissiveColor").AsVector().Set(Vector3.Zero);
            effect.GetVariableByName("SpecularPower").AsScalar().Set(16.0f);

            // Key light.
            effect.GetVariableByName("DirLight0Direction").AsVector().Set(new Vector3(-0.5265408f, -0.5735765f, -0.6275069f));
            effect.GetVariableByName("DirLight0DiffuseColor").AsVector().Set(new Vector3(1, 0.9607844f, 0.8078432f));
            effect.GetVariableByName("DirLight0SpecularColor").AsVector().Set(new Vector3(1, 0.9607844f, 0.8078432f));

            // Fill light.
            effect.GetVariableByName("DirLight1Direction").AsVector().Set(new Vector3(0.7198464f, 0.3420201f, 0.6040227f));
            effect.GetVariableByName("DirLight1DiffuseColor").AsVector().Set(new Vector3(0.9647059f, 0.7607844f, 0.4078432f));
            effect.GetVariableByName("DirLight1SpecularColor").AsVector().Set(Vector3.Zero);

            // Back light.
            effect.GetVariableByName("DirLight2Direction").AsVector().Set(new Vector3(0.4545195f, -0.7660444f, 0.4545195f));
            effect.GetVariableByName("DirLight2DiffuseColor").AsVector().Set(new Vector3(0.3231373f, 0.3607844f, 0.3937255f));
            effect.GetVariableByName("DirLight2SpecularColor").AsVector().Set(new Vector3(0.3231373f, 0.3607844f, 0.3937255f));

            effect.GetVariableByName("FogColor").AsVector().Set(FogColor);
            effect.GetVariableByName("FogStart").AsScalar().Set(FogStart);
            effect.GetVariableByName("FogEnd").AsScalar().Set(FogEnd);

            effect.GetVariableByName("EyePosition").AsVector().Set(Matrix.Invert(View).TranslationVector);

            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(context);

            foreach(var p in instanceData)
            {
                if (toAdd[p.Key].Count > 0 || toRemove[p.Key].Count > 0 || toModify[p.Key].Count > 0)
                {
                    p.Value.InstanceBuffer.SetData(p.Value.Instances.ToArray(), 0, p.Value.InstanceCount);

                    toAdd[p.Key].Clear();
                    toModify[p.Key].Clear();
                    toRemove[p.Key].Clear();
                }

                context.InputAssembler.SetVertexBuffers(0, new SharpDX.Direct3D11.VertexBufferBinding[]
                    {
                        new SharpDX.Direct3D11.VertexBufferBinding(p.Key.VertexBuffer,p.Key.VertexBuffer.ElementSize,0),
                        new SharpDX.Direct3D11.VertexBufferBinding(p.Value.InstanceBuffer,p.Value.InstanceBuffer.ElementSize,0),
                    }
                    );
                context.InputAssembler.SetIndexBuffer(p.Key.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                context.DrawIndexedInstanced(p.Key.IndexBuffer.ElementCount, p.Value.InstanceCount, 0, 0, 0);
            }
        }

    }
}
