﻿using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Control;
using Escenografia;
using TGC.MonoGame.Samples.Physics.Bepu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;



namespace TGC.MonoGame.TP
{
    /// <summary>
    ///     Esta es la clase principal del juego.
    ///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
    /// </summary>
    public class TGCGame : Game
    {
        public const string ContentFolder3D = "Models/";
        public const string ContentFolderEffects = "Effects/";
        public const string ContentFolderMusic = "Music/";
        public const string ContentFolderSounds = "Sounds/";
        public const string ContentFolderSpriteFonts = "SpriteFonts/";
        public const string ContentFolderTextures = "Textures/";

        private GraphicsDeviceManager Graphics { get; }
        private SpriteBatch SpriteBatch { get; set; }
        private Effect _basicShader;
        private Effect _vehicleShader;
        private Effect _terrenoShader;
        private Simulation _simulacion;
        //Control.Camera camara;
        Control.Camarografo camarografo;
        Escenografia.AutoJugador auto;
        AdministradorConos generadorConos;

        private Escenografia.Plano _plane { get; set; }
        private AdminUtileria Escenario;
        Primitiva Colisionable1;
        private Escenografia.Plataforma _plataforma { get; set;}
        private Turbo turboPowerUp;
        private BepuPhysics.Collidables.Box _box {get; set;}
        private PrismaRectangularEditable _boxVisual {get; set;}
        private BepuPhysics.Collidables.Box _hitboxAuto {get; set;}
        private BufferPool bufferPool;

        private Terreno terreno;

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        public TGCGame()
        {
            // Maneja la configuracion y la administracion del dispositivo grafico.
            Graphics = new GraphicsDeviceManager(this);
            
            Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
            Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
            // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
            // Carpeta raiz donde va a estar toda la Media.
            Content.RootDirectory = "Content";
            // Hace que el mous e sea visible.
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.
            // Apago el backface culling.
            // Esto se hace por un problema en el diseno del modelo del logo de la materia.
            // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
            var rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState; 

            bufferPool= new BufferPool();

            _simulacion = Simulation.Create(bufferPool, 
                                            new NarrowPhaseCallbacks(new SpringSettings(30f,1f)), 
                                            new Control.AyudanteSimulacion.PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));

            AyudanteSimulacion.simulacion = _simulacion;



            auto = new Escenografia.AutoJugador( Vector3.Backward,Convert.ToSingle(Math.PI)/2f, 15f);
            auto.Misil = new Misil();
            //seteamos una figura para el auto
            Box figuraAuto = new BepuPhysics.Collidables.Box(300f, 250f, 500f);
            BodyInertia autoInertia = figuraAuto.ComputeInertia(2f);
            TypedIndex referenciaAFigura = _simulacion.Shapes.Add(figuraAuto);
            //BodyHandle handlerDeCuerpo = AyudanteSimulacion.agregarCuerpoDinamico(new RigidPose( new Vector3(1f,0.5f,0f).ToNumerics() * 1500f),2f,referenciaAFigura,0.01f);
            BodyHandle handlerDeCuerpo = _simulacion.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose( new Vector3(1f,1f,0f).ToNumerics() * 1500f),
                autoInertia,
                new CollidableDescription(referenciaAFigura, 0.1f),
                new BodyActivityDescription(0.01f)
            ));
            //SEGUI LOS SAMPLES para agregar el auto, y comenzo a rotar segun el terreno

            auto.darCuerpo(handlerDeCuerpo);

            Colisionable1 = Primitiva.Prisma(new Vector3(100,100,100),- new Vector3(100,100,100));
            AyudanteSimulacion.agregarCuerpoStatico(new RigidPose(Vector3.UnitZ.ToNumerics() * -500f),
                                    _simulacion.Shapes.Add(new Sphere(100f)));

            AyudanteSimulacion.SetScenario();


            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 11000f, 150, 1100f);
            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1500f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
            Escenario = new AdminUtileria(-new Vector3(1f,0f,1f)*10000f, new Vector3(1f,0f,1f)*10000f);
            _plane = new Plano(GraphicsDevice, new Vector3(-11000, -200, -11000));

            terreno = new Terreno();
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            String[] efectos = {ContentFolderEffects + "BasicShader"};
            
            camarografo.loadTextFont(ContentFolderEffects, Content);

            _basicShader = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
            _vehicleShader = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            _terrenoShader = Content.Load<Effect>(ContentFolderEffects + "TerrenoShader");
            _plane.SetEffect(_basicShader);
            
            Plataforma.setGScale(15f);
            Escenario.loadPlataformas(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "BasicShader", Content);

            terreno.CargarTerreno(ContentFolder3D+"Terreno/height2",Content, 10f);
            terreno.SetEffect(_terrenoShader);

            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);
            Colisionable1.loadPrimitiva(Graphics.GraphicsDevice, _basicShader, Color.DarkCyan);
            auto.Misil.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "BasicShader", Content);
            
            terreno.CrearCollider(bufferPool, _simulacion, new Vector3(-10000f, 0f, -10000f));
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "BasicShader", Content, bufferPool, _simulacion);


            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Aca deberiamos poner toda la logica de actualizacion del juego.
            // Capturar Input teclado
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                //Salgo del juego.
                Exit();
            }
            
            auto.Mover(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            auto.Misil.ActualizarPowerUp(gameTime);
            //para que el camarografo nos siga siempre
            camarografo.setPuntoAtencion(auto.Posicion);
            camarografo.GetInputs();
            _simulacion.Timestep(1f/60f);//por ahora corre en el mismo thread que todo lo demas
            base.Update(gameTime);
        }

        private float Timer{get;set;}= 0f;
        protected override void Draw(GameTime gameTime)
        {
            // Aca deberiamos poner toda la logia de renderizado del juego.
            GraphicsDevice.Clear(Color.LightBlue);

            Escenario.Dibujar(camarografo);

            
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());

            terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.DarkGray);

            Colisionable1.dibujar(camarografo, new Vector3(0, 0, -500).ToNumerics());
            
            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
            auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            
            camarografo.DrawDatos(SpriteBatch);

            Timer += ((float)gameTime.TotalGameTime.TotalSeconds) % 1f;

        }

        /// <summary>
        ///     Libero los recursos que se cargaron en el juego.
        /// </summary>
        protected override void UnloadContent()
        {
            // Libero los recursos.
            Content.Unload();

            base.UnloadContent();
        }
    }
}