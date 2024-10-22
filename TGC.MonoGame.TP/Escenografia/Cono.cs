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
        BodyReference refACuerpo;

        public Cono(Vector3 posicion){
            this.posicion = posicion;
        }
        public override Matrix getWorldMatrix()
        {
           //Console.WriteLine("Cono:" + refACuerpo.Pose.Position);
           
           return Matrix.CreateScale(scale) * Matrix.CreateTranslation(refACuerpo.Pose.Position);
           
        }
        public void SetScale(float scale)
        {
            this.scale = scale;
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

        public void CrearCollider(BufferPool bufferPool, Simulation simulation, Vector3 posicion)
        {
            // Crear un colisionador para el cono.
            //var conoCollider = new Mesh(CrearBufferDeTriangulos(bufferPool), new Vector3(1, 1, 1).ToNumerics() * scale, bufferPool);
            //var conoCollider = new Cylinder(20f,100f);
            var conoCollider = new Box(30f, 40f, 30f);
            
            var figuraCono = simulation.Shapes.Add(conoCollider);

            // Agregar el colisionador a la simulación.

            BodyInertia conoInertia = conoCollider.ComputeInertia(0.1f);

            this.posicion = posicion;
            BodyHandle handler = simulation.Bodies.Add(BodyDescription.CreateDynamic(posicion.ToNumerics(), conoInertia, simulation.Shapes.Add(conoCollider), 0.01f));
            refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);
        }
        public Buffer<Triangle> CrearBufferDeTriangulos(BufferPool bufferPool)
        {
            // Definición de vértices para el cono
            var vertices = new Vector3[]
            {
                new Vector3(0, 1, 0),    // Vértice superior (pico del cono)
                new Vector3(-1, 0, -1),  // Vértice en la base, cuadrante 3
                new Vector3(1, 0, -1),   // Vértice en la base, cuadrante 2
                new Vector3(1, 0, 1),    // Vértice en la base, cuadrante 1
                new Vector3(-1, 0, 1)    // Vértice en la base, cuadrante 4
            };

            // Definir los índices de los triángulos que forman el cono
            var indices = new int[]
            {
                // Triángulos laterales conectando el vértice superior con la base
                0, 1, 2, // Cara frontal
                0, 2, 3, // Cara derecha
                0, 3, 4, // Cara trasera
                0, 4, 1, // Cara izquierda

                // Triángulos de la base
                1, 2, 3, // Base (triángulo 1)
                1, 3, 4  // Base (triángulo 2)
            };

            // Crear buffer para los triángulos
            bufferPool.Take<Triangle>(indices.Length / 3, out var triangulos);

            // Crear triángulos a partir de los índices y agregar al buffer
            for (int i = 0; i < indices.Length; i += 3)
            {
                int index0 = indices[i];
                int index1 = indices[i + 1];
                int index2 = indices[i + 2];

                // Crear triángulo usando los vértices correspondientes
                triangulos[i / 3] = new Triangle(
                    vertices[index0].ToNumerics(),
                    vertices[index1].ToNumerics(),
                    vertices[index2].ToNumerics()
                );
            }

            return triangulos;
        }

    }
    
}