using System;
using Control;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Escenografia
{
    public class Luz : System.IDisposable{
        private GraphicsDevice _graphicsDevice;
        private Matrix _lightWorld = Matrix.Identity;

        private Camara camara;

        public Vector3 lightPosition = new Vector3(7000,3000,2000);
        public Vector3 lightTarget = Vector3.Zero;
        public Matrix lightView;

        public Matrix lightProjection;


        public Luz(GraphicsDevice device){
            camara = new Camara(lightPosition, lightTarget);
            
            var FrontDirection = Vector3.Normalize(lightTarget - lightPosition);
            var RightDirection = Vector3.Normalize(Vector3.Cross(Vector3.Up, FrontDirection));
            var UpDirection = Vector3.Cross(FrontDirection, RightDirection);
            lightView = Matrix.CreateLookAt(lightPosition, lightPosition + FrontDirection, UpDirection);
            
            lightProjection = Matrix.CreatePerspectiveFieldOfView(MathF.PI/4, device.Viewport.AspectRatio, 1, 16000);
        }

        public Matrix getWorldMatrix()
        {   
            return Matrix.CreateTranslation(lightPosition);

        }
        public void BuildView(Vector3 target){
            lightTarget = target;
            var FrontDirection = Vector3.Normalize(target - lightPosition);
            var RightDirection = Vector3.Normalize(Vector3.Cross(Vector3.Up, FrontDirection));
            var UpDirection = Vector3.Cross(FrontDirection, RightDirection);
            lightView = Matrix.CreateLookAt(lightPosition, lightPosition + FrontDirection, UpDirection);
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
