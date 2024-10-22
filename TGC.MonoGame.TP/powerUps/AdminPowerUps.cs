using System;
using System.Security.Cryptography.X509Certificates;
using Control;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Data;
using System.Collections.Generic;

abstract class PowerUp
{
    public string tipoPowerUp;
    public float DuracionPowerUp; // Duración del power-up en segundos

    public abstract void ActivarPowerUp(AutoJugador auto);
    public abstract void DesactivarPowerUp(AutoJugador auto);
    public abstract void ActualizarPowerUp(GameTime gameTime);

    public void ActivarPowerUp(string tipoPowerUp)
    {
    
    }
}

class Turbo : PowerUp
{
    private float boostVelocidad;
    private AutoJugador auto;
    public Turbo()
    {
        tipoPowerUp = "Turbo";
        DuracionPowerUp = 5f;
        boostVelocidad = 15f;
    }

    public override void ActivarPowerUp(AutoJugador auto)
    {
        auto.escalarDeVelocidad += boostVelocidad;

        Console.WriteLine("Turbo activado");
    }

    public override void DesactivarPowerUp(AutoJugador auto)
    {
        auto.escalarDeVelocidad -= boostVelocidad;
        Console.WriteLine("Turbo desactivado");
    }

    public override void ActualizarPowerUp(GameTime gameTime)
    {
        DuracionPowerUp -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (DuracionPowerUp <= 0)
        {
            DesactivarPowerUp(auto);
        }
    }
}

class Misil : PowerUp
{

    private AutoJugador auto;
    private int MunicionMisiles = 0;

    public Model modelo;
    public Effect efecto;
    public float scale = 0.6f;
    public Vector3 posicion= new Vector3(0,150,-150);
    public bool activado = false;
    public Matrix world;

    public Misil()
    {
        tipoPowerUp = "Misil";
        MunicionMisiles += 1;

    }

    public override void ActivarPowerUp(AutoJugador auto)
    {
        world = Matrix.CreateRotationX((float) Math.PI/2) * Matrix.CreateScale(scale) * auto.getWorldMatrix() * Matrix.CreateTranslation(posicion);
        activado = true;
        Console.WriteLine("Cantidad de misiles : " + MunicionMisiles);
    }

    public override void DesactivarPowerUp(AutoJugador auto)
    {
        Console.WriteLine("Misiles desactivados");
        MunicionMisiles = 0;
        activado = false;
    }

    public override void ActualizarPowerUp(GameTime gameTime)
    {
        world *= Matrix.CreateTranslation(Vector3.Normalize((world * Matrix.CreateRotationX((float) Math.PI/2)).Forward) * 15f);
        DuracionPowerUp -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        /*if (DuracionPowerUp <= 0 || MunicionMisiles <= 0)
        {
          DesactivarPowerUp(auto);
        }*/
    }

    public void loadModel(string direcionModelo, string direccionEfecto, ContentManager contManager)
    {    
        //asignamos el modelo deseado
        modelo = contManager.Load<Model>(direcionModelo);
        //mismo caso para el efecto
        efecto = contManager.Load<Effect>(direccionEfecto);
        foreach ( ModelMesh mesh in modelo.Meshes )
        {
            foreach ( ModelMeshPart meshPart in mesh.MeshParts)
            {
                meshPart.Effect = efecto;
            }
        }

    }
    public Matrix getWorldMatrix()
    {
       return world;
    }

    public void dibujar(Matrix view, Matrix projection, Color color)
    {
        if(activado){
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
        
    }
}