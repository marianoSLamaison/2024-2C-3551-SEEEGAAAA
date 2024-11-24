using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Control;
using Escenografia;
using System;
using System.Collections.Generic;
using BepuUtilities.Memory;



namespace Escenografia
{
    struct LimBox
    {
        public Vector3 minVertice;
        public Vector3 maxVertice;
        public LimBox(Vector3 minVertice, Vector3 maxVertice)
        {
            this.minVertice = minVertice;
            this.maxVertice = maxVertice;
        }
    }

    /// <summary>
    /// Lo separe de Escenografia 3D para poder tener la Posicion ligada a nuestro objeto
    /// </summary>
    abstract class Auto 
    {
        //ralacionadas con movimiento

        /// <summary>
        /// Este es el handler del cuerpo, hay mas cosas a demas de la ref a si que nos lo quedamos por si acaso
        /// </summary>
        protected BodyHandle handlerCuerpo;
        /// <summary>
        /// esta es la referencia al cuerpo, con esto es con lo que aplicamos fuerzas y demas
        /// </summary>
        public BodyReference refACuerpo;
        /// <summary>
        /// Esto es para ligar la posicion con la que trabaja Bepu a nuestro modelo visible
        /// No tengo idea como valla a reaccionar Bepu si ustedes simplemente le setean una posicion a si que solo pueden consultarla
        /// </summary>
        public Vector3 Posicion { get{return AyudanteSimulacion.NumericsToMicrosofth(refACuerpo.Pose.Position);}}
        /// <summary>
        /// Aproximo vamos a tener que usarla en algun momento para rotar bien el auto cuando este pegue saltos
        /// </summary>
        public Matrix orientacion  { get{ return Matrix.CreateFromQuaternion(refACuerpo.Pose.Orientation);}}
        /// <summary>
        /// Para mover al auto
        /// </summary>
        protected float fuerza;
        protected float velocidadAngular;
        //protected float peso;
        protected bool estaSaltando = false;

        protected float velocidadVertical = 0f;
      //esto es lo nuevo
        protected float altura = 0f;
        protected const float velocidadSalto = 980f;        //Antes era 500f

        protected const float maximaVelocidadPosible = 500f; //Antes era 2536f;

        protected Box limites;
        protected Vector3 direccion;

        protected float rotacionRuedasDelanteras;
        protected float revolucionDeRuedas;

        public float velocidad = 500000f;


     ///////Cosas de textureo//////////////   
        protected List<Texture2D> Textures {get;set;} 

        protected Model modelo; 
        protected Effect efecto;
        protected Texture2D baseColorTexture;
        protected Texture2D normalTexture;
        protected Texture2D metallicTexture;
        protected Texture2D roughnessTexture;
        protected Texture2D AOTexture;
        protected Texture2D emissionTexture;

        //para referencia el auto contiene las dimensiones 300 ancho, 500 largo y mas o menos 100 alto

        protected Vector3 posicionRuedaDelanteraIzquierda => new Vector3(-75f, -36, 150f); // Ajusta según tu modelo
        protected Vector3 posicionRuedaDelanteraDerecha => new Vector3(75f, -36, 150f);
        protected Vector3 posicionRuedaTraseraIzquierda => new Vector3(-75f, -36, -150f);
        protected Vector3 posicionRuedaTraseraDerecha => new Vector3(75f, -36, -150f);



        //para limitar el movimiento de objetos
        //esto es una constante
        static protected Vector3 esquinaInferiorEsc = new Vector3(1f,0f,1f) * -10000f;
        static protected Vector3 esquinaSuperiorEsc = new Vector3(1f, 0f, 1f) * 10000f;
        //vector unitario

        //esto lo implementan los hijos de la clase
        abstract public void Mover(float fuerzaAAplicar);
        abstract public Matrix getWorldMatrix();
        abstract public void loadModel(string direccionModelo, string direccionEfecto, ContentManager contManager);
        //abstract public void dibujar(Matrix view, Matrix projection, Color color);
        /// <summary>
        /// se encarga de asignar un cuerpo para el auto, siempre hara que no duerma por que 
        /// Bepu pone a "dormir" todo lo que no este moviendose y por culpa de eso, luego no puedes moverlo
        /// Sigue siendo algo con lo que puedes chocar, solo que no puedes aplicarle impulso
        /// con las funciones apply linear impulsa, y bepu no se molestara en moverlo
        /// </summary>
        /// <param name="handler"> es solo el handler del objeto </param>
        public void darCuerpo(BodyHandle handler)
        {
            handlerCuerpo = handler;
            refACuerpo = AyudanteSimulacion.getRefCuerpoDinamico(handler);
            refACuerpo.Activity.SleepThreshold = -1;//esto es lo que permite que el objeto no sea 
                                                    //puesto a dormir
                                                    //valores negativos lo haceno No durmiente
                                                    //valores positivos solo le dan un tiempo hasta que duerma
        }

    }

    class AutoJugador : Auto
    {   
        float RotUp, RotFront, RotSide;
        
        /// <summary>
        /// se prevee Otro rework para cuando agreguemos friccion con plano ( si se puede )
        /// Estoy apostandole a poder poner friccion con el piso en el engine mismo, a si que esto solo 
        /// dice el comportamiento historico de la velocidad, no se usa para nada mas
        /// Osea que tanto has estado llendo Ej: hacia a delante. Y por tanto que tanto
        /// deberias estar apretanto atras, para revertir el movimiento
        /// </summary>
        float comportamientoDeVelocidad;
        public TypedIndex referenciaAFigura;
        public float escalarDeVelocidad = 150f;
        public ConstraintHandle constraintHandle;
        public float anteriorFuerzaBuscada = 0f;
        public float anteriorVelocidadBuscada = 0f;

        public Misil Misil;
        public Metralleta Metralleta;
        public AutoJugador(Vector3 direccion, float velocidadGiro, float fuerzaDireccional)
        {
            this.direccion = direccion;
            this.velocidadAngular = velocidadGiro;
            this.fuerza = 50f;
        }
        public void setVelocidadGiro(float velocidadGiro)
        {
            this.velocidadAngular = velocidadGiro;
        }

        public Vector3 getDireccion()
        {
            return this.direccion;
        }

        public Vector3 getRefACuerpo(){
            return refACuerpo.Velocity.Linear;
        }

        public override Matrix getWorldMatrix() =>  orientacion * Matrix.CreateTranslation(Posicion);

        private float duracionTurbo = 0f;  // Variable para controlar la duración del turbo
        private bool turboActivo = false; 

        
        public void RecogerPowerUp(PowerUp powerUp)
        {
            powerUp.ActivarPowerUp(orientacion, refACuerpo.Pose.Position);   
        }
        
        /// <summary>
        /// Este metodo tomara los imputs del jugador y seteara las variables necesarias
        /// para mover el mismo con el metodo mover
        /// </summary>
        override public void Mover(float deltaTime)
        {
                //Console.WriteLine(refACuerpo.Pose.Position);
                if ( !estaSaltando )
                {
                
                float vAngularInst = velocidadAngular * deltaTime;
                float velocidadGRuedas = vAngularInst * 2.00f;//es solo un poco mas rapida que el giro del auto
                //si estamos en la 
                float sentidoMov = comportamientoDeVelocidad > 0 ? 1 : -1;
                //estas estan dedicadas a incrementar la fuerza con la que se mueve el auto
                //aparentemente nuestro auto esta mirando hacia atras a si que estan puestos asi
                
                //Uso la orientacion para tener cubierto el temita de que posiblemente
                //los choques con otros autos puedan alterar la rotacion del modelo durante la partida

                if (turboActivo)
                {
                    refACuerpo.ApplyLinearImpulse(orientacion.Backward.ToNumerics() * escalarDeVelocidad * 5f); // Turbo multiplicador
                }


                if(Keyboard.GetState().IsKeyDown(Keys.R)){
                    refACuerpo.Pose.Orientation = Quaternion.Identity.ToNumerics();
                }

                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    comportamientoDeVelocidad += 1f;
                    //refACuerpo.Velocity.Linear += orientacion.Backward.ToNumerics() * escalarDeVelocidad;
                    //refACuerpo.ApplyImpulse(orientacion.Backward.ToNumerics() * escalarDeVelocidad, Vector3.Zero.ToNumerics());
                    refACuerpo.ApplyLinearImpulse(orientacion.Backward.ToNumerics() * escalarDeVelocidad);
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    comportamientoDeVelocidad += -1f;
                    //refACuerpo.Velocity.Linear += orientacion.Backward.ToNumerics() * -escalarDeVelocidad;
                    //refACuerpo.ApplyImpulse(orientacion.Forward.ToNumerics() * escalarDeVelocidad , Vector3.Zero.ToNumerics());
                    refACuerpo.ApplyLinearImpulse(orientacion.Forward.ToNumerics() * escalarDeVelocidad);
                }
                else
                {
                    refACuerpo.Velocity.Linear *= 0.96f;
                    comportamientoDeVelocidad *= 0.96f;
                }
                //Estas dos estan dedicadas a inclinar el auto
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    rotacionRuedasDelanteras += velocidadGRuedas;
                    RotUp += vAngularInst * sentidoMov;
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    rotacionRuedasDelanteras -= velocidadGRuedas;
                    RotUp -= vAngularInst * sentidoMov;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.T))
                {
                    //Turbo turbo = new Turbo();
                    turboActivo = true;
                    //RecogerPowerUp(turbo);
                    //escalarDeVelocidad = 300f;
                }
                else{
                    turboActivo= false;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.M))
                {
                    RecogerPowerUp(Misil);
                }
                else if(Keyboard.GetState().IsKeyDown(Keys.P))
                {
                    
                    RecogerPowerUp(Metralleta);
                }
                //evitamos que las ruedas den una vuelta entera
                rotacionRuedasDelanteras = Convert.ToSingle(Math.Clamp(rotacionRuedasDelanteras, -Math.PI/4f, Math.PI/4f));
                //para no tener el problema de estar girando por siempre a un mismo lado
                RotUp = Convert.ToSingle(Math.Clamp(RotUp, -Math.PI/2, MathF.PI/2));
                //solo nos interesa rotar si nos movemos, de otra forma solo rotamos ruedas
                if ( refACuerpo.Velocity.Linear.LengthSquared() > 1f )
                {
                    RotUp *= 0.98f;
                    refACuerpo.Velocity.Angular *= 0.98f;
                    refACuerpo.Velocity.Angular += orientacion.Up.ToNumerics() * RotUp * 1/60f;
                    revolucionDeRuedas += vAngularInst;
                }
                    
                rotacionRuedasDelanteras *= 0.96f;
                //Esto nos bloqueara el movimiento cuando estemos en el aire, y agregara un impulso desde abajo
                //Esto queda de tarea para el que tenia que hacer el piso
                if (Keyboard.GetState().IsKeyDown(Keys.Space) && !estaSaltando )
                {
                    estaSaltando = true;
                    refACuerpo.Velocity.Linear += new System.Numerics.Vector3(0f, 1000f, 0f);
                }
            } else {
                if ( refACuerpo.Velocity.Linear.Y < 0.05f)
                    estaSaltando = false;
                if ( Keyboard.GetState().IsKeyDown(Keys.A) )
                {
                    refACuerpo.Velocity.Angular += orientacion.Forward.ToNumerics() * velocidadAngular * deltaTime;
                }
                else if ( Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    refACuerpo.Velocity.Angular -= orientacion.Forward.ToNumerics() * velocidadAngular * deltaTime;
                }
                else if ( Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    refACuerpo.Velocity.Angular -= orientacion.Left.ToNumerics() * velocidadAngular * deltaTime;
                }
                else if ( Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    refACuerpo.Velocity.Angular -= orientacion.Left.ToNumerics() * velocidadAngular * deltaTime;
                }else{
                    refACuerpo.Velocity.Angular *= 0.80f;
                }
            }
        }

    public void CrearCollider(Simulation _simulacion, BufferPool _bufferpool){

        var compoundBuilder = new CompoundBuilder(_bufferpool, _simulacion.Shapes, 3);

        var boxMainShape = new Box(300f, 100f, 500f);
        //var boxMainShape = new Capsule(100, 400f);
        //Esta cosa estaba rotando la caja y dejandono como si estuviese caida
        //Quaternion.CreateFromYawPitchRoll(0f, MathF.PI/2, 0f).ToNumerics()
        var capsuleMainLocalPose = new RigidPose(new Vector3(0f,100f,0f).ToNumerics());
        //var capsuleMainLocalPose = new RigidPose(new Vector3(0f,120f,0f).ToNumerics());

        var ruedaShape = new Cylinder(17.5f, 35);
        var ruedaDelanteraIzquierdaLocalPose = new RigidPose(posicionRuedaDelanteraIzquierda.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
        var ruedaDelanteraDerechaLocalPose = new RigidPose(posicionRuedaDelanteraDerecha.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
        var ruedaTraseraIzquierdaLocalPose = new RigidPose(posicionRuedaTraseraIzquierda.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
        var ruedaTraseraDerechaLocalPose = new RigidPose(posicionRuedaTraseraDerecha.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());

        compoundBuilder.Add(boxMainShape, capsuleMainLocalPose, 5f);
        compoundBuilder.Add(ruedaShape, ruedaDelanteraIzquierdaLocalPose,.5f);
        compoundBuilder.Add(ruedaShape, ruedaDelanteraDerechaLocalPose, .5f);
        compoundBuilder.Add(ruedaShape, ruedaTraseraIzquierdaLocalPose, .5f);
        compoundBuilder.Add(ruedaShape, ruedaTraseraDerechaLocalPose, .5f);

        compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var compoundCenter);
        compoundBuilder.Reset();

        BodyHandle handlerDeCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateDynamic(compoundCenter + System.Numerics.Vector3.UnitY * 1000f, compoundInertia, _simulacion.Shapes.Add(new Compound(compoundChildren)), 0.01f));
        this.darCuerpo(handlerDeCuerpo);

    }

    public void ApplyTexturesToShader()
    {
        efecto.Parameters["baseTexture"].SetValue(baseColorTexture);
        efecto.Parameters["metallicTexture"]?.SetValue(metallicTexture);
        efecto.Parameters["AOTexture"]?.SetValue(AOTexture);
        efecto.Parameters["normalTexture"]?.SetValue(normalTexture);
        
        

        efecto.Parameters["lightPosition"]?.SetValue(new Vector3(7000,3000,2000));

        efecto.Parameters["ambientColor"]?.SetValue(new Vector3(0.25f, 0.25f, 0.25f));
        efecto.Parameters["diffuseColor"]?.SetValue(new Vector3(0.75f, 0.75f, 0.75f));
        efecto.Parameters["specularColor"]?.SetValue(new Vector3(1f, 1f, 1f));

        efecto.Parameters["KAmbient"]?.SetValue(0.4f);
        efecto.Parameters["KDiffuse"]?.SetValue(1.0f);
        efecto.Parameters["KSpecular"]?.SetValue(0.25f);
        efecto.Parameters["shininess"]?.SetValue(4.0f);

        foreach ( ModelMesh mesh in modelo.Meshes )
        {
            foreach ( ModelMeshPart meshPart in mesh.MeshParts)
            {
                meshPart.Effect = efecto;
            }
        }
    }

    public void CargarModelo(Effect efecto, Model modelo, Texture2D[] texturas )
    {
        this.efecto = efecto;
        this.modelo = modelo;
        baseColorTexture = texturas[0];
        normalTexture = texturas[1];
        metallicTexture = texturas[2];
        roughnessTexture = texturas[3];
        AOTexture = texturas[4];
        emissionTexture = texturas[5];

        //this.ApplyTexturesToShader();

        // Asignar el shader a cada parte del modelo
        foreach (ModelMesh mesh in modelo.Meshes)
        {   
            //Console.WriteLine(mesh.Name);
            foreach (ModelMeshPart meshPart in mesh.MeshParts)
            {
                meshPart.Effect = efecto;
            }
        }
    }

    public override void loadModel(string direccionModelo, string direccionEfecto, ContentManager contManager){
            //asignamos el modelo deseado
            modelo = contManager.Load<Model>(direccionModelo);
            //mismo caso para el efecto
            efecto = contManager.Load<Effect>(direccionEfecto);

            // Cargar texturas específicas
            baseColorTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_basecolor_0");
            normalTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_normal");
            metallicTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_metallic");
            roughnessTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_rougness");
            AOTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_ao");
            emissionTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_emission");

            this.ApplyTexturesToShader();

            // Asignar el shader a cada parte del modelo
            foreach (ModelMesh mesh in modelo.Meshes)
            {   
                //Console.WriteLine(mesh.Name);
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                }
            }
        }
        public void LlenarGbuffer(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection)
        {
            efecto.CurrentTechnique = efecto.Techniques["DeferredShading"];

            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);
            //seteamos aqui las texturas
            MonoHelper.loadShaderTextures(efecto, baseColorTexture, metallicTexture, AOTexture, roughnessTexture);

            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        public void dibujar(Matrix view, Matrix projection, RenderTarget2D shadowMap)
        {
            efecto.CurrentTechnique = efecto.Techniques["AutoTechnique"];

            efecto.Parameters["shadowMap"]?.SetValue(shadowMap);
            efecto.Parameters["shadowMapSize"]?.SetValue(Vector2.One * 16384);

            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);

            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        public void dibujarSombras(Matrix ligthView, Matrix projection){
            efecto.CurrentTechnique = efecto.Techniques["DepthPass"];
            //cargamos la view en el shader
            efecto.Parameters["View"].SetValue(ligthView);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);
            //setea este parametro, si es que no a sido eliminado por HLSL (denuevo)
            efecto.Parameters["LightViewProjection"]?.SetValue(ligthView * projection);

            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        public void LlenarEfectsBuffer(Camarografo camarografo)
        {

            efecto.CurrentTechnique = efecto.Techniques["EffectsPass"];
            MonoHelper.loadShaderMatrices(efecto, getWorldMatrix(),
            camarografo.getViewMatrix(),
            camarografo.getProjectionMatrix(),
            camarografo.GetLigthViewProj());
            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
    }
    
    class AutoNPC : Auto
    {

        private float anguloCorreccion;
        private float MaxRuedaRotacion;
        public BoundingSphere BoundingVolume;

        public void LlenarGbuffer(Matrix view, Matrix proj, Matrix lightViewProj)
        {
            efecto.CurrentTechnique = efecto.Techniques["DeferredShading"];
            MonoHelper.loadShaderMatrices(efecto, getWorldMatrix(), view, proj, lightViewProj);
            MonoHelper.loadShaderTextures(efecto, baseColorTexture, metallicTexture, AOTexture, roughnessTexture);
            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        public void LlenarEfectsBuffer(Matrix view, Matrix proj, Matrix lightViewProj)
        {
            efecto.CurrentTechnique = efecto.Techniques["EffectsPass"];
            MonoHelper.loadShaderMatrices(efecto, getWorldMatrix(),
            view,
            proj,
            lightViewProj);
            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        public void dibujar(Matrix view, Matrix projection, RenderTarget2D shadowMap)
        {
            efecto.CurrentTechnique = efecto.Techniques["AutoTechnique"];

            efecto.Parameters["shadowMap"]?.SetValue(shadowMap);
            efecto.Parameters["shadowMapSize"]?.SetValue(Vector2.One * 16384);
            MonoHelper.loadKColorValues(efecto, 1f, 0.7f, 0.2f);
            efecto.Parameters["brillantes"]?.SetValue(0.3f);

            efecto.Parameters["View"].SetValue(view);
            // le cargamos el como quedaria projectado en la pantalla
            efecto.Parameters["Projection"].SetValue(projection);

            foreach( ModelMesh mesh in modelo.Meshes)
            {
                if(mesh.Name == "Car")
                    efecto.Parameters["World"].SetValue(mesh.ParentBone.Transform * 
                    //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                    getWorldMatrix());

                if (mesh.Name.StartsWith("Wheel"))
                {
                    Vector3 posicionRueda = Vector3.Zero;
                    float rotacionYRueda = 0f;

                    // Determinar la posición de la rueda según su nombre
                    if (mesh.Name == "WheelB") {// Rueda delantera izquierda
                        posicionRueda = posicionRuedaDelanteraIzquierda;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelA"){ // Rueda delantera derecha
                        posicionRueda = posicionRuedaDelanteraDerecha;
                        rotacionYRueda = rotacionRuedasDelanteras;
                    }
                    else if (mesh.Name == "WheelD") {
                        // Rueda trasera izquierda
                        posicionRueda = posicionRuedaTraseraIzquierda;
                        rotacionYRueda = 0;
                    }
                    else if (mesh.Name == "WheelC"){ // Rueda trasera derecha
                        posicionRueda = posicionRuedaTraseraDerecha;
                        rotacionYRueda = 0;
                    }
                    // Calcular la matriz de transformación para la rueda
                    Matrix wheelWorld = orientacion * // cargamos su rotacion con respecto del eje XZ con respecto del auto
                                        Matrix.CreateTranslation(Posicion); // cargamos su posicion con respcto del auto
        
                    efecto.Parameters["World"].SetValue(Matrix.CreateRotationX(revolucionDeRuedas) * //primero la rotamos sobre su propio eje 
                                                        Matrix.CreateRotationY(rotacionYRueda ) * // segundo la rotamos sobre el plano XZ
                                                        mesh.ParentBone.Transform * // luego la hacemos heredar la transformacion del padre
                                                        //Matrix.CreateFromYawPitchRoll(0,-MathF.PI/2, 0) * 
                                                        wheelWorld); // pos ultimo
                }
                mesh.Draw();    
            }
        }
        
        public override Matrix getWorldMatrix()
        {
            return orientacion * Matrix.CreateTranslation(Posicion);
        }

        public void SetDireccion(Vector3 direccion)
        {
            //sacamos el angulo por el cual tendremos que rotar el auto hasta que se alinee con nuestro objetivo deseado
            Vector3 frenteAuto = new Vector3(orientacion.Backward.X, 0f, orientacion.Backward.Z);
            float relacion = Vector3.Dot(orientacion.Backward, direccion);
            anguloCorreccion = MathF.Acos(relacion);
            //si es positivo el PP con el ortogonal a izquierda del vector, entonces esta mirando a la izquierda, caso contrario esta mirando a la derecha
            //con eso sabemos para que sentido hay que rotar
            anguloCorreccion *= MathF.Sign(Vector3.Dot(frenteAuto, Utils.Matematicas.XZOrthogonal(direccion)));
            MaxRuedaRotacion = anguloCorreccion;
            this.direccion = direccion;
        }
        public void SetVelocidadAngular(float vAngular)
        {
            this.velocidadAngular = vAngular;
        }

    
    public void ApplyTexturesToShader()
    {
        //Thake that OOP >:D Nobody needs you when you have Ctrl + c and Ctrl + v
        efecto.Parameters["baseTexture"].SetValue(baseColorTexture);
        efecto.Parameters["metallicTexture"]?.SetValue(metallicTexture);
        efecto.Parameters["AOTexture"]?.SetValue(AOTexture);
        efecto.Parameters["normalTexture"]?.SetValue(normalTexture);
        
        

        efecto.Parameters["lightPosition"]?.SetValue(new Vector3(7000,3000,2000));

        efecto.Parameters["ambientColor"]?.SetValue(new Vector3(0.25f, 0.25f, 0.25f));
        efecto.Parameters["diffuseColor"]?.SetValue(new Vector3(0.75f, 0.75f, 0.75f));
        efecto.Parameters["specularColor"]?.SetValue(new Vector3(1f, 1f, 1f));

        efecto.Parameters["KAmbient"]?.SetValue(0.4f);
        efecto.Parameters["KDiffuse"]?.SetValue(1.5f);
        efecto.Parameters["KSpecular"]?.SetValue(0.25f);
        efecto.Parameters["shininess"]?.SetValue(4.0f);

        foreach ( ModelMesh mesh in modelo.Meshes )
        {
            foreach ( ModelMeshPart meshPart in mesh.MeshParts)
            {
                meshPart.Effect = efecto;
            }
        }
    }

    public void CargarModelo(Effect efecto, Model modelo, Texture2D[] texturas )
    {
        this.efecto = efecto;
        this.modelo = modelo;
        baseColorTexture = texturas[0];
        normalTexture = texturas[1];
        metallicTexture = texturas[2];
        roughnessTexture = texturas[3];
        AOTexture = texturas[4];
        emissionTexture = texturas[5];
        //si no quitamos eso, la cosa va a sobre escribir las texturas
        //this.ApplyTexturesToShader();

        // Asignar el shader a cada parte del modelo
        foreach (ModelMesh mesh in modelo.Meshes)
        {   
            //Console.WriteLine(mesh.Name);
            foreach (ModelMeshPart meshPart in mesh.MeshParts)
            {
                meshPart.Effect = efecto;
            }
        }
        BoundingVolume = MonoHelper.GenerarBoundingSphere(modelo, 1.8f);
    }

    public override void loadModel(string direccionModelo, string direccionEfecto, ContentManager contManager){
            //asignamos el modelo deseado
            modelo = contManager.Load<Model>(direccionModelo);
            //mismo caso para el efecto
            efecto = contManager.Load<Effect>(direccionEfecto);

            // Cargar texturas específicas
            baseColorTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_basecolor_0");
            normalTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_normal");
            metallicTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_metallic");
            roughnessTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_rougness");
            AOTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_ao");
            emissionTexture = contManager.Load<Texture2D>("Models/Auto/" + "Vehicle_emission");

            this.ApplyTexturesToShader();

            // Asignar el shader a cada parte del modelo
            foreach (ModelMesh mesh in modelo.Meshes)
            {   
                //Console.WriteLine(mesh.Name);
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efecto;
                }
            }
            //generamos una Bounding box para optimizar el juego un poco
            //estas cosas tienen demaciados poligonos
            BoundingVolume = MonoHelper.GenerarBoundingSphere(modelo);
        }

        public void  Mover( float fuerzaAAplicar, float deltaTime)
        {//la logica de a donde moverse, se maneja en otro lado, a qui solo nos movemos
            float scuareLimit = (velocidad * velocidad) * 100f;
            //aceleramos solo lo que se necesite
            if ( refACuerpo.Velocity.Linear.LengthSquared() < scuareLimit )
                refACuerpo.ApplyLinearImpulse(direccion.ToNumerics() * fuerzaAAplicar);
            const float epsilon = 0.01f;
            float VAInstantanea = velocidadAngular * deltaTime;
            //para alinear el auto
            if (anguloCorreccion > epsilon || anguloCorreccion < -epsilon)
            {
                VAInstantanea *= MathF.Sign(anguloCorreccion);
                refACuerpo.Velocity.Angular += Vector3.UnitY.ToNumerics() * VAInstantanea ;
                anguloCorreccion -= VAInstantanea;
            }
            //si estamos mirando hacia abajo
            if ( Vector3.Dot(Vector3.UnitY, orientacion.Up) < 0.85f)
                refACuerpo.Velocity.Angular += Vector3.Cross(orientacion.Up, Vector3.UnitY).ToNumerics() * VAInstantanea * 2f;
            //para ajustar las ruedas delanteras de a poco
            if ( MathF.Sign(rotacionRuedasDelanteras) < MathF.Sign(MaxRuedaRotacion) )
                rotacionRuedasDelanteras += MathF.Sign(MaxRuedaRotacion) * VAInstantanea * 2f;
            
            revolucionDeRuedas += VAInstantanea * 3f;
            revolucionDeRuedas = revolucionDeRuedas > MathF.Tau ? 0f : revolucionDeRuedas;
            refACuerpo.Velocity.Angular *= 0.98f;
            
        }

        public float DarAceleracion(float fuerz) => refACuerpo.LocalInertia.InverseMass * fuerz;
        public void CrearCollider(Simulation _simulacion, BufferPool _bufferpool, Vector2 posicionInicial){
            var compoundBuilder = new CompoundBuilder(_bufferpool, _simulacion.Shapes, 3);

            //var boxMainShape = new Box(280f, 100f, 500f);
            var capsuleMainShape = new Capsule(100, 400f);
            
            var capsuleMainLocalPose = new RigidPose(new Vector3(posicionInicial.X,100f,posicionInicial.Y).ToNumerics(), Quaternion.CreateFromYawPitchRoll(0f, MathF.PI/2, 0f).ToNumerics());
            //var capsuleMainLocalPose = new RigidPose(new Vector3(0f,120f,0f).ToNumerics());

            var ruedaShape = new Cylinder(35, 35);
            var ruedaDelanteraIzquierdaLocalPose = new RigidPose(posicionRuedaDelanteraIzquierda.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
            var ruedaDelanteraDerechaLocalPose = new RigidPose(posicionRuedaDelanteraDerecha.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
            var ruedaTraseraIzquierdaLocalPose = new RigidPose(posicionRuedaTraseraIzquierda.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());
            var ruedaTraseraDerechaLocalPose = new RigidPose(posicionRuedaTraseraDerecha.ToNumerics(), Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(MathF.PI/2)).ToNumerics());

            compoundBuilder.Add(capsuleMainShape, capsuleMainLocalPose, 5f);
            compoundBuilder.Add(ruedaShape, ruedaDelanteraIzquierdaLocalPose, .5f);
            compoundBuilder.Add(ruedaShape, ruedaDelanteraDerechaLocalPose, .5f);
            compoundBuilder.Add(ruedaShape, ruedaTraseraIzquierdaLocalPose, .5f);
            compoundBuilder.Add(ruedaShape, ruedaTraseraDerechaLocalPose, .5f);

            compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var compoundCenter);
            compoundBuilder.Reset();

            BodyHandle handlerDeCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateDynamic(compoundCenter + System.Numerics.Vector3.UnitY * 1000f, compoundInertia, _simulacion.Shapes.Add(new Compound(compoundChildren)), 0.01f));
            this.darCuerpo(handlerDeCuerpo);
        }

        public override void Mover(float fuerzaAAplicar)
        {
            throw new NotImplementedException();
        }
    }
}