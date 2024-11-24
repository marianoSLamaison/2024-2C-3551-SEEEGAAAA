using BepuPhysics;
using BepuPhysics.Collidables;
using Control;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Escenografia
{
    public abstract class Escenografia3D 
    {
        public Model modelo; 
        protected Effect efecto;

        public Vector3 posicion;
        protected float rotacionX, rotacionY, rotacionZ;
        /// <summary>
        /// Usado para obtener la matriz mundo de cada objeto
        /// </summary>
        /// <returns>La matriz "world" asociada al objeto que llamo</returns>
        abstract public Matrix getWorldMatrix();
        /// <summary>
        /// Inicializa un modelo junto a sus efectos dado una direccion de archivo para este
        /// </summary>
        /// <param name="direcionModelo"> Direccion en el sistema de archivos para el modelo</param>
        /// <param name="direccionEfecto"> Direccion en el sistema de archivos para el efecto</param>
        /// <param name="contManager"> Content Manager del juego </param>
        /// <remarks> 
        /// Este metodo es virtual, para permitir sobre escribirlo, en caso de que
        /// necesitemos que algun modelo tenga diferentes efectos por mesh
        /// </remarks>
        virtual public void loadModel(String direcionModelo,
                        String direccionEfecto, ContentManager contManager)
        {
            //asignamos el modelo deseado
            modelo = contManager.Load<Model>(direcionModelo);
            //mismo caso para el efecto
            efecto = contManager.Load<Effect>(direccionEfecto);
            //agregamos el efecto deseado a cada parte del modelo
            //por ahora cada modelo, carga una misma textura para todo el modelo
            //luego podemos re escribir esto para hacerlo de otra forma
            //podria mover esta parte a los hijos de la clase y solo dejar la carga generica
            //esto sera aplicado por cada clase hija
            /*
            foreach ( ModelMesh mesh in modelo.Meshes )
            {
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                }
            }
            */
        }
        /// <summary>+
        /// Funcion para dibujar los modelos
        /// </summary>
        /// <param name="view">la matriz de la camara</param>
        /// <param name="projection">la matriz que define el como se projecta sobre la camara</param>
        /// <param name="color">el color que queremos que tenga el modelo de base</param>
        virtual public void dibujar(Matrix view, Matrix projection, Color color)
        {
            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);
            // le pasamos el color ( repasar esto )
            efecto.Parameters["DiffuseColor"].SetValue(color.ToVector3());
            foreach( ModelMesh mesh in modelo.Meshes)
            {
                efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * getWorldMatrix());
                mesh.Draw();
            }
        }
        public void LlenarGbuffer(Matrix view, Matrix Proj, Matrix lightViewProj)
        {
            //aclaramos la tecnica a usar
            efecto.CurrentTechnique = efecto.Techniques["DeferredShading"];
            //cargamos las matrices para esto
            MonoHelper.loadShaderMatrices(efecto, getWorldMatrix(), view, Proj, lightViewProj);
            //cargams las texturas (si las hubiera)
            MonoHelper.loadShaderTextures(efecto, null, null, null, null);
            foreach(ModelMesh mesh in modelo.Meshes)
            {
                efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                                                    getWorldMatrix());
                mesh.Draw();
            }
        }

        public void LlenarEfectsBuffer(Microsoft.Xna.Framework.Matrix view,
                                            Microsoft.Xna.Framework.Matrix Proj,
                                            Microsoft.Xna.Framework.Matrix lightViewProj)
        {
            efecto.CurrentTechnique = efecto.Techniques["EffectsPass"];
            MonoHelper.loadShaderMatrices(efecto, getWorldMatrix(),
            view,
            Proj,
            lightViewProj);
            foreach(ModelMesh mesh in modelo.Meshes)
            {
                efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                                                    getWorldMatrix());
                mesh.Draw();
            }
        }
    }
    /// <summary>
    /// Esta es la clase que te permite generar las figuras primitivas
    /// triangulos, cuadrados, circulo, una mesh cuadrada, etc.
    /// No tenemos un poligono general, solo poligonos regulares. 
    /// </summary>
    class Primitiva 
    {
        private GraphicsDevice device;
        private short[] indices;
        private Effect effect;
        private VertexPositionColor[] vertices;
        private Color color;
        private int numeroTriangulos;
        private BodyReference cuerpoFisico;
        public BodyHandle handlerCuerpo;
    
        public System.Numerics.Vector3 Posicion {get {return cuerpoFisico.Pose.Position;}}

        public static Primitiva Prisma(Vector3 vMenor, Vector3 vMayor)
        {
            Primitiva ret = new Primitiva();
            
            ret.vertices = new VertexPositionColor[8];

            var vertices = new VertexPositionNormalTexture[8];
            Vector3 [] lVertices = new Vector3[8];
            Vector3 dimensiones = vMayor - vMenor;
            lVertices[0] = vMenor;
            lVertices[1] = new Vector3(vMayor.X, vMenor.Y, vMenor.Z);
            lVertices[2] = new Vector3(vMenor.X, vMayor.Y, vMenor.Z);
            lVertices[3] = new Vector3(vMenor.X, vMenor.Y, vMayor.Z);
            lVertices[4] = new Vector3(vMayor.X, vMayor.Y, vMenor.Z);
            lVertices[5] = new Vector3(vMayor.X, vMenor.Y, vMayor.Z);
            lVertices[6] = new Vector3(vMenor.X, vMayor.Y, vMayor.Z);
            lVertices[7] = vMayor;

            // Vértices del prisma
            ret.vertices[0] = new VertexPositionColor(lVertices[0], Color.DarkTurquoise);
            ret.vertices[1] = new VertexPositionColor(lVertices[1], Color.DarkTurquoise);
            ret.vertices[2] = new VertexPositionColor(lVertices[2], Color.DarkTurquoise);
            ret.vertices[3] = new VertexPositionColor(lVertices[3], Color.DarkTurquoise);
            ret.vertices[4] = new VertexPositionColor(lVertices[4], Color.DarkTurquoise);
            ret.vertices[5] = new VertexPositionColor(lVertices[5], Color.DarkTurquoise);
            ret.vertices[6] = new VertexPositionColor(lVertices[6], Color.DarkTurquoise);
            ret.vertices[7] = new VertexPositionColor(lVertices[7], Color.DarkTurquoise);

        // Definir índices para los triángulos
            ret.indices = new short[]
            {     
                0, 2, 3, 3, 6, 2,// Frente
                1, 5, 4, 4, 7, 5,// Atrás
                3, 5, 6, 6, 7, 5,// Izquierda
                0, 1, 2, 2, 4, 1,// Derecha
                0, 3, 1, 1, 5, 3,// Arriba
                2, 6, 4, 4, 7, 6// Abajo
            };
            ret.numeroTriangulos = 12;

            return ret;
        }

        public static Primitiva Cilindro(float radio, float altura, int segmentos = 16)
        {
            Primitiva ret = new Primitiva();
            
            int verticesPorCapa = segmentos + 1; // Un vértice adicional para cerrar el círculo
            int totalVertices = verticesPorCapa * 2; // Capa superior e inferior

            ret.vertices = new VertexPositionColor[totalVertices];
            ret.indices = new short[segmentos * 12]; // 6 índices por cada segmento para tapa superior, lateral e inferior

            float mitadAltura = altura / 2;

            // Crear vértices
            for (int i = 0; i <= segmentos; i++)
            {
                float theta = MathHelper.TwoPi * i / segmentos;
                float x = radio * (float)Math.Cos(theta);
                float z = radio * (float)Math.Sin(theta);

                // Capa inferior
                ret.vertices[i] = new VertexPositionColor(new Vector3(x, -mitadAltura, z), Color.Blue);
                // Capa superior
                ret.vertices[i + verticesPorCapa] = new VertexPositionColor(new Vector3(x, mitadAltura, z), Color.Red);
            }

            // Crear índices para los triángulos
            int index = 0;
            for (int i = 0; i < segmentos; i++)
            {
                // Lados
                ret.indices[index++] = (short)i;
                ret.indices[index++] = (short)(i + verticesPorCapa);
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa);

                ret.indices[index++] = (short)((i + 1) % verticesPorCapa);
                ret.indices[index++] = (short)(i + verticesPorCapa);
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa + verticesPorCapa);

                // Tapa superior
                ret.indices[index++] = (short)(i + verticesPorCapa);
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa + verticesPorCapa);
                ret.indices[index++] = (short)(verticesPorCapa + segmentos);

                // Tapa inferior
                ret.indices[index++] = (short)i;
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa);
                ret.indices[index++] = (short)segmentos;
            }

            ret.numeroTriangulos = segmentos * 4;

            return ret;
        }
        public static Primitiva Capsula(float radio, float altura, int segmentos = 16, int segmentosEsfera = 8)
        {
            Primitiva ret = new Primitiva();

            int verticesPorCapa = segmentos + 1;
            int verticesPorSemiesfera = (segmentosEsfera + 1) * verticesPorCapa;
            int totalVertices = verticesPorSemiesfera * 2 + verticesPorCapa * 2;

            ret.vertices = new VertexPositionColor[totalVertices];
            ret.indices = new short[segmentos * (12 * (segmentosEsfera + 1))];

            float mitadAltura = altura / 2;

            int verticeIndex = 0;
            
            // Crear vértices de la capa inferior (cilindro)
            for (int i = 0; i <= segmentos; i++)
            {
                float theta = MathHelper.TwoPi * i / segmentos;
                float x = radio * (float)Math.Cos(theta);
                float z = radio * (float)Math.Sin(theta);

                ret.vertices[verticeIndex++] = new VertexPositionColor(new Vector3(x, -mitadAltura, z), Color.Blue);
                ret.vertices[verticeIndex++] = new VertexPositionColor(new Vector3(x, mitadAltura, z), Color.Red);
            }

            // Crear vértices de la semiesfera inferior
            for (int i = 0; i <= segmentosEsfera; i++)
            {
                float phi = MathHelper.PiOver2 * i / segmentosEsfera;
                float y = -mitadAltura - radio * (float)Math.Sin(phi);
                float radioEsfera = radio * (float)Math.Cos(phi);

                for (int j = 0; j <= segmentos; j++)
                {
                    float theta = MathHelper.TwoPi * j / segmentos;
                    float x = radioEsfera * (float)Math.Cos(theta);
                    float z = radioEsfera * (float)Math.Sin(theta);

                    ret.vertices[verticeIndex++] = new VertexPositionColor(new Vector3(x, y, z), Color.CornflowerBlue);
                }
            }

            // Crear vértices de la semiesfera superior
            for (int i = 0; i <= segmentosEsfera; i++)
            {
                float phi = MathHelper.PiOver2 * i / segmentosEsfera;
                float y = mitadAltura + radio * (float)Math.Sin(phi);
                float radioEsfera = radio * (float)Math.Cos(phi);

                for (int j = 0; j <= segmentos; j++)
                {
                    float theta = MathHelper.TwoPi * j / segmentos;
                    float x = radioEsfera * (float)Math.Cos(theta);
                    float z = radioEsfera * (float)Math.Sin(theta);

                    ret.vertices[verticeIndex++] = new VertexPositionColor(new Vector3(x, y, z), Color.CornflowerBlue);
                }
            }

            int index = 0;
            for (int i = 0; i < segmentos; i++)
            {
                // Lados del cilindro
                ret.indices[index++] = (short)i;
                ret.indices[index++] = (short)(i + verticesPorCapa);
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa);

                ret.indices[index++] = (short)((i + 1) % verticesPorCapa);
                ret.indices[index++] = (short)(i + verticesPorCapa);
                ret.indices[index++] = (short)((i + 1) % verticesPorCapa + verticesPorCapa);
            }

            for (int i = 0; i < segmentosEsfera; i++)
            {
                for (int j = 0; j < segmentos; j++)
                {
                    // Triángulos para las semiesferas
                    int baseIndexInferior = verticesPorCapa * 2 + i * verticesPorCapa + j;
                    int baseIndexSuperior = verticesPorCapa * 2 + verticesPorSemiesfera + i * verticesPorCapa + j;

                    ret.indices[index++] = (short)baseIndexInferior;
                    ret.indices[index++] = (short)(baseIndexInferior + 1);
                    ret.indices[index++] = (short)(baseIndexInferior + verticesPorCapa);

                    ret.indices[index++] = (short)(baseIndexInferior + 1);
                    ret.indices[index++] = (short)(baseIndexInferior + verticesPorCapa + 1);
                    ret.indices[index++] = (short)(baseIndexInferior + verticesPorCapa);

                    ret.indices[index++] = (short)baseIndexSuperior;
                    ret.indices[index++] = (short)(baseIndexSuperior + 1);
                    ret.indices[index++] = (short)(baseIndexSuperior + verticesPorCapa);

                    ret.indices[index++] = (short)(baseIndexSuperior + 1);
                    ret.indices[index++] = (short)(baseIndexSuperior + verticesPorCapa + 1);
                    ret.indices[index++] = (short)(baseIndexSuperior + verticesPorCapa);
                }
            }

            ret.numeroTriangulos = index / 3;
            return ret;
        }


        public static Primitiva Triangulo(Vector3 vertice1, Vector3 vertice2, Vector3 vertice3)
        {
            Primitiva ret = new Primitiva();
            ret.vertices = new VertexPositionColor[3];
            ret.vertices[0] = new VertexPositionColor(vertice1, Color.Black);
            ret.vertices[1] = new VertexPositionColor(vertice2, Color.Black);
            ret.vertices[2] = new VertexPositionColor(vertice3, Color.Black);
            ret.indices = new short[] {0, 1, 2};
            ret.numeroTriangulos = 1;
            return ret;
        }
        public static Primitiva Cuad(Vector3 vertice1, Vector3 vertice2, Vector3 vertice3, Vector3 vertice4)
        {
            Primitiva ret = new Primitiva();
            ret.vertices = new VertexPositionColor[4];
            ret.vertices[0] = new VertexPositionColor(vertice1, Color.Black);
            ret.vertices[1] = new VertexPositionColor(vertice2, Color.Black);
            ret.vertices[2] = new VertexPositionColor(vertice3, Color.Black);
            ret.vertices[3] = new VertexPositionColor(vertice4, Color.Black);
            ret.indices = new short[] { 0, 1, 2, 2, 3, 0 };
            ret.numeroTriangulos = 2;
            return ret;
        }
        public static Primitiva RegPoligon(Vector3 centro, int caras, float radio)
        {
            if ( caras < 3 ) throw new Exception("No puedes hacer una figura cerrada con 2 caras rectas.\n");
            float anguloPorCara = Convert.ToSingle(Math.Tau / caras);
            Primitiva ret = new Primitiva();
            int numeroVertices = caras + 1;
            ret.vertices = new VertexPositionColor[numeroVertices];//uno de mas por el centro
            ret.indices = new short[caras * 3];//hay 3 indices por cara ( tienes un triangulo por cara)
            Vector3 vectorDireccion;
            //creamos los vertices del circulo
            ret.vertices[0] = new VertexPositionColor(centro, Color.Black);
            for( int i=1; i < numeroVertices; i++)
            {
                vectorDireccion = Vector3.Transform(Vector3.Forward, Matrix.CreateRotationY(-anguloPorCara * i)) * radio;
                ret.vertices[i] = new VertexPositionColor(vectorDireccion + centro, Color.Black);
            }
            //cargamos los indices
            for (int i=0 ; i < caras; i++)
            {
                ret.indices[i * 3] = 0;
                ret.indices[i * 3 + 1] = (short)(i + 1);
                ret.indices[i * 3 + 2] = (short)(i < (caras - 1) ? i + 2 : 1);
            }
            ret.numeroTriangulos = caras;
            return ret;
        }

        public void loadPrimitiva(GraphicsDevice device, Effect effect, Color color)
        {
            this.device = device;
            this.effect = effect;
            this.color = color;
        }

        public void dibujar(Camarografo camarografo, RigidPose pose)
        {
            effect.Parameters["Projection"].SetValue(camarografo.getProjectionMatrix());
            effect.Parameters["View"].SetValue(camarografo.getViewMatrix());
            effect.Parameters["World"].SetValue(Matrix.CreateFromQuaternion(pose.Orientation) * Matrix.CreateTranslation(pose.Position));
            effect.Parameters["DiffuseColor"].SetValue(color.ToVector3());
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length /3);
            }
        }

        public void dibujar(Camarografo camarografo, Matrix world)
        {
            effect.Parameters["Projection"].SetValue(camarografo.getProjectionMatrix());
            effect.Parameters["View"].SetValue(camarografo.getViewMatrix());
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["DiffuseColor"].SetValue(color.ToVector3());
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length /3);
            }
        }

        public void setearCuerpoPrisma(Vector3 minV, Vector3 maxV, Vector3 posicion)
        {
            Vector3 dims = maxV - minV;
            TypedIndex figura = AyudanteSimulacion.simulacion.Shapes.Add(new BepuPhysics.Collidables.Box(dims.X,dims.Y,dims.Z));
            handlerCuerpo = AyudanteSimulacion.agregarCuerpoDinamico(new RigidPose(posicion.ToNumerics()), 1f, figura, 1f);
            cuerpoFisico = AyudanteSimulacion.simulacion.Bodies.GetBodyReference(handlerCuerpo);
            cuerpoFisico.Activity.SleepThreshold = -1f;
        }
        public void Dispose()
        {

        } 
    }
class FullScreenCuad
{
    private GraphicsDevice device;
    private short[] indices;
    private Effect effect;
    private VertexPositionTexture[] vertices;
    int numeroTriangulos;
    // Guardo aqui todos los render targets intermedios
    // GBuffer
    public RenderTarget2D positions;
    public RenderTarget2D normals;
    public RenderTarget2D albedo;
    public RenderTarget2D especular;
    public RenderTarget2D ShadowMap;

    public FullScreenCuad(GraphicsDevice screen)
    {
        Vector3 vertice1 = new Vector3(-1, -1, 0),
                vertice2 = new Vector3(1, -1, 0),
                vertice3 = new Vector3(1, 1, 0), 
                vertice4 = new Vector3(-1, 1, 0);

        Vector2 esquina1 = new Vector2(0, 1),
                esquina2 = new Vector2(1, 1),
                esquina3 = new Vector2(1, 0),
                esquina4 = new Vector2(0, 0);

        vertices = new VertexPositionTexture[4];
        vertices[0] = new VertexPositionTexture(vertice1, esquina1);
        vertices[1] = new VertexPositionTexture(vertice2, esquina2);
        vertices[2] = new VertexPositionTexture(vertice3, esquina3);
        vertices[3] = new VertexPositionTexture(vertice4, esquina4);

        indices = new short[] { 0, 2, 1, 0, 3, 2 }; // Corrected winding order

        numeroTriangulos = 2;
        
        // Creando todos los targets con las configuraciones básicas
        int height = screen.Viewport.Bounds.Height;
        int width = screen.Viewport.Bounds.Width;

        positions = new RenderTarget2D(screen, width, height, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8);
        normals   = new RenderTarget2D(screen, width, height, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8);
        albedo    = new RenderTarget2D(screen, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        especular = new RenderTarget2D(screen, width, height, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8);
        ShadowMap = new RenderTarget2D(screen, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24);

        device = screen;
    }

    public void LoadTargets(Effect effect)
    {
        // Cargamos los targets en nuestro shader
        effect.Parameters["position"]?.SetValue(positions);
        effect.Parameters["normal"]?.SetValue(normals);
        effect.Parameters["albedo"]?.SetValue(albedo);
        effect.Parameters["especular"]?.SetValue(especular);
        effect.Parameters["shadowMap"]?.SetValue(ShadowMap);
        this.effect = effect;
    }

    private T[] mapLight<T>(List<luzConica> luces, Func<luzConica, T> getValue)
    {
        T[] ret = new T[luces.Count];
        for (int i = 0; i < luces.Count; i++)
            ret[i] = getValue(luces.ElementAt<luzConica>(i));
        return ret;
    }

    public void Dibujar(GraphicsDevice screen, List<luzConica> luces, Matrix view)
    {
        
        effect.CurrentTechnique = effect.Techniques["Lighting"];
        MonoHelper.loadKColorValues(effect, 0.3f, 0.5f, 0.2f);
        MonoHelper.loadShaderLigthColors(effect, Color.Black, Color.White, Color.White);
        effect.Parameters["posicionesLuces"]?.SetValue(mapLight<Vector3>(luces, luz => luz.posicion));
        effect.Parameters["direcciones"]?.SetValue(mapLight<Vector3>(luces, luz => luz.direccion));
        effect.Parameters["projeccionVorde"]?.SetValue(mapLight<float>(luces, luz => luz.porcentajeDeProjeccionVorde));
        effect.Parameters["colores"]?.SetValue(mapLight<Vector3>(luces, luz => luz.color));
        effect.Parameters["numero_luces"]?.SetValue(luces.Count);
        effect.Parameters["View"].SetValue(view);

        effect.CurrentTechnique.Passes[0].Apply();
        screen.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
    }
}

}