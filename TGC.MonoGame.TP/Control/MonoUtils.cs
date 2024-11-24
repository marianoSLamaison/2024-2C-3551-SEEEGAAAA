using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

class MonoHelper {
    //textura usada para poder ignorar los temas de algunos modelos no teniendo texturas pero poder seguir usando el mismo shader
    static Texture2D defaultTexture;
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        defaultTexture = new Texture2D(graphicsDevice, 1, 1);
    }
    public static BoundingSphere GenerarBoundingSphere(Model modelo)
    {
        Vector3 centro, min, max;
        float radio;
        GetMinMaxVertices(out min, out max, modelo);
        centro = (max - min)/2f + min;
        radio = (centro - min).Length();
        return new BoundingSphere(centro, radio);
    }

    public static BoundingSphere GenerarBoundingSphere(Model modelo, float escala)
    {
        Vector3 centro, min, max;
        float radio;
        GetMinMaxVertices(out min, out max, modelo);
        centro = (max - min)/2f + min;
        radio = (centro - min).Length() * escala;
        return new BoundingSphere(centro, radio);
    }

    public static void OperarModelo<T>(out List<T> salida, Func<Vector3, T> operador, Model modelo)
    {
        salida = new List<T>();
        
        foreach( ModelMesh modelM in modelo.Meshes)
        {
            foreach( ModelMeshPart part in modelM.MeshParts)
            {
                VertexPositionColorNormal[] datosMesh = new VertexPositionColorNormal[part.VertexBuffer.VertexCount];
                part.VertexBuffer.GetData(datosMesh);
                foreach(VertexPositionColorNormal vertice in datosMesh)
                {
                    salida.Add(operador(vertice.Position));
                }
            }
        }
    }

    public static void GetCentroRadio(out Vector3 centro, out float radio, Model modelo)
    {
        //primero sacamos el centro geometrico
        List<Vector3> posiblesCentros;
        Vector3 max, center;
        OperarModelo<Vector3>(out posiblesCentros, (vec) => {return vec;}, modelo);
        center = Utils.Commons.Sum<Vector3>(posiblesCentros, (A, B) => {return A + B;}, Vector3.Zero);
        center /= posiblesCentros.Count;
        //sacamos la distancia maxima que algun punto tenga de ese centro para el radio
        max = Utils.Commons.map<Vector3>(posiblesCentros, (vec) => {return vec - center;}).MaxBy((vec) => {return vec.LengthSquared();});
        radio = max.Length();
        centro = center;
    }
    
    public static void GetMinMaxVertices(out Vector3 min, out Vector3 max, Model modelo)
    {
        min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach( ModelMesh mMesh in modelo.Meshes)
        {
            foreach ( ModelMeshPart parte in mMesh.MeshParts){
                VertexPositionNormalTexture[] datosMesh = new VertexPositionNormalTexture[parte.VertexBuffer.VertexCount];
                parte.VertexBuffer.GetData(datosMesh);
                foreach( VertexPositionNormalTexture vert in datosMesh )
                {
                    min = new Vector3(MathF.Min(min.X, vert.Position.X), MathF.Min(min.Y, vert.Position.Y), MathF.Min(min.Z, vert.Position.Z) );
                    max = new Vector3(MathF.Max(min.X, vert.Position.X), MathF.Max(min.Y, vert.Position.Y), MathF.Max(min.Z, vert.Position.Z) );
                }
            }
        }
    }
    //resulta que las BB de mono game son Axis Aligned, pero dejo esto por si acaso se necesita
    public static BoundingBox GenerarBoundingBox (Model modelo)
    {
        Vector3 min;
        Vector3 max; 
        GetMinMaxVertices(out min, out max, modelo);
        return new BoundingBox(min, max);
    }
    public static void loadShaderMatrices(Effect efecto, Matrix world, Matrix view, Matrix proj)
    {
        //estos 3 parametros son basicos para trabajar todo tipo de shader que tenemos
        efecto.Parameters["World"]?.SetValue(world);
        efecto.Parameters["View"]?.SetValue(view);
        efecto.Parameters["Projection"]?.SetValue(proj);
        //pero igual por si acaso les pongo la marga de precausion
    }
    public const String kambient = "KAmbient", kdiffuse = "KDiffuse", kspecular = "KSpecular", shininess = "shininess";
    public static void loadKColorValues(Effect efecto, float kambient, float kdiffuse, float kspecular)
    {
        efecto.Parameters["KLuzAmbiental"]?.SetValue(kambient);
        efecto.Parameters["KLuzDifusa"].SetValue(kdiffuse);
        efecto.Parameters["KSpecular"]?.SetValue(kspecular);
    }
    public static void loadShaderLigthColors(Effect efecto, Color  ambiente, Color difuso, Color especular)
    {
        efecto.Parameters["ambientColor"]?.SetValue(ambiente.ToVector3());
        efecto.Parameters["diffuseColor"]?.SetValue(difuso.ToVector3());
        efecto.Parameters["specularColor"]?.SetValue(especular.ToVector3());
    }
    public static void loadShaderTextures(Effect efecto, Texture2D color, Texture2D metalic, Texture2D AO, Texture2D rougness)
    {
        //esto a si el shader puede trabajar incluso si no usamos texturas
        color = color == null ? defaultTexture : color;
        metalic = metalic == null ? defaultTexture : metalic;
        AO = AO == null ? defaultTexture : AO;
        rougness = rougness == null ? defaultTexture : rougness;
        efecto.Parameters["baseTexture"]?.SetValue(color);
        efecto.Parameters["metallicTexture"]?.SetValue(metalic);
        efecto.Parameters["AOTexture"]?.SetValue(AO);
        efecto.Parameters["roughnessTexture"]?.SetValue(rougness);
    }
}