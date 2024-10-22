using System;
using System.Security.Cryptography.X509Certificates;
using Control;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Microsoft.Xna.Framework.Input;
using System.Data;
using System.Collections.Generic;
using BepuUtilities.Memory;

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
        DuracionPowerUp = 2f;
        boostVelocidad = 5f;
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
    private Simulation _simulacion;
    private BufferPool _bufferPool;
    private AutoJugador auto;
    private int MunicionMisiles = 0;
    public Model modelo;
    public Effect efecto;
    public float scale = 0.6f;
    public Vector3 posicion;
    protected float fuerza;
    protected BodyHandle handlerCuerpo;
    protected BodyReference refACuerpo;

    public Vector3 Posicion { get{return AyudanteSimulacion.NumericsToMicrosofth(refACuerpo.Pose.Position);}}
    public Matrix orientacion  { get{ return Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation);}}

    //Vector3 position = new Vector3(0,150,-150);
    public bool activado = false;
    public Matrix world;

    public Misil()
    {
        tipoPowerUp = "Misil";
        MunicionMisiles += 1;
        this.fuerza = 50f;

    }
    public void CrearColliderCinematico(Simulation _simulacion, BufferPool _bufferpool)
    {
        var compoundBuilder = new CompoundBuilder(_bufferpool, _simulacion.Shapes, 3);
        var capsuleShape = new Capsule(10f, 48f); // Ajusta dimensiones
        var capsuleLocalPose = new RigidPose(new Vector3(0f, 100f, 0f).ToNumerics(), Quaternion.Identity.ToNumerics());
        
        compoundBuilder.Add(capsuleShape, capsuleLocalPose, 5f);
        // Llamada corregida: solo devuelve los hijos y el centro
        compoundBuilder.BuildKinematicCompound(out var compoundChildren, out var compoundCenter);

        // Agregar el cuerpo cinemático a la simulación
        BodyHandle handlerDeCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateKinematic(compoundCenter, System.Numerics.Vector3.Zero, _simulacion.Shapes.Add(new Compound(compoundChildren)), 2f));
        this.darCuerpo(handlerDeCuerpo);
    }

        public void darCuerpo(BodyHandle handler)
        {
            handlerCuerpo = handler;
            refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);
            refACuerpo.Activity.SleepThreshold = -1;//esto es lo que permite que el objeto no sea 
                                                    //puesto a dormir
                                                    //valores negativos lo haceno No durmiente
                                                    //valores positivos solo le dan un tiempo hasta que duerma
        }
    public override void ActivarPowerUp(AutoJugador auto)
    {
        world = Matrix.CreateRotationX((float) Math.PI/2) * Matrix.CreateScale(scale) * auto.getWorldMatrix() * Matrix.CreateTranslation(posicion);
        activado = true;
        Console.WriteLine("Cantidad de misiles : " + MunicionMisiles);
    }

public void ActivarMisil(AutoJugador auto, Simulation _simulacion, BufferPool _bufferPool)
{
            world = Matrix.CreateRotationX((float) Math.PI/2) * Matrix.CreateScale(scale) * auto.getWorldMatrix() * Matrix.CreateTranslation(posicion);
        activado = true;
        Console.WriteLine("Cantidad de misiles : " + MunicionMisiles);
        this.CrearColliderCinematico(_simulacion, _bufferPool);
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

/*
        world *= Matrix.CreateTranslation(refACuerpo.Pose.Position* 15f);
        Vector3 currentPosition = this.Posicion;
        float velocidadMisil = 100f;
        Vector3 velocity = new Vector3(0, 0, 1) * velocidadMisil * (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 newPose = currentPosition + velocity;
        System.Numerics.Vector3 newPoseNumerics = new System.Numerics.Vector3(newPose.X, newPose.Y, newPose.Z);
        refACuerpo.Pose.Position = newPoseNumerics;
        refACuerpo.Pose.Position = AyudanteSimulacion.MicrosoftToNumerics(newPose);
        if (DuracionPowerUp <= 0 || MunicionMisiles <= 0)
        {
          DesactivarPowerUp(auto);
        }
*/
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