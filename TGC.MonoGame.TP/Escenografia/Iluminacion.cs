using Control;
using Microsoft.Xna.Framework;

public struct luzConica
{//es conica pero en teoria la puedes usar para lo que quieras
    public Vector3 posicion;
    public Vector3 direccion;
    public Vector3 color;
    public float porcentajeDeProjeccionVorde;
    public float intensidad;
    public luzConica(Vector3 posicion, Vector3 direccion, Color color, float porcentajeDeProjeccionVorde, float intensidad)
    {
        this.posicion                    = posicion;
        this.direccion                   = Vector3.Normalize(direccion);
        this.color                       = color.ToVector3();
        this.porcentajeDeProjeccionVorde = porcentajeDeProjeccionVorde;
        this.intensidad                  = intensidad;
    }
}