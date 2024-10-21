using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using Control;
using Escenografia;
using BepuPhysics.Collidables;
using System;
using BepuUtilities.Memory;
using BepuUtilities;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace Escenografia
{
    public class Terreno : Escenografia3D
    {
        private VertexPosition[] vertices;
        private int[] indices;
        private Texture2D heightMapTexture;
        private Texture2D terrenoTextureDiffuse;
        private Texture2D terrenoTextureNormal;
        private Texture2D terrenoTextureHeight;

        StaticHandle handlerTerreno;

        private float[,] heightData;
        private int width, height;

        /// <summary>
        /// Constructor para inicializar el terreno con un heightmap.
        /// </summary>
        /// <param name="heightMapPath">La ruta de la imagen del heightmap.</param>
        /// <param name="content">El ContentManager del juego.</param>
        /// <param name="alturaMaxima">Altura máxima del terreno basado en el heightmap.</param>
        /// 
        public void CargarTerreno(string heightMapPath, ContentManager content, float alturaMaxima)
        {
            // Cargar el heightmap como textura
            heightMapTexture = content.Load<Texture2D>(heightMapPath);
            terrenoTextureDiffuse = content.Load<Texture2D>("Models/Terreno/"+"OrangeRockTexture");
            terrenoTextureHeight = content.Load<Texture2D>("Models/Terreno/"+"OrangeRockTexture");
            terrenoTextureNormal = content.Load<Texture2D>("Models/Terreno/"+"normal");
            width = heightMapTexture.Width;
            height = heightMapTexture.Height;

            // Extraer datos de altura del heightmap
            Color[] heightMapColors = new Color[width * height];
            heightMapTexture.GetData(heightMapColors);

            heightData = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightData[x, y] = heightMapColors[x + y * width].R / 255.0f * alturaMaxima;
                }
            }

            // Suavizar los datos de altura
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // Promediar la altura con los píxeles vecinos
                    heightData[x, y] = (heightData[x, y] + 
                                        heightData[x - 1, y] + heightData[x + 1, y] + 
                                        heightData[x, y - 1] + heightData[x, y + 1] + 
                                        heightData[x + 1, y + 1] + heightData[x - 1, y + 1] +
                                        heightData[x - 1, y - 1] + heightData[x + 1, y - 1]) / 9.0f;
                }
            }

            // Crear el mesh (malla) del terreno
            GenerarVertices();
            GenerarIndices();
        }

        public void SetEffect (Effect effect){
            this.efecto = effect;
        }

        public void ApplyTexturesToShader()
        {
            efecto.Parameters["TerrenoTexture"].SetValue(heightMapTexture);
        }

        /// <summary>
        /// Generar los vértices del terreno basados en el heightmap.
        /// </summary>
        private void GenerarVertices()
        {
            vertices = new VertexPosition[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 posicion = new Vector3(x, heightData[x, y], y);
                    //Vector2 texCoord = new Vector2((float)x / (width - 1), (float)y / (height - 1));
                    vertices[x + y * width] = new VertexPosition(posicion);
                }
            }

            // Puedes calcular las normales más adelante si es necesario.
        }

        /// <summary>
        /// Generar los índices para los triángulos del terreno.
        /// </summary>
        private void GenerarIndices()
        {
            indices = new int[(width - 1) * (height - 1) * 6]; // 6 índices por cuadrado (2 triángulos por cuadrado)
            int indice = 0;

            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    // Triángulo 1
                    indices[indice++] = x + y * width;
                    indices[indice++] = (x + 1) + y * width;
                    indices[indice++] = x + (y + 1) * width;

                    // Triángulo 2
                    indices[indice++] = (x + 1) + y * width;
                    indices[indice++] = (x + 1) + (y + 1) * width;
                    indices[indice++] = x + (y + 1) * width;
                }
            }
        }

        public void CrearCollider(BufferPool bufferPool, Simulation simulation, Vector3 posicion)
        {
            // Extraer solo las posiciones de los vértices para el colisionador.
            var posiciones = new System.Numerics.Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                posiciones[i] = new System.Numerics.Vector3(vertices[i].Position.X, vertices[i].Position.Y, vertices[i].Position.Z);
            }

            var terrenoCollider = new Mesh(CrearBufferDeTriangulos(bufferPool), Vector3.One.ToNumerics() * 40f, bufferPool);
            var figuraTerreno = simulation.Shapes.Add(terrenoCollider);

            // Agregar el colisionador a la simulación.
            //le damos una posicion 
            this.posicion = posicion;
            AyudanteSimulacion.agregarCuerpoEstatico(simulation, new RigidPose(posicion.ToNumerics()), figuraTerreno);
        }

        public void CrearCollider(BufferPool bufferPool, Simulation _simulacion, ThreadDispatcher ThreadDispatcher){
            var planeWidth = 500;
            var scale = 35;
            Vector2 terrainPosition = new Vector2(1 - planeWidth, 1 - planeWidth) * scale * 0.5f;

            var planeMesh = DemoMeshHelper.CreateDeformedPlane(planeWidth, planeWidth,
                (int vX, int vY) =>
                {
                    // Alturas basadas en combinaciones de funciones seno y coseno
                    var octave1 = MathF.Sin(vX * 0.05f) * MathF.Cos(vY * 0.05f) * 50;   // Primer octava
                    var octave2 = MathF.Sin(vX * 0.1f) * MathF.Cos(vY * 0.1f) * 10;     // Segunda octava
                    var octave3 = MathF.Sin(vX * 0.02f) * MathF.Cos(vY * 0.02f) * 50;  // Tercera octava

                    
                    //var noise = (float)(random.NextDouble() * 2.0 - 1.0) * 5; // Ruido aleatorio

                    // Sumar diferentes contribuciones para lograr más irregularidad
                    var totalHeight = octave1 + octave2 + octave3;

                    var vertexPosition = new Vector2(vX * scale, vY * scale) + terrainPosition;

                    // Devolver la posición del vértice con la altura calculada
                    return new Vector3(vertexPosition.X, totalHeight * 2.5f, vertexPosition.Y).ToNumerics();


                }, new Vector3(1, 1, 1).ToNumerics(), bufferPool, ThreadDispatcher);
            
            // Asumimos que el mesh tiene la propiedad Triangles (puntero a triángulos)
            var triangles = planeMesh.Triangles;
            int triangleCount = triangles.Length; // Número de triángulos en el mesh

             // Crear arrays para almacenar los vértices e índices
            vertices = new VertexPosition[triangleCount * 3];
            indices = new int[triangleCount * 3];

            // Recorrer cada triángulo y extraer los vértices
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];

                // Cada triángulo tiene tres vértices: A, B, C
                vertices[i * 3] = new VertexPosition(new Vector3(triangle.A.X, triangle.A.Y, triangle.A.Z));
                vertices[i * 3 + 1] = new VertexPosition(new Vector3(triangle.B.X, triangle.B.Y, triangle.B.Z));
                vertices[i * 3 + 2] = new VertexPosition(new Vector3(triangle.C.X, triangle.C.Y, triangle.C.Z));

                // Los índices apuntan a los tres vértices de cada triángulo
                indices[i * 3] = i * 3;
                indices[i * 3 + 1] = i * 3 + 1;
                indices[i * 3 + 2] = i * 3 + 2;
            }
                    
            
            AyudanteSimulacion.agregarCuerpoEstatico(_simulacion, new RigidPose(new Vector3(0, -15, 0).ToNumerics()), _simulacion.Shapes.Add(planeMesh));
            //handlerTerreno = _simulacion.Statics.Add(new StaticDescription(new Vector3(0, -15, 0).ToNumerics(), _simulacion.Shapes.Add(planeMesh)));
        }

        public Buffer<Triangle> CrearBufferDeTriangulos(BufferPool bufferPool)
        {
            // Crear un buffer para almacenar los triángulos, el tamaño es la cantidad de triángulos
            bufferPool.Take<Triangle>(indices.Length / 3, out var triangulos);

            // Crear triángulos a partir de los índices
            for (int i = 0; i < indices.Length; i += 3)
            {
                // Obtener los índices de los vértices
                int index0 = indices[i];
                int index1 = indices[i + 1];
                int index2 = indices[i + 2];

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

        /// <summary>
        /// Devuelve la matriz de transformación mundial del terreno.
        /// </summary>
        public override Microsoft.Xna.Framework.Matrix getWorldMatrix()
        {
            return
                //Microsoft.Xna.Framework.Matrix.CreateRotationY(rotacionY) *
                //Microsoft.Xna.Framework.Matrix.CreateRotationZ(rotacionZ) * 
                //Microsoft.Xna.Framework.Matrix.CreateScale(40f) *
                //Microsoft.Xna.Framework.Matrix.CreateTranslation(posicion);
                Microsoft.Xna.Framework.Matrix.CreateTranslation(0, -15, 0);
        }

        /// <summary>
        /// Sobreescribe el método para dibujar el terreno.
        /// </summary>
        public override void dibujar(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection, Color color)
        {
            efecto.Parameters["View"].SetValue(view);
            efecto.Parameters["Projection"].SetValue(projection);
            efecto.Parameters["DiffuseColor"]?.SetValue(color.ToVector3());
            efecto.Parameters["SamplerType+diffuse"]?.SetValue(terrenoTextureDiffuse);

            efecto.Parameters["World"].SetValue(getWorldMatrix());

            foreach (var pass in efecto.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice device = efecto.GraphicsDevice;
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
            }
        }
    }
}