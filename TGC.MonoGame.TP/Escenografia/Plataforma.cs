using System;
using System.Collections.Generic;
using BepuUtilities.Memory;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using Control;

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
        public void loadModel(Model modelo, Effect efecto) 
        {
            //base.loadModel(direcionModelo, direccionEfecto, contManager);
            this.modelo = modelo;
            this.efecto = efecto;
            foreach ( ModelMesh mesh in modelo.Meshes )
            {
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                }
            }
        }
        public override void loadModel(String a, String b, ContentManager c)
        {
            //Deprecado pero retirarlo tomaria mas tiempo del que tengo
        }
        private void getModelData(out List<Vector3> vertices, out List<int> indices)
        {
            vertices = new List<Vector3>();
            indices = new List<int>();

            foreach (ModelMesh mesh in modelo.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    VertexBuffer vertexBuffer = part.VertexBuffer;
                    IndexBuffer indexBuffer = part.IndexBuffer;

                    VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[vertexBuffer.VertexCount];
                    vertexBuffer.GetData(vertexData);

                    foreach (var vertex in vertexData)
                    {
                        vertices.Add(vertex.Position);
                    }

                    // Read indices as 16-bit values
                    ushort[] indexData = new ushort[indexBuffer.IndexCount];
                    indexBuffer.GetData(indexData);

                    for (int i = 0; i < indexData.Length; i++)
                    {
                        indices.Add(indexData[i]);
                    }
                }
            }
        }


//Por algun motivo no funciona _/\(°_°)/\_ No tengo idea por que
//en teoria deberia funcionar pero no lo hace
//ya veo que sera algo como con el dibujado de poligonos, 
//pero ahora no tengo tiempo para eso a si que lo dejare aqui
        public void CrearColliderExp(BufferPool bufferPool, Simulation simulacion)
        {
            // Load the data needed to obtain the shape
            List<Vector3> vertices;
            List<int> indices;
            getModelData(out vertices, out indices);

            // Calculate the number of triangles
            int triangleCount = indices.Count / 3;

            // Allocate a buffer for the triangles using the buffer pool
            bufferPool.Take<Triangle>(triangleCount, out var triangleBuffer);
            Triangle t;
            // Fill the buffer with triangle data
            for (int i = 0; i < triangleBuffer.Length; i++)
            {
                int index0 = indices[i * 3];
                int index1 = indices[i * 3 + 1];
                int index2 = indices[i * 3 + 2];
                
                t = new Triangle(vertices[index0].ToNumerics(), vertices[index1].ToNumerics(), vertices[index2].ToNumerics());
                triangleBuffer[i] = t;
            }

            // Create the mesh and add it to the simulation
            Mesh mesh = new Mesh(triangleBuffer, Vector3.One.ToNumerics(), bufferPool);
            var shapeIndex = simulacion.Shapes.Add(mesh);

            // Return the buffer to the pool after use
            bufferPool.Return(ref triangleBuffer);

            // For a static body
            var staticDescription = new StaticDescription(
                posicion.ToNumerics(),  // Position of the static body
                Quaternion.Identity.ToNumerics(),     // Orientation of the static body
                shapeIndex              // Shape index
            );
            Console.WriteLine($"Collider Position: {posicion}");
            Console.WriteLine($"Shape Index: {shapeIndex}");

            // Debugging information
            var staticBody = simulacion.Statics.GetStaticReference(simulacion.Statics.Add(staticDescription));
            Console.WriteLine($"Static Body Position: {staticBody.Pose.Position}");
            Console.WriteLine($"Static Body Orientation: {staticBody.Pose.Orientation}");
        }


        public void CrearCollider(BufferPool bufferPool, Simulation simulacion)
        {
            VertexBuffer buffer;
            // Definir las formas de cada sección
            var plataformaPrincipal = new Box(3000 * 1.75f, 500, 3000 * 1.75f);
            var plataformaPrincipalPose = new RigidPose(new System.Numerics.Vector3 (posicion.X, 250, posicion.Z), 
                Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(rotacionY)).ToNumerics());

            var posicionRampaDerecha = Vector3.Zero;
            var posicionRampaInferior = Vector3.Zero;
            var orientacionRampaDerecha = Quaternion.Identity;
            var orientacionRampaInferior = Quaternion.Identity;

            var posicionBaranda1 = Vector3.Zero;
            var posicionBaranda2 = Vector3.Zero;
            var posicionBaranda3 = Vector3.Zero;
            var posicionBaranda4 = Vector3.Zero;

            

            

            switch (rotacionY){
                case 0:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, -2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, MathF.PI / 6.85f, 0);

                    posicionBaranda1 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2450, 300, 1700);
                    posicionBaranda2 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2450, 300, -1600);
                    posicionBaranda3 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(1600, 300, -2450);
                    posicionBaranda4 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-1700, 300, -2450);

                    break;
                case MathF.PI:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, 2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, -MathF.PI / 6.85f, 0);

                    posicionBaranda1 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2450, 300, -1700);
                    posicionBaranda2 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2450, 300, 1600);
                    posicionBaranda3 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-1600, 300, 2450);
                    posicionBaranda4 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(1700, 300, 2450);
                    break;
                case MathF.PI/2:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, -2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, -MathF.PI / 6.85f, 0);

                    posicionBaranda1 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2450, 300, 1700);
                    posicionBaranda2 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2450, 300, -1600);
                    posicionBaranda3 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-1600, 300, -2450);
                    posicionBaranda4 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(1700, 300, -2450);
                    break;
                case 3*MathF.PI/2:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, 2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, MathF.PI / 6.85f, 0);

                    posicionBaranda1 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2450, 300, -1700);
                    posicionBaranda2 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2450, 300, 1600);
                    posicionBaranda3 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(1600, 300, 2450);
                    posicionBaranda4 = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-1700, 300, 2450);
                    break;
                default:
                    break;
            }
            var rampa = new Box(750* 1.75f, 500, 2000* 1.75f);
            var rampaDerechaPose = new RigidPose(
                posicionRampaDerecha.ToNumerics(),
                orientacionRampaDerecha.ToNumerics()
            );
            var rampaInferiorPose = new RigidPose(
                posicionRampaInferior.ToNumerics(),
                orientacionRampaInferior.ToNumerics()
            );

            // Agregar cada colisionador de forma independiente a la simulación
            simulacion.Statics.Add(new StaticDescription(
                plataformaPrincipalPose.Position,
                plataformaPrincipalPose.Orientation,
                simulacion.Shapes.Add(plataformaPrincipal)
            ));

            simulacion.Statics.Add(new StaticDescription(
                rampaDerechaPose.Position,
                rampaDerechaPose.Orientation,
                simulacion.Shapes.Add(rampa)
            ));

            simulacion.Statics.Add(new StaticDescription(
                rampaInferiorPose.Position,
                rampaInferiorPose.Orientation,
                simulacion.Shapes.Add(rampa)
            ));

            simulacion.Statics.Add(new StaticDescription(
                posicionBaranda1.ToNumerics(),
                System.Numerics.Quaternion.Identity,
                simulacion.Shapes.Add(new Box(340, 340, 2100))
            ));
            simulacion.Statics.Add(new StaticDescription(
                posicionBaranda2.ToNumerics(),
                System.Numerics.Quaternion.Identity,
                simulacion.Shapes.Add(new Box(340, 340, 1900))
            ));
            simulacion.Statics.Add(new StaticDescription(
                posicionBaranda3.ToNumerics(),
                System.Numerics.Quaternion.Identity,
                simulacion.Shapes.Add(new Box(1900, 340, 340))
            ));
            simulacion.Statics.Add(new StaticDescription(
                posicionBaranda4.ToNumerics(),
                System.Numerics.Quaternion.Identity,
                simulacion.Shapes.Add(new Box(2100, 340, 340))
            ));

        }


        public void dibujarPlataforma(Matrix view, Matrix projection, Color color)
        {
            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);
            // le pasamos el color ( repasar esto )
            efecto.Parameters["DiffuseColor"]?.SetValue(color.ToVector3());
            foreach( ModelMesh mesh in modelo.Meshes)
            {
                efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform 
                * Matrix.CreateScale(g_scale) 
                * Matrix.CreateRotationY(rotacionY)
                //* Matrix.CreateFromQuaternion(refACollider.Pose.Orientation)
                //* Matrix.CreateTranslation(refACollider.Pose.Position));
                * Matrix.CreateTranslation(posicion));
                mesh.Draw();
            }

        }
       
    }
    
}