using System;
using BepuPhysics.Collidables;
using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
//como solo tendremos una projeccion presumo solo habra una camara, pero despues podemos cambiarlo,
//si quieren hacer algo como tener dos camara, una mas cerca del auto o algo parecido
namespace Control
{
    public class Camara
    {
        public Vector3 posicion;
        private Vector3 puntoAtencion;
        //queremos que al cambiar el punto de atencion, la camara siga manteniendose en el mismo lugar relativo
        //no se si queda limpio a si, probablemente seria mas legible con un metodo dedicado
        public Vector3 PuntoAtencion {get {return puntoAtencion;} 
            set 
            {
                Vector3 desplazamiento = value - puntoAtencion;

                // Condicional para el eje X
                if (posicion.X >= 8500 && value.X >= 7500)
                {
                    desplazamiento.X = 0;  // Mantener X constante si se excede el límite
                }

                // Condicional para el eje Z
                if (posicion.Z >= 8500 && value.Z >= 7500)
                {
                    desplazamiento.Z = 0;  // Mantener Z constante si se excede el límite
                }

                // Actualizar posición y punto de atención
                posicion += desplazamiento;
                puntoAtencion = value;
            }}

        public Camara(Vector3 posicion, Vector3 puntoAtencion)
        {
            this.posicion = posicion;
            this.puntoAtencion = puntoAtencion;
        }

        public Matrix getPerspectiveProjectionMatrix(float FOV, float aspectRatio, float nearPlane, float farPlane)
        {
            return Matrix.CreatePerspectiveFieldOfView(FOV, aspectRatio, nearPlane, farPlane);
        }

        public Matrix getOrtographic(float width, float height, float nearPlane, float farPlane)
        {
            return Matrix.CreateOrthographic(width, height, nearPlane, farPlane);
        }
        //para obtener la vision de la camara
        public Matrix getViewMatrix()
        {
            return Matrix.CreateLookAt(posicion,puntoAtencion, Vector3.Up);
        }
        public Matrix getIsometricView()
        {//para tener la vista isometrica
            return Matrix.CreateLookAt(posicion, puntoAtencion, Vector3.Up);
        }
        public void rotatePuntoAtencion(float angle)
        {
            Vector3 newPa = puntoAtencion - Vector3.UnitY * puntoAtencion.Y;
            Vector3 posPro = posicion - Vector3.UnitY * posicion.Y;
            Vector3 PaRelativo = newPa - posPro;
            PaRelativo = Vector3.Transform(PaRelativo, Matrix.CreateRotationY(angle));
            PaRelativo += posPro;
            PaRelativo.Y = puntoAtencion.Y;
            puntoAtencion = PaRelativo;
        }
    }
}
