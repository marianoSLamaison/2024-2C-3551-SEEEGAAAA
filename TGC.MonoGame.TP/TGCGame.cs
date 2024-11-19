using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BepuUtilities;
using Control;
using Escenografia;
using TGC.MonoGame.Samples.Physics.Bepu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;
using System.Collections.Generic;



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
        private Effect _vehicleCombatShader;
        private Effect _terrenoShader;
        private Simulation _simulacion;
        //Control.Camera camara;
        Control.Camarografo camarografo;
        Escenografia.AutoJugador auto;
        AdministradorConos generadorConos;
        private AdminUtileria Escenario;
        private BufferPool bufferPool;
        private ThreadDispatcher ThreadDispatcher;
        private Terreno terreno;

        RenderTarget2D shadowMap;

        Luz luz;

        private Primitiva cajaPowerUp1;
        private Primitiva cajaPowerUp2;
        private Primitiva cajaPowerUp3;
        private Primitiva cajaPowerUp4;
        private AdministradorNPCs adminNPCs;
        private Dictionary<int, object> bodyHandleTags;
        private Dictionary<int, object> staticHandleTags;

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

            //DICCIONARIO PARA CHEQUEAR EN LAS CALLBACKS, SE PUEDEN AGREGAR TANTO STRINGS COMO LOS PROPIOS OBJETOS PARA HACERLES REFERENCIA, LA KEY ES EL VALUE DEL HANDLER
            bodyHandleTags = new Dictionary<int, object>(); 
            staticHandleTags = new Dictionary<int, object>();

            bufferPool = new BufferPool();

            

            var callbacks = new CustomNarrowPhaseCallbacks(new SpringSettings(30f,1f), bodyHandleTags, staticHandleTags);

            _simulacion = Simulation.Create(bufferPool, 
                                           callbacks, 
                                            new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));

            AyudanteSimulacion.simulacion = _simulacion;
            
            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);
            
            auto.CrearCollider(_simulacion, bufferPool);
            bodyHandleTags.Add(auto.handlerDeCuerpo.Value, "Auto");

            auto.Misil = new Misil();
            auto.Misil.CrearColliderMisil(_simulacion);
            bodyHandleTags.Add(auto.Misil.handlerCuerpo.Value, "Misil");

            AyudanteSimulacion.SetScenario();

            auto.Metralleta = new Metralleta();
            auto.Metralleta.CrearColliderMetralleta(_simulacion);
            
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 6000f, 1, 1000f);
            
            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
            Escenario = new AdminUtileria(new Vector3(-6100f,400f,-6100f), new Vector3(6100f,400f,6100f));

            terreno = new Terreno();
            terreno.CrearCollider(bufferPool, _simulacion, ThreadDispatcher);

            shadowMap = new RenderTarget2D(GraphicsDevice,  4096,  4096, false, SurfaceFormat.Single, DepthFormat.Depth24);

            luz = new Luz(GraphicsDevice);

            cajaPowerUp1 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp2 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp3 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp4 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));

            cajaPowerUp1.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp2.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp3.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,-6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp4.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,-6100)),_simulacion.Shapes.Add(new Box(200,200,200))));

            staticHandleTags.Add(cajaPowerUp1.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp2.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp3.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp4.staticHandle.Value, "Caja");

            Plataforma.setGScale(15f*1.75f);
            Escenario.CrearColliders(bufferPool, _simulacion);
            
            adminNPCs = new AdministradorNPCs();
            adminNPCs.generarAutos(2, 7000f, _simulacion, bufferPool, bodyHandleTags); //Baje la cantidad de autos (por las pruebas) y le paso el diccionario para agregar sus handlers

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            String[] efectos = {ContentFolderEffects + "VehicleShader"};
            
            camarografo.loadTextFont(ContentFolderEffects, Content);


            _basicShader = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
            _vehicleShader = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            //_vehicleCombatShader = Content.Load<Effect>(ContentFolderEffects + "VehicleCombatShader");
            _terrenoShader = Content.Load<Effect>(ContentFolderEffects + "TerrenoShader");
            
            Escenario.loadPlataformas(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "BasicShader", Content);

            terreno.SetEffect(_terrenoShader, Content);

            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);

            auto.Misil.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "BasicShader", Content);
            auto.Metralleta.loadModel(ContentFolder3D + "Bullet/sphere", ContentFolderEffects + "BasicShader", Content);
            
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "BasicShader", Content, bufferPool, _simulacion);

            cajaPowerUp1.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp2.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp3.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp4.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            
            adminNPCs.load(efectos, modelos, Content);

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
            auto.Metralleta.ActualizarPowerUp(gameTime);

            //adminNPCs.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            luz.BuildView(auto.Posicion);
            //para que el camarografo nos siga siempre
            camarografo.setPuntoAtencion(auto.Posicion);
            camarografo.GetInputs();
            _simulacion.Timestep(1/60f, ThreadDispatcher);//por ahora corre en el mismo thread que todo lo demas
            base.Update(gameTime);
        }

        private float Timer{get;set;}= 0f;
        protected override void Draw(GameTime gameTime)
        {

            #region Shadows

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            // Set the render target as our shadow map, we are drawing the depth into this texture
            GraphicsDevice.SetRenderTarget(shadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);

            auto.dibujarSombras(luz.lightView, luz.lightProjection);
            adminNPCs.drawSombras(luz.lightView, luz.lightProjection); //Agregue las sombras de los otros autos
            terreno.dibujarSombras(luz.lightView, luz.lightProjection);

            #endregion
            
            #region Default

            GraphicsDevice.SetRenderTarget(null);

            // Aca deberiamos poner toda la logica de renderizado del juego.
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.LightBlue, 1f, 0);
            
            Escenario.Dibujar(camarografo, GraphicsDevice);
            
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.GetFrustum());

            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);
            terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.camaraAsociada.posicion, shadowMap);
            
            auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            auto.Metralleta.dibujar(camarografo.getViewMatrix(),camarografo.getProjectionMatrix(), Color.Red);

            cajaPowerUp1.dibujar(camarografo, new Vector3(6100,600,6100).ToNumerics()); 
            cajaPowerUp2.dibujar(camarografo, new Vector3(-6100,600,6100).ToNumerics()); 
            cajaPowerUp3.dibujar(camarografo, new Vector3(6100,600,-6100).ToNumerics()); 
            cajaPowerUp4.dibujar(camarografo, new Vector3(-6100,600,-6100).ToNumerics()); 

            adminNPCs.draw(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);

            #endregion

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