using System;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Control
{
    public class Camarografo
    {
        public Control.Camara camaraAsociada;
        private Matrix projeccion;
        private Vector2 FontPos;
        private SpriteFont Font;
        private bool deboDibujarDatos;
        private bool teclaOprimida;

        public BoundingFrustum frustum;

        public Camarografo(Vector3 posicion, Vector3 puntoDeFoco, float AspectRatio, float minVista, float maxVista)
        {
            camaraAsociada = new Control.Camara(posicion, puntoDeFoco);
            projeccion = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, AspectRatio, minVista, maxVista);
            deboDibujarDatos = false;
            teclaOprimida = false;

        }

        public Matrix getViewMatrix()
        {
            return camaraAsociada.getViewMatrix();
        }

        public Matrix getProjectionMatrix()
        {
            return projeccion;
        }

        public BoundingFrustum GetFrustum(){
            // Combinar las matrices de vista y proyección
            Matrix viewProjection = getViewMatrix() * getProjectionMatrix();

            // Crear el frustum basado en la matriz combinada
            frustum = new BoundingFrustum(viewProjection);
            return frustum;
        }

        public void GetInputs()
        {
            if (Font == null)
                throw new System.Exception("No puedo escribir sin una fuente");
            if (Keyboard.GetState().IsKeyDown(Keys.O) && !teclaOprimida)
            {
                deboDibujarDatos = !deboDibujarDatos;
                teclaOprimida = true;
            }
            if (Keyboard.GetState().IsKeyUp(Keys.O) && teclaOprimida)
                teclaOprimida = false;

        }

        public void setPuntoAtencion(Vector3 PuntoAtencion)
        {
            camaraAsociada.PuntoAtencion = PuntoAtencion;
        }

        public void DrawDatos(SpriteBatch batch)
        {
            if (deboDibujarDatos && Font != null)
            {
                batch.Begin();
                string output = "Información de la cámara";
                batch.DrawString(Font, output, Vector2.Zero, Color.LightGreen);
                batch.End();
            }
        }

        public void loadTextFont(string CarpetaEfectos, ContentManager contManager)
        {
            Font = contManager.Load<SpriteFont>(CarpetaEfectos + "debugFont");
        }
    } 

}