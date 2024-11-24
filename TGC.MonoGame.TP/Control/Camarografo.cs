using System;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Control
{
    class Camarografo
    {
        public Control.Camara camaraAsociada;
        private Matrix projeccion;
        private bool teclaOprimida;
        private float nearPLane, farPLane;

        private bool ortographicMode = false;

        public Camarografo(Vector3 posicion, Vector3 puntoDeFoco, float AspectRatio, float minVista, float maxVista)
        {
            nearPLane = minVista;
            farPLane = maxVista;
            camaraAsociada = new Camara(posicion, puntoDeFoco);
            projeccion = camaraAsociada.getPerspectiveProjectionMatrix(MathF.PI / 4f, AspectRatio, minVista, maxVista);
            teclaOprimida = false;
        }
        public Camarografo(Vector3 posicion, Vector3 puntoDeFoco, float width, float height,float minVista, float maxVista)
        {
            nearPLane = minVista;
            farPLane = maxVista;
            camaraAsociada = new Camara(posicion, puntoDeFoco);
            projeccion = camaraAsociada.getOrtographic(width, height, nearPLane, farPLane);
            teclaOprimida = false;
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
        }

    }

}