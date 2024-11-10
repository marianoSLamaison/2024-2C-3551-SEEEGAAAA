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
        private AdminUtileria Escenario;
        private BufferPool bufferPool;
        private ThreadDispatcher ThreadDispatcher;
        private Terreno terreno;

        RenderTarget2D shadowMap;

        Luz luz;

        private Primitiva prismaRectangular;

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

            bufferPool = new BufferPool();

            _simulacion = Simulation.Create(bufferPool, 
                                            new NarrowPhaseCallbacks(new SpringSettings(30f,1f)), 
                                            //new CarCallbacks() { Properties = carProperties},
                                            new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            //new DemoPoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));

            AyudanteSimulacion.simulacion = _simulacion;

            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);
            auto.Misil = new Misil();
            
            //seteamos un colisionador para el auto
            auto.CrearCollider(_simulacion, bufferPool);
            auto.Misil.CrearColliderMisil(_simulacion);

            AyudanteSimulacion.SetScenario();

            auto.Metralleta = new Metralleta();
            auto.Metralleta.CrearColliderMetralleta(_simulacion);
            
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 6000f, 100, 1000f);
            
            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
            Escenario = new AdminUtileria(new Vector3(-6100f,400f,-6100f), new Vector3(6100f,400f,6100f));

            terreno = new Terreno();

            shadowMap = new RenderTarget2D(GraphicsDevice,  4096,  4096, false, SurfaceFormat.Single, DepthFormat.Depth24);

            luz = new Luz(GraphicsDevice);

            prismaRectangular = Primitiva.Prisma(new Vector3(0, 0, 0), new Vector3(100, 100, 100));
            
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
            
            
            Plataforma.setGScale(15f*1.75f);
            Escenario.loadPlataformas(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "BasicShader", Content);
            Escenario.CrearColliders(bufferPool, _simulacion);

            terreno.CrearCollider(bufferPool, _simulacion, ThreadDispatcher);
            terreno.SetEffect(_terrenoShader, Content);

            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);

            auto.Misil.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "BasicShader", Content);
            auto.Metralleta.loadModel(ContentFolder3D + "Bullet/sphere", ContentFolderEffects + "BasicShader", Content);
            
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "BasicShader", Content, bufferPool, _simulacion);

            prismaRectangular.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);

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
            terreno.dibujarSombras(luz.lightView, luz.lightProjection);

            #endregion
            
            #region Default

            GraphicsDevice.SetRenderTarget(null);

            // Aca deberiamos poner toda la logia de renderizado del juego.
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.LightBlue, 1f, 0);
            
            Escenario.Dibujar(camarografo, GraphicsDevice);
            
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.camaraAsociada.posicion);

            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);
            terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.camaraAsociada.posicion, shadowMap);
            
            
            auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            auto.Metralleta.dibujar(camarografo.getViewMatrix(),camarografo.getProjectionMatrix(), Color.Red);

            prismaRectangular.dibujar(camarografo, new Vector3(6300f, 600f, 6100).ToNumerics()); //Caja en plataforma (abajo a la derecha)
            prismaRectangular.dibujar(camarografo, new Vector3(6775f, 600, -6073f).ToNumerics()); //Caja en plataforma (abajo a la izq)
            prismaRectangular.dibujar(camarografo, new Vector3(-7000f, 600f, -6000f).ToNumerics()); //Caja en plataforma (arriba a la derecha)
            prismaRectangular.dibujar(camarografo, new Vector3(-5442f, 600f, -5722f).ToNumerics()); //Caja en plataforma (arriba a la izq)

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