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
        private VertexPositionNormalTexture[] vertices;
        private int[] indices;
        private Texture2D heightMapTexture;
        private Texture2D terrenoTextureDiffuse;
        private Texture2D terrenoTextureNormal;
        private Texture2D terrenoTextureHeight;

        StaticHandle handlerTerreno;

        private int width, height;

        public void SetEffect (Effect effect, ContentManager content){
            this.efecto = effect;
            this.ApplyTexturesToShader(content);
        }

        public void ApplyTexturesToShader(ContentManager content)
        {
            terrenoTextureDiffuse = content.Load<Texture2D>("Models/Terreno/"+"greenTerrainDiffuse_3");

            efecto.Parameters["baseTexture"]?.SetValue(terrenoTextureDiffuse);
            efecto.Parameters["lightPosition"]?.SetValue(new Vector3(7000,3000,2000));

            efecto.Parameters["ambientColor"]?.SetValue(new Vector3(0.4f, 0.4f, 0.2f));
            efecto.Parameters["diffuseColor"]?.SetValue(new Vector3(0.8f, 0.75f, 0.3f));
            efecto.Parameters["specularColor"]?.SetValue(new Vector3(1f, 1f, 1f));

            efecto.Parameters["KAmbient"]?.SetValue(0.2f);
            efecto.Parameters["KDiffuse"]?.SetValue(1.5f);
            efecto.Parameters["KSpecular"]?.SetValue(0.05f);
            efecto.Parameters["shininess"]?.SetValue(32.0f);

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

        public void CrearCollider(BufferPool bufferPool, Simulation _simulacion, ThreadDispatcher ThreadDispatcher, int ancho, int alto){
            //var planeWidth = 500;
            var scale = 35;
            //esto estaba multiplicado por scale antes
            Vector2 terrainPosition = new Vector2(1 - ancho, 1 - alto) * 0.5f;

            var planeMesh = DemoMeshHelper.CreateDeformedPlane(500, 500,
                (int vX, int vY) =>
                {
                    // Alturas basadas en combinaciones de funciones seno y coseno
                    var octave1 = MathF.Sin(vX * 0.05f) * MathF.Cos(vY * 0.05f) * 50;   // Primer octava
                    var octave2 = MathF.Sin(vX * 0.1f) * MathF.Cos(vY * 0.1f) * 10;     // Segunda octava
                    var octave3 = MathF.Sin(vX * 0.02f) * MathF.Cos(vY * 0.02f) * 50;  // Tercera octava

                    
                    //var noise = (float)(random.NextDouble() * 2.0 - 1.0) * 5; // Ruido aleatorio

                    // Sumar diferentes contribuciones para lograr más irregularidad
                    var totalHeight = octave1 + octave2 + octave3;
                    //
                    var vertexPosition = new Vector2(vX * scale, vY * scale) + terrainPosition;

                    // Devolver la posición del vértice con la altura calculada
                    return new Vector3(vertexPosition.X, totalHeight * 2.5f, vertexPosition.Y).ToNumerics();


                }, new Vector3(1, 1, 1).ToNumerics(), bufferPool, ThreadDispatcher);
            
            // Asumimos que el mesh tiene la propiedad Triangles (puntero a triángulos)
            var triangles = planeMesh.Triangles;
            int triangleCount = triangles.Length; // Número de triángulos en el mesh

            // Crear arrays para almacenar los vértices, índices y las normales

            vertices = new VertexPositionNormalTexture[triangleCount * 3];
            indices = new int[triangleCount * 3];
            Vector3[] vertexNormals = new Vector3[triangleCount * 3];

            // Recorrer cada triángulo y extraer los vértices
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                Vector3 vertexA = new Vector3(triangle.A.X, triangle.A.Y, triangle.A.Z);
                Vector3 vertexB = new Vector3(triangle.B.X, triangle.B.Y, triangle.B.Z);
                Vector3 vertexC = new Vector3(triangle.C.X, triangle.C.Y, triangle.C.Z);

                // Calcular la normal del triángulo
                Vector3 edge1 = vertexB - vertexA;
                Vector3 edge2 = vertexC - vertexA;
                Vector3 triangleNormal = Vector3.Cross(edge2, edge1);
                triangleNormal.Normalize();                

                // Añadir la normal a los vértices involucrados
                vertexNormals[i * 3] = triangleNormal;
                vertexNormals[i * 3 + 1] = triangleNormal;
                vertexNormals[i * 3 + 2] = triangleNormal;

                // Asignar posiciones y normales a los vértices
                vertices[i * 3] = new VertexPositionNormalTexture(vertexA, Vector3.Zero, new Vector2(vertexA.X, vertexA.Z));   // Placeholder para la normal
                vertices[i * 3 + 1] = new VertexPositionNormalTexture(vertexB, Vector3.Zero, new Vector2(vertexB.X, vertexB.Z)); 
                vertices[i * 3 + 2] = new VertexPositionNormalTexture(vertexC, Vector3.Zero, new Vector2(vertexC.X, vertexC.Z));

                // Índices para los triángulos
                indices[i * 3] = i * 3;
                indices[i * 3 + 1] = i * 3 + 1;
                indices[i * 3 + 2] = i * 3 + 2;
            }

            // Normalizar y asignar las normales a cada vértice
            for (int i = 0; i < vertexNormals.Length; i++)
            {
                vertices[i].Normal = Vector3.Normalize(vertexNormals[i]);
                //vertices[i].Normal = Vector3.One * 0.8f;
            }
            
            AyudanteSimulacion.agregarCuerpoEstatico(_simulacion, new RigidPose(new Vector3(0, 0, 0).ToNumerics()), _simulacion.Shapes.Add(planeMesh));
            //handlerTerreno = _simulacion.Statics.Add(new StaticDescription(new Vector3(0, -15, 0).ToNumerics(), _simulacion.Shapes.Add(planeMesh)));
        }

        /// <summary>
        /// Devuelve la matriz de transformación mundial del terreno.
        /// </summary>
        public override Microsoft.Xna.Framework.Matrix getWorldMatrix()
        {
            return
                Microsoft.Xna.Framework.Matrix.CreateTranslation(0, -15, 0);
        }

        /// <summary>
        /// Sobreescribe el método para dibujar el terreno.
        /// </summary>
        public void dibujar(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection, Vector3 posicionCamara, RenderTarget2D shadowMap)
        {
            efecto.CurrentTechnique = efecto.Techniques["TerrenoTechnique"];

            efecto.Parameters["shadowMap"]?.SetValue(shadowMap);
            efecto.Parameters["shadowMapSize"]?.SetValue(Vector2.One * 4096);

            efecto.Parameters["View"].SetValue(view);
            efecto.Parameters["Projection"].SetValue(projection);
            efecto.Parameters["CameraPosition"]?.SetValue(posicionCamara);

            efecto.Parameters["World"].SetValue(getWorldMatrix());
            efecto.Parameters["InverseTransposeWorld"]?.SetValue(Microsoft.Xna.Framework.Matrix.Transpose(Microsoft.Xna.Framework.Matrix.Invert(getWorldMatrix())));

            foreach (var pass in efecto.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice device = efecto.GraphicsDevice;
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
            }
        }

        public void dibujarSombras(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection){
            efecto.CurrentTechnique = efecto.Techniques["DepthPass"];

            efecto.Parameters["View"].SetValue(view);
            efecto.Parameters["Projection"].SetValue(projection);

            efecto.Parameters["LightViewProjection"]?.SetValue(view * projection);

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