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


        /*public void CrearCollider(BufferPool bufferPool, Simulation simulacion){

            var compoundBuilder = new CompoundBuilder(bufferPool, simulacion.Shapes, 3);

            var plataformaPrincipal = new Box(3000,250,3000);
            var plataformaPrincipalPose = new RigidPose(posicion.ToNumerics() - new System.Numerics.Vector3(0,250,0));
            var rampa = new Box(600,500,2000);
            var rampaDerecha = new RigidPose(plataformaPrincipalPose.Position + new System.Numerics.Vector3(0,-380,-2500),
                            Quaternion.CreateFromYawPitchRoll(0,-MathF.PI/12,0).ToNumerics());
            var rampaInferior = new RigidPose(plataformaPrincipalPose.Position + new System.Numerics.Vector3(2500,-380,0),
                            Quaternion.CreateFromYawPitchRoll(MathF.PI/2,MathF.PI/12,0).ToNumerics());

            compoundBuilder.AddForKinematic(plataformaPrincipal, plataformaPrincipalPose, 10f);
            compoundBuilder.AddForKinematic(rampa, rampaDerecha, 1f);
            compoundBuilder.AddForKinematic(rampa, rampaInferior, 1f);

            compoundBuilder.BuildKinematicCompound(out var children, out var center);
            compoundBuilder.Reset();

            var staticHandle = simulacion.Statics.Add(new StaticDescription(center,
                                    Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(rotacionY)).ToNumerics(), 
                                    simulacion.Shapes.Add(new Compound(children))));
            refACollider = simulacion.Statics.GetStaticReference(staticHandle);

            Console.WriteLine(refACollider.Pose.Position);

        }*/

        public void CrearCollider(BufferPool bufferPool, Simulation simulacion)
        {
            // Definir las formas de cada sección
            var plataformaPrincipal = new Box(2800, 500, 2800);
            var plataformaPrincipalPose = new RigidPose(posicion.ToNumerics() - new System.Numerics.Vector3(0, 400, 0), 
                Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(rotacionY)).ToNumerics());

            var rampa = new Box(600, 500, 2000);
            var rampaDerechaPose = new RigidPose(
                plataformaPrincipalPose.Position + new System.Numerics.Vector3(-200, -400, -2400),
                Quaternion.Multiply(plataformaPrincipalPose.Orientation, Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 12, 0)).ToNumerics()
            );
            var rampaInferiorPose = new RigidPose(
                plataformaPrincipalPose.Position + new System.Numerics.Vector3(2400, -400, 0),
                Quaternion.Multiply(plataformaPrincipalPose.Orientation,Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, MathF.PI / 12, 0)).ToNumerics()
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
                //* Matrix.CreateRotationY(rotacionY)
                * Matrix.CreateFromQuaternion(refACollider.Pose.Orientation)
                * Matrix.CreateTranslation(refACollider.Pose.Position));
                mesh.Draw();
            }

        }
       
    }
    
}