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

public abstract class PowerUp
{
    public string tipoPowerUp;
    public float DuracionPowerUp; // Duración del power-up en segundos

    //public abstract void ActivarPowerUp(AutoJugador auto);
    public abstract void DesactivarPowerUp(AutoJugador auto);
    public abstract void ActualizarPowerUp(GameTime gameTime);
    public virtual void ActivarPowerUp(Matrix autoMatrix, Vector3 autoPosicion){}
    public virtual void ActivarPowerUp(AutoJugador auto){}
}

public class Turbo : PowerUp
{
    private float boostVelocidad;
    public Turbo()
    {
        tipoPowerUp = "Turbo";
        DuracionPowerUp = 2f;
        boostVelocidad = 50f;
        DuracionPowerUp += 1000f;
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
            //DesactivarPowerUp(auto);
        }
    }
}

public class Misil : PowerUp
{
    private int MunicionMisiles = 0;
    public Model modelo;
    public Effect efecto;
    public float scale = 0.8f;

    Vector3 posicionRelativaAlAuto = new Vector3(0, 150, 0);

    protected float fuerza;
    public BodyHandle handlerCuerpo;
    protected BodyReference refACuerpo;
    public Matrix orientacionAutoSalida;

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
    public void CrearColliderMisil(Simulation _simulacion, Dictionary<int, object> bodyHandleTags)
    {
        //var compoundBuilder = new CompoundBuilder(_bufferpool, _simulacion.Shapes, 3);
        var capsuleShape = new Capsule(30f, 120f); // Ajusta dimensiones
        var capsuleLocalPose = 
            new RigidPose(System.Numerics.Vector3.UnitY * -10000f);
            //Quaternion.CreateFromYawPitchRoll(MathF.PI/2, 0, 0).ToNumerics());

        BodyInertia bodyInertia = capsuleShape.ComputeInertia(.5f);

        //compoundBuilder.Add(capsuleShape, capsuleLocalPose, 5f);
        // Llamada corregida: solo devuelve los hijos y el centro
        //compoundBuilder.BuildKinematicCompound(out var compoundChildren, out var compoundCenter);

        // Agregar el cuerpo cinemático a la simulación
        handlerCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateDynamic(capsuleLocalPose, bodyInertia, _simulacion.Shapes.Add(capsuleShape), 0.01f));
        bodyHandleTags.Add(handlerCuerpo.Value, this);

        this.darCuerpo(handlerCuerpo);
    }

    public void darCuerpo(BodyHandle handler)
    {
        handlerCuerpo = handler;
        refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);
        //refACuerpo.Activity.SleepThreshold = -1;
    }
    public override void ActivarPowerUp(Matrix orientacionAuto, Vector3 posicionAuto)
    {
        refACuerpo.Velocity.Angular = System.Numerics.Vector3.Zero;
        refACuerpo.Pose.Position = (posicionAuto + posicionRelativaAlAuto).ToNumerics();

        refACuerpo.Velocity.Linear = System.Numerics.Vector3.Zero;

        world = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation) * Matrix.CreateTranslation(Posicion);
        orientacionAutoSalida = orientacionAuto;
        refACuerpo.Velocity.Linear = new Vector3(orientacionAutoSalida.Backward.X, orientacionAutoSalida.Backward.Y, orientacionAutoSalida.Backward.Z).ToNumerics() * 2000f;
        activado = true;
        DuracionPowerUp = 1;
        //Console.WriteLine("Cantidad de misiles : " + MunicionMisiles);
    }

    public override void DesactivarPowerUp(AutoJugador auto)
    {
        Console.WriteLine("Misiles desactivados");
        MunicionMisiles = 0;
        activado = false;
    }
    public void DesactivarPowerUp(){
        activado = false;
    }

    public override void ActualizarPowerUp(GameTime gameTime)
    {
        refACuerpo.Pose.Orientation = Quaternion.CreateFromRotationMatrix(orientacionAutoSalida).ToNumerics() 
                                    * Quaternion.CreateFromYawPitchRoll(0, MathF.PI/2, 0).ToNumerics();
        world = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation) * Matrix.CreateTranslation(Posicion);

        if(activado){
            refACuerpo.Velocity.Linear += new Vector3(orientacionAutoSalida.Backward.X, 0, orientacionAutoSalida.Backward.Z).ToNumerics();
            DuracionPowerUp -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }else{
            refACuerpo.Pose.Position = System.Numerics.Vector3.UnitY * -10000f;
        }

        activado = DuracionPowerUp >= 0;
        //Console.WriteLine(DuracionPowerUp);
        
    
        
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

    public void GuardarMisilEnMundo(){
        refACuerpo.Pose.Position = System.Numerics.Vector3.UnitY * -10000f;
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




public class Metralleta : PowerUp
{
    private int municionMetralleta = 0;
    public Model modelo;
    public Effect efecto;
    public float scale = 1f;
    protected float fuerza;
    protected BodyHandle handlerCuerpo;
    protected BodyReference refACuerpo;

    Vector3 posicionRelativaAlAuto = new Vector3(0, 150, 0);

    public Matrix orientacionAutoSalida;

    public Vector3 Posicion { get{return AyudanteSimulacion.NumericsToMicrosofth(refACuerpo.Pose.Position);}}
    public Matrix orientacion { get { return Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation); } }

    public bool activado = false;
    public Matrix world;

    public Metralleta()
    {
        tipoPowerUp = "Metralleta";
        municionMetralleta = 2;
        this.fuerza = 50;
    }
    public void CrearColliderMetralleta(Simulation _simulacion, Dictionary<int, object> bodyHandleTags)
    {
        var capsuleShape = new Sphere(10f); // Ajusta dimensiones
        var capsuleLocalPose = 
            new RigidPose(System.Numerics.Vector3.UnitY * -10000f);
            //Quaternion.CreateFromYawPitchRoll(MathF.PI/2, 0, 0).ToNumerics());

        BodyInertia bodyInertia = capsuleShape.ComputeInertia(.5f);

        // Agregar el cuerpo cinemático a la simulación
        handlerCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateDynamic(capsuleLocalPose, bodyInertia, _simulacion.Shapes.Add(capsuleShape), 0.01f));
        bodyHandleTags.Add(handlerCuerpo.Value, this);

        this.darCuerpo(handlerCuerpo);
    }

    public void darCuerpo(BodyHandle handler)
    {
        handlerCuerpo = handler;
        refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);
        //refACuerpo.Activity.SleepThreshold = -1;
    }
    public override void ActivarPowerUp(Matrix orientacionAuto, Vector3 posicionAuto)
    {
        refACuerpo.Velocity.Angular = System.Numerics.Vector3.Zero;
        refACuerpo.Pose.Position = (posicionAuto + posicionRelativaAlAuto).ToNumerics();

        refACuerpo.Velocity.Linear = System.Numerics.Vector3.Zero;

        world = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation) * Matrix.CreateTranslation(Posicion);
        orientacionAutoSalida = orientacionAuto;
        refACuerpo.Velocity.Linear = new Vector3(orientacionAutoSalida.Backward.X, orientacionAutoSalida.Backward.Y, orientacionAutoSalida.Backward.Z).ToNumerics() * 4000f;
        activado = true;
        DuracionPowerUp = 1;
    }

    public override void DesactivarPowerUp(AutoJugador auto)
    {
        Console.WriteLine("Metralleta desactivada");
        municionMetralleta = 0;
        activado = false;
    }

     public override void ActualizarPowerUp(GameTime gameTime)
    {
        refACuerpo.Pose.Orientation = Quaternion.CreateFromRotationMatrix(orientacionAutoSalida).ToNumerics() 
                                    * Quaternion.CreateFromYawPitchRoll(0, MathF.PI/2, 0).ToNumerics();
        world = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation) * Matrix.CreateTranslation(Posicion);

        if(activado){
            refACuerpo.Velocity.Linear += new Vector3(orientacionAutoSalida.Backward.X, 0, orientacionAutoSalida.Backward.Z).ToNumerics();
            DuracionPowerUp -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }else{
            refACuerpo.Pose.Position = System.Numerics.Vector3.UnitY * -10000f;
        }

        activado = DuracionPowerUp >= 0;
        //Console.WriteLine(DuracionPowerUp);
    }

    public void GuardarBalaEnMundo(){
        refACuerpo.Pose.Position = System.Numerics.Vector3.UnitY * -10000f;
    }


    public void loadModel(string direccionModelo, string direccionEfecto, ContentManager contManager)
    {
        modelo = contManager.Load<Model>(direccionModelo);
        efecto = contManager.Load<Effect>(direccionEfecto);
        foreach (ModelMesh mesh in modelo.Meshes)
        {
            foreach (ModelMeshPart meshPart in mesh.MeshParts)
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
