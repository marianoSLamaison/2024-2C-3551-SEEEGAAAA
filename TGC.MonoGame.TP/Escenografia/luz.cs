using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Escenografia
{
    public class Luz : Escenografia3D, System.IDisposable{
        private GraphicsDevice _graphicsDevice;
        private Matrix _lightWorld = Matrix.Identity;
        private Matrix _lightBoxWorld;
        private Effect _lightEffect;
        protected float rotacionX, rotacionZ;

        public Vector3 Direccion { get; set; }
        public Color Color { get; set; }
        public float Intensidad { get; set; }
        private PrismaRectangularEditable _lightBox;

        public Luz(GraphicsDevice graphicsDevice,Vector3 posicion, Vector3 direccion, Color color, float intensidad)
    {
        _graphicsDevice = graphicsDevice;
        this.posicion = posicion;
        Direccion = direccion;
        Color = color;
        Intensidad = intensidad;
        _lightBox = new PrismaRectangularEditable(_graphicsDevice, new Vector3(500000f,500000f,500000f));
        _lightBox.setPosicion(posicion);
        _lightBox.setDireccion(Direccion);
        rotacionX = direccion.X;
        rotacionZ = direccion.Z;
    }

        public void SetEffect (Effect effect){//Shader para la Luz propiamente.
            this.efecto = effect;
        }


        public Effect getBoxEffect(){
            return _lightBox.GetEffect();
        }

        public void SetLightEffect(Effect effectForLight, Effect effectForBox){
            SetEffect(effectForLight);
            _lightBox.SetEffect(effectForBox);
        }
        public Vector3 getPosition()
        {
            return posicion;
        }

        public override void dibujar(Matrix view, Matrix projection, Color color)
        {
            // Establece los parámetros de transformación en el Effect.
            //efecto.Parameters["World"].SetValue(_lightBox.getWorldMatrix());         
            _lightEffect.Parameters["World"].SetValue(getWorldMatrix());
            _lightEffect.Parameters["View"].SetValue(view);
            _lightEffect.Parameters["Projection"].SetValue(projection);
            _lightEffect.Parameters["lightPosition"].SetValue(posicion);
            // Configuración del color
            _lightEffect.Parameters["Diffuse"]?.SetValue(color.ToVector3());
            //efecto.Parameters["SamplerType+Diffuse"]?.SetValue(color.ToVector4());
            _lightBox.dibujar(view, projection, Color.White);
        }

        public void cargarLuz(string direccionEfectoBox, string direccionEfectoLuz, ContentManager content)
        {
            Effect _lightBoxEffect = content.Load<Effect>(direccionEfectoBox);
            _lightEffect = content.Load<Effect>(direccionEfectoLuz);
            SetLightEffect(_lightEffect, _lightBoxEffect);
            //_lightBox.getWorldMatrix();
            //getWorldMatrix();
            //base.loadModel(direccionModelo, direccionEfecto, content);
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

        public override Matrix getWorldMatrix()
        {   
            return Matrix.CreateRotationZ(rotacionZ) * Matrix.CreateRotationX(rotacionX) * Matrix.CreateTranslation(posicion)* Matrix.CreateScale(10f);
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
