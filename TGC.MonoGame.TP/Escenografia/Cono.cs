using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Control;
using System.Linq;

namespace Escenografia
{
    public class Cono : Escenografia3D
    {
        float scale;
        public BodyReference refACuerpo;

        public Cono(Vector3 posicion){
            this.posicion = posicion;
        }
        public override Matrix getWorldMatrix()
        {
           //Console.WriteLine("Cono:" + refACuerpo.Pose.Position);
           
           return Matrix.CreateScale(scale)
                * Matrix.CreateTranslation(0, -70.5f, 0)
                * Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation) 
                * Matrix.CreateTranslation(refACuerpo.Pose.Position);
           
        }
        public void SetScale(float scale)
        {
            this.scale = scale;
        }
        public override void loadModel(string direcionModelo, string direccionEfecto, ContentManager contManager)
        {
            base.loadModel(direcionModelo, direccionEfecto, contManager);


            efecto.Parameters["lightPosition"]?.SetValue(new Vector3(7000,3000,2000));

            efecto.Parameters["ambientColor"]?.SetValue(new Vector3(0.5f, 0.2f, 0.15f));
            efecto.Parameters["diffuseColor"]?.SetValue(new Vector3(0.9f, 0.7f, 0.3f));
            efecto.Parameters["specularColor"]?.SetValue(new Vector3(1f, 1f, 1f));

            efecto.Parameters["KAmbient"]?.SetValue(0.4f);
            efecto.Parameters["KDiffuse"]?.SetValue(1.5f);
            efecto.Parameters["KSpecular"]?.SetValue(0.25f);
            efecto.Parameters["shininess"]?.SetValue(32.0f);

            foreach ( ModelMesh mesh in modelo.Meshes )
            {
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                }
            }
        }

        public void CrearCollider(BufferPool bufferPool, Simulation simulation, Vector3 posicion)
        {
            // Crear un colisionador para el cono.
            var conoCollider = new Box(140f, 150f, 140f);
            
            var figuraCono = simulation.Shapes.Add(conoCollider);

            // Agregar el colisionador a la simulación.

            BodyInertia conoInertia = conoCollider.ComputeInertia(0.5f);

            this.posicion = posicion;
            BodyHandle handler = simulation.Bodies.Add(BodyDescription.CreateDynamic(posicion.ToNumerics(), conoInertia, simulation.Shapes.Add(conoCollider), 0.01f));
            refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);

        }
        public void dibujar(Matrix view, Matrix projection, Color color, Vector3 posicionCamara)
        {
            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);
            // le pasamos el color ( repasar esto )
            efecto.Parameters["DiffuseColor"]?.SetValue(color.ToVector3());
            efecto.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(getWorldMatrix())));
            efecto.Parameters["CameraPosition"]?.SetValue(posicionCamara);


            foreach( ModelMesh mesh in modelo.Meshes)
            {
                efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * getWorldMatrix());
                mesh.Draw();
            }
        }

    }
    
}