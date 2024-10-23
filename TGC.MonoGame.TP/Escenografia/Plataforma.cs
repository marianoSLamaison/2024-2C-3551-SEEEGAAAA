using System;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using BepuUtilities.Memory;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using Control;
using System.Runtime.CompilerServices;

namespace Escenografia
{
    class Plataforma : Escenografia3D
    {
        private Texture2D texture;
        static private float g_scale;
        public Plataforma(float rotacionY, Vector3 posicion)
        {
            this.rotacionY = rotacionY;
            this.posicion = posicion;
        }

        public void SetTexture(Texture2D unaTextura){
            this.texture = unaTextura;
        }
        public override Matrix getWorldMatrix()
        {
            return Matrix.CreateScale(g_scale) * Matrix.CreateRotationY(rotacionY) * Matrix.CreateTranslation(posicion);
        }

        public static void setGScale(float scale)
        {
            g_scale = scale;
        }
        public override void loadModel(string direcionModelo, string direccionEfecto, ContentManager contManager) 
        {
            base.loadModel(direcionModelo, direccionEfecto, contManager);
            foreach ( ModelMesh mesh in modelo.Meshes )
            {
                
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                    //Console.WriteLine(meshPart);
                }
            }
        }
        public void CrearCollider(BufferPool bufferPool, Simulation simulacion){

            var compoundBuilder = new CompoundBuilder(bufferPool, simulacion.Shapes, 7);

            foreach ( ModelMesh mesh in modelo.Meshes )
            {
                Console.WriteLine(mesh.Name);
                var meshCollider = new Mesh();
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    if(meshPart.VertexBuffer.VertexCount > 4){throw new Exception();}
                    int[] indices = new int[meshPart.IndexBuffer.IndexCount];

                    meshPart.IndexBuffer.GetData(indices);

                    var VertexBuffer = new VertexPosition[meshPart.VertexBuffer.VertexCount];

                    meshPart.VertexBuffer.GetData(VertexBuffer);

                    meshCollider = new Mesh(CrearBufferDeTriangulos(bufferPool, VertexBuffer, indices),
                        Vector3.One.ToNumerics() * 15f,
                        bufferPool);
                }
                var meshShape = simulacion.Shapes.Add(meshCollider);

                BodyInertia bodyInertia = new Box().ComputeInertia(1);

                compoundBuilder.AddForKinematic(meshShape, new RigidPose(mesh.ParentBone.ModelTransform.Translation.ToNumerics()),1f);
            }

            compoundBuilder.BuildKinematicCompound(out var compoundChildren, out var _);

            compoundBuilder.Reset();

            var handler = simulacion.Statics.Add(new StaticDescription(getWorldMatrix().Translation.ToNumerics(), simulacion.Shapes.Add(new Compound(compoundChildren))));

            var staticReference = simulacion.Statics.GetStaticReference(handler);
            posicion = staticReference.Pose.Position;

            Console.WriteLine(staticReference.Pose.Position);
            //simulacion.Bodies.Add(BodyDescription.CreateStatic(compoundCenter, new BodyVelocity(System.Numerics.Vector3.Zero), simulacion.Shapes.Add(new Compound(compoundChildren)), 1f));
        }

        public Buffer<Triangle> CrearBufferDeTriangulos(BufferPool bufferPool, VertexPosition[] vertices, int[] indices){
            
            if (indices.Length % 3 != 0) throw new Exception();

            bufferPool.Take<Triangle>(indices.Length / 3, out var triangulos);
            // Crear triángulos a partir de los índices
            for (int i = 0; i < indices.Length; i += 3)
            {
                // Obtener los índices de los vértices
                int index0 = indices[i]%vertices.Length < 0 ? indices[i]%vertices.Length     + vertices.Length: indices[i]%vertices.Length;
                int index1 = indices[i+1]%vertices.Length < 0 ? indices[i+1]%vertices.Length + vertices.Length: indices[i+1]%vertices.Length;
                int index2 = indices[i+2]%vertices.Length < 0 ? indices[i+2]%vertices.Length + vertices.Length: indices[i+2]%vertices.Length;

                // Crear un triángulo usando los vértices correspondientes
                Triangle triangle = new Triangle(
                    new System.Numerics.Vector3(vertices[index0].Position.X, vertices[index0].Position.Y, vertices[index0].Position.Z),
                    new System.Numerics.Vector3(vertices[index1].Position.X, vertices[index1].Position.Y, vertices[index1].Position.Z),
                    new System.Numerics.Vector3(vertices[index2].Position.X, vertices[index2].Position.Y, vertices[index2].Position.Z)
                );

                // Agregar el triángulo al buffer
                triangulos[i / 3] = triangle;
            }

            return triangulos;
        }
    }
    
}