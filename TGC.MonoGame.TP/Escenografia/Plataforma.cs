using System;
using System.Collections.Generic;
using BepuUtilities.Memory;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;

namespace Escenografia
{
    class Plataforma : Escenografia3D
    {
        private Texture2D texture;
        static private float g_scale;
        private StaticReference refACollider;
        private StaticReference refAColliderDerecha;
        private StaticReference refAColliderInferior;
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
                }
            }
        }
        
        public void CrearCollider(BufferPool bufferPool, Simulation simulacion)
        {
            // Definir las formas de cada sección
            var plataformaPrincipal = new Box(3000 * 1.75f, 500, 3000 * 1.75f);
            var plataformaPrincipalPose = new RigidPose(new System.Numerics.Vector3 (posicion.X, 250, posicion.Z), 
                Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(rotacionY)).ToNumerics());

            var posicionRampaDerecha = Vector3.Zero;
            var posicionRampaInferior = Vector3.Zero;
            var orientacionRampaDerecha = Quaternion.Identity;
            var orientacionRampaInferior = Quaternion.Identity;

            switch (rotacionY){
                case 0:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, -2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, MathF.PI / 6.85f, 0);
                    break;
                case MathF.PI:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, 2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, -MathF.PI / 6.85f, 0);
                    break;
                case MathF.PI/2:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, -2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(-2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, -MathF.PI / 6.85f, 0);
                    break;
                case 3*MathF.PI/2:
                    posicionRampaDerecha = plataformaPrincipalPose.Position + new System.Numerics.Vector3(0, -428f * 1.75f, 2332f * 1.75f);
                    posicionRampaInferior = plataformaPrincipalPose.Position + new System.Numerics.Vector3(2332 * 1.75f, -428f * 1.75f, 0);
                    orientacionRampaDerecha = Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 6.85f, 0);
                    orientacionRampaInferior = Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, MathF.PI / 6.85f, 0);
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
            var plataformaHandle = simulacion.Statics.Add(new StaticDescription(
                plataformaPrincipalPose.Position,
                plataformaPrincipalPose.Orientation,
                simulacion.Shapes.Add(plataformaPrincipal)
            ));

            var rampaDerechaHandle = simulacion.Statics.Add(new StaticDescription(
                rampaDerechaPose.Position,
                rampaDerechaPose.Orientation,
                simulacion.Shapes.Add(rampa)
            ));

            var rampaInferiorHandle = simulacion.Statics.Add(new StaticDescription(
                rampaInferiorPose.Position,
                rampaInferiorPose.Orientation,
                simulacion.Shapes.Add(rampa)
            ));

            // Guardar referencias a cada colisionador, si necesitas acceder a ellos más tarde
            refACollider = simulacion.Statics.GetStaticReference(plataformaHandle);
            refAColliderDerecha = simulacion.Statics.GetStaticReference(rampaDerechaHandle);
            refAColliderInferior = simulacion.Statics.GetStaticReference(rampaInferiorHandle);

            // Imprimir las posiciones de cada colisionador para debug
            Console.WriteLine("Plataforma: " + refACollider.Pose.Position);
            Console.WriteLine("Rampa Derecha: " + refAColliderDerecha.Pose.Position);
            Console.WriteLine("Rampa Inferior: " + refAColliderInferior.Pose.Position);
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