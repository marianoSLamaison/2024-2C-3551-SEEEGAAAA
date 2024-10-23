using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Escenografia
{
    public class Luz : Escenografia3D, System.IDisposable{
        private GraphicsDevice _graphicsDevice;
        private Matrix _lightWorld = Matrix.Identity;
        private Matrix _modelWorld;

        public Vector3 Direccion { get; set; }
        public Color Color { get; set; }
        public float Intensidad { get; set; }
        private PrismaRectangularEditable _lightBox;

        private Texture2D luzTextureDiffuse;


        public Luz(GraphicsDevice graphicsDevice,Vector3 posicion, Vector3 direccion, Color color, float intensidad)
    {
        this._graphicsDevice = graphicsDevice;
        this.posicion = posicion;
        this.Direccion = direccion;
        this.Color = color;
        this.Intensidad = intensidad;
    }

        public void SetEffect (Effect effect){
            this.efecto = effect;
        }


        public override void dibujar(Matrix view, Matrix projection, Color color)
        {
            // Establece los parámetros de transformación en el Effect.
            //efecto.Parameters["World"].SetValue(getWorldMatrix());
            efecto.Parameters["View"].SetValue(view);
            efecto.Parameters["Projection"].SetValue(projection);

            // Configuración del color
            efecto.Parameters["DiffuseColor"]?.SetValue(color.ToVector3());
            //efecto.Parameters["SamplerType+Diffuse"]?.SetValue(color.ToVector4());

        }

        public override void loadModel(string direccionModelo, string direccionEfecto, ContentManager content)
        {
            _lightBox = new PrismaRectangularEditable(_graphicsDevice, new Vector3(10f, 10f, 10f));           
            _modelWorld = Matrix.CreateTranslation(0f, 3f, 3f);
            efecto = content.Load<Effect>(direccionEfecto);
            luzTextureDiffuse = content.Load<Texture2D>("Models/Terreno/"+"diffuseColor");
            base.loadModel(direccionModelo, direccionEfecto, content);
            foreach ( ModelMesh mesh in modelo.Meshes )
                {
                    foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                    {
                        meshPart.Effect = efecto;
                    }
                }
        }
        public override Matrix getWorldMatrix()
        {   
            return Matrix.CreateTranslation(posicion) * Matrix.CreateScale(100f);

        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
