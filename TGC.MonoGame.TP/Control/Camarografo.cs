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
        public Camara camaraAsociada;
        public Camara camaraLuz;
        private Matrix projeccion;
        private float nearPLane, farPLane;

        private bool ortographicMode = false;
        public luzConica AmbientLight;

        public BoundingFrustum frustum;

        public Camarografo(Vector3 posicion, Vector3 puntoDeFoco, float AspectRatio, float minVista, float maxVista)
        {
            nearPLane = minVista;
            farPLane = maxVista;
            camaraAsociada = new Camara(posicion, puntoDeFoco);
            projeccion = camaraAsociada.getPerspectiveProjectionMatrix(MathF.PI / 4f, AspectRatio, minVista, maxVista);
            AmbientLight = new luzConica(
                                new Vector3(0f, 0.5f, 1f) * 5000,
                                -new Vector3(0f, 0.5f, 1f),
                                Color.IndianRed,
                                0f,
                                1f);
            camaraLuz = new Camara(AmbientLight.posicion, puntoDeFoco);
        }
        public Camarografo(Vector3 posicion, Vector3 puntoDeFoco, float width, float height,float minVista, float maxVista)
        {
            nearPLane = minVista;
            farPLane = maxVista;
            camaraAsociada = new Camara(posicion, puntoDeFoco);
            projeccion = camaraAsociada.getOrtographic(width, height, nearPLane, farPLane);
            ortographicMode = true;
        }

        public void turnIsometric(float width, float heigth)
        {
            projeccion = camaraAsociada.getOrtographic(width, heigth, nearPLane, farPLane);
            ortographicMode = true;
        }

        public void turnPerspective(float aspectRatio) 
        {
            projeccion = camaraAsociada.getPerspectiveProjectionMatrix(MathF.PI / 4, aspectRatio, nearPLane, farPLane);
        }


        public Matrix GetLigthViewProj() => camaraLuz.getViewMatrix() * projeccion;

        public BoundingFrustum GetFrustum(){
            // Combinar las matrices de vista y proyección
            Matrix viewProjection = getViewMatrix() * getProjectionMatrix();

            // Crear el frustum basado en la matriz combinada
            frustum = new BoundingFrustum(viewProjection);
            return frustum;
        }

        public Matrix getViewMatrix()
        {
            return ortographicMode ? camaraAsociada.getIsometricView() : camaraAsociada.getViewMatrix();
        }

        public Matrix getProjectionMatrix()
        {
            return projeccion;
        }

        public void setPuntoAtencion(Vector3 PuntoAtencion)
        {
            camaraAsociada.PuntoAtencion = PuntoAtencion;
            camaraLuz.setPuntoDeAtencion(PuntoAtencion);
        }
        public void rotatePuntoAtencion(float angle)
        {
            camaraAsociada.rotatePuntoAtencion(angle);
            camaraLuz.rotatePuntoAtencion(angle);
        }

    }

}