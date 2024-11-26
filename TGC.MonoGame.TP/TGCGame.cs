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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Media;

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
        private Simulation _simulacion;
        //Control.Camera camara;
        Control.Camarografo camarografo;
        Escenografia.AutoJugador auto;
        AdministradorConos generadorConos;
        private AdminUtileria Escenario;
        private BufferPool bufferPool;
        private ThreadDispatcher ThreadDispatcher;
        private CASOS estadoActualJuego;//controla que se muestra y que logica se corre

        private AdministradorNPCs adminNPCs;

        private FullScreenCuad ScreenCuad;//para dibujar todo lo preprocesado
        private InicioMenu MenuInicio;
        private AyudaMenu MenuDeAyuda;
        private PantallaFinal PantallaFinal;
        private const float ALTURA_ESCENARIO = 500f;
        private const float LONGITUD_ESCENARIO = 10000f;
        private const float NEAR_PLANE = 1f, FAR_PLANE = 20000f;

        private Dictionary<int, object> bodyHandleTags;
        private Dictionary<int, object> staticHandleTags;
        
        private float tiempoRestante = 120f;

        private Song playingMusic;
        private Song menuMusic;
        private Song musicaActual;
        private float Puntuacion = 0f;

        private Primitiva cajaPowerUp1, cajaPowerUp2,cajaPowerUp3,cajaPowerUp4;
        private SpriteFont fuente; // Fuente para el texto

        private Contador toonTimer;

        private const int TIEMPO_MAXIMO = 180;


        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        public TGCGame()
        {
            // Maneja la configuracion y la administracion del dispositivo grafico.
            Graphics = new GraphicsDeviceManager(this);
            
            Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            Graphics.ToggleFullScreen();
            // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
            // Carpeta raiz donde va a estar toda la Media.
            Content.RootDirectory = "Content";
            // Hace que el mous e sea visible.
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            //esto es independiente del tipo de inicializacion
            var rasterizerState = new RasterizerState();

            rasterizerState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = rasterizerState; 
            MonoHelper.Initialize(GraphicsDevice);
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            ScreenCuad = new FullScreenCuad(GraphicsDevice);//para ya inicializar todo
            //para poder iniciar desde estados distintos
            //TODO: 
            //agregar el shader de terreno a los muros
            //ver por que el glitching en la pared
            //modificar forma del auto para que toque el suelo


            //DICCIONARIO PARA CHEQUEAR EN LAS CALLBACKS, SE PUEDEN AGREGAR TANTO STRINGS COMO LOS PROPIOS OBJETOS PARA HACERLES REFERENCIA, LA KEY ES EL VALUE DEL HANDLER
            bodyHandleTags = new Dictionary<int, object>(); 
            staticHandleTags = new Dictionary<int, object>();

            bufferPool = new BufferPool();

            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5);

            var callbacks = new CustomNarrowPhaseCallbacks(new SpringSettings(30f,1f), bodyHandleTags, staticHandleTags, auto);

            _simulacion = Simulation.Create(bufferPool, 
                                           callbacks, 
                                            new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));
            AyudanteSimulacion.simulacion = _simulacion;

            auto.CrearCollider(_simulacion, bufferPool);
            bodyHandleTags.Add(auto.handlerDeCuerpo.Value, "Auto");

            auto.adminMisiles = new AdminMisiles(_simulacion, bodyHandleTags);
            auto.adminMetralleta = new AdminMetralleta(_simulacion, bodyHandleTags);
           
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 6000f, 100, 1000f);
            Escenario = new AdminUtileria(LONGITUD_ESCENARIO, ALTURA_ESCENARIO, 26f, _simulacion);//el escenario tiene 100.000 unidades de lado es como 200 autos de largo


            //inicializamos la cosa que dibujaa todos los menues
            MenuInicio = new InicioMenu(SpriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            MenuInicio.loadBotonesMenuInicio();
            MenuDeAyuda = new AyudaMenu(SpriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            MenuDeAyuda.loadBotonesMenuAyuda();
            PantallaFinal = new PantallaFinal(SpriteBatch, new fRectangle(0.20f, 0.07f, 0.6f, 0.5f),GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            toonTimer = new Contador(TIEMPO_MAXIMO, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, SpriteBatch);

            InicializarJuego(CASOS.MENU_INICIAL);
            
            cajaPowerUp1 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp2 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp3 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));
            cajaPowerUp4 = Primitiva.Prisma(new Vector3(100, 100, 100), -new Vector3(100, 100, 100));

            cajaPowerUp1.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp2.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp3.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,-6100)),_simulacion.Shapes.Add(new Box(200,200,200))));
            cajaPowerUp4.staticHandle = _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,-6100)),_simulacion.Shapes.Add(new Box(200,200,200))));

            cajaPowerUp1.Pose = _simulacion.Statics.GetStaticReference( cajaPowerUp1.staticHandle).Pose;
            cajaPowerUp2.Pose = _simulacion.Statics.GetStaticReference( cajaPowerUp2.staticHandle).Pose;
            cajaPowerUp3.Pose = _simulacion.Statics.GetStaticReference( cajaPowerUp3.staticHandle).Pose;
            cajaPowerUp4.Pose = _simulacion.Statics.GetStaticReference( cajaPowerUp4.staticHandle).Pose;

            staticHandleTags.Add(cajaPowerUp1.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp2.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp3.staticHandle.Value, "Caja");
            staticHandleTags.Add(cajaPowerUp4.staticHandle.Value, "Caja");
            
            Escenario.CrearColliders(bufferPool, _simulacion);
            
            adminNPCs = new AdministradorNPCs();
            adminNPCs.generarAutos(2, 7000f, _simulacion, bufferPool, bodyHandleTags); //Baje la cantidad de autos (por las pruebas) y le paso el diccionario para agregar sus handlers

            base.Initialize();
        }

        protected override void LoadContent()
        {

            /*
            #region carga de datos
            // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            String[] efectos = {ContentFolderEffects + "VehicleShader"};

            _basicShader = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
            _vehicleShader = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            //_vehicleCombatShader = Content.Load<Effect>(ContentFolderEffects + "VehicleCombatShader");
            _terrenoShader = Content.Load<Effect>(ContentFolderEffects + "TerrenoShader");



            #endregion
            */
            //cargamos las bases para la prueba
            //Escenario.loadPlataformas(ContentFolder3D + "Plataforma/Plataformas", ContentFolderEffects + "BasicShader", Content);
            
            //camarografo.loadTextFont(ContentFolderEffects, Content);
            //Nota importante:
            //como todos los elementos comparten shader, 
            //todos los elementos tienen que setear sus texturas antes de ser dibujados
            //no durante el load
            //si lo hacen durante el load, van a sobre escribir la textura en el shader
            //y vas a tener algo como campo de pasto morado que se ve horrible


            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            Texture2D[] texturasParaAutos = {
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_basecolor_0"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_normal"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_metallic"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_rougness"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_ao"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_emission")
            };
            Effect renderer = Content.Load<Effect>(ContentFolderEffects + "DeferredShadows");
            Effect rendererColor = Content.Load<Effect>(ContentFolderEffects + "DeferredShadowsColor");

            ScreenCuad.LoadTargets(renderer);//tenemos los targets cargados
            
            Escenario.loadPlataformas(ContentFolder3D + "Plataforma/Plataformas", ContentFolderEffects + "DeferredShadows", Content);
            Escenario.loadTerreno(renderer, Content);
            
            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "DeferredShadows", Content);
            
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "DeferredShadowsColor", Content, bufferPool, _simulacion);
            adminNPCs.load(renderer, modelos, texturasParaAutos, Content);
            
            auto.adminMisiles.loadMisiles(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "DeferredShadowsColor", Content);
            auto.adminMetralleta.loadMetralleta(ContentFolder3D + "Bullet/sphere", ContentFolderEffects + "DeferredShadowsColor", Content);

            MenuInicio.Load(Content);
            MenuDeAyuda.Load(Content);
            PantallaFinal.Load(Content);

            toonTimer.Load(Content);
            
            cajaPowerUp1.loadPrimitiva(GraphicsDevice, renderer, Color.DarkGreen, Content);
            cajaPowerUp2.loadPrimitiva(GraphicsDevice, renderer, Color.DarkGreen, Content);
            cajaPowerUp3.loadPrimitiva(GraphicsDevice, renderer, Color.DarkGreen, Content);
            cajaPowerUp4.loadPrimitiva(GraphicsDevice, renderer, Color.DarkGreen, Content);
            
            fuente = Content.Load<SpriteFont>("debugFont");

            playingMusic = Content.Load<Song>("Danger Zone - Instrumental  TOP GUN");
            menuMusic = Content.Load<Song>("Gran Turismo 4 Soundtrack - Race Menu 2");

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            ActualizarJuego(gameTime);
            base.Update(gameTime);
        }

        private float Timer{get;set;}= 0f;
        protected override void Draw(GameTime gameTime)
        {        
            Dibujar(gameTime);
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
        private enum CASOS
        {
            JUEGO,
            MENU_INICIAL,
            MENU_AYUDA,

            PANTALLA_FINAL
        }
        private void MenuAyuda(){}
        private void Juego(GameTime gameTime)
        {

            if (musicaActual != playingMusic)
            {
                musicaActual = playingMusic;
                MediaPlayer.IsRepeating = true; //Loop
                MediaPlayer.Volume = 1f;     
                MediaPlayer.Play(playingMusic);
            }

            if(toonTimer.time < 0 || Keyboard.GetState().IsKeyDown(Keys.D0)){
                PantallaFinal.Victoria = false;
                CambiarCaso(CASOS.PANTALLA_FINAL);

                //CambiarCaso(CASOS.DERROTA);
            }else{
                toonTimer.time -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if(auto.score == 20 || Keyboard.GetState().IsKeyDown(Keys.D9)){
                PantallaFinal.Victoria = true;
                CambiarCaso(CASOS.PANTALLA_FINAL);
                //CambiarCaso(CASOS.VICTORIA);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Back))
            {
                //Salgo del juego.
                Exit();
            }
            if ( Keyboard.GetState().IsKeyDown(Keys.P))
                CambiarCaso(CASOS.MENU_AYUDA);

            
            
            auto.Mover((float)gameTime.ElapsedGameTime.TotalSeconds);

            auto.adminMisiles.ActualizarMisiles(gameTime);
            auto.adminMetralleta.ActualizarMetralleta(gameTime);

            adminNPCs.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds), new Vector2(auto.Posicion.X, auto.Posicion.Z));

            

            //Console.WriteLine(camarografo.getProjectionMatrix() + "\n\n" + auto.Posicion);
            camarografo.setPuntoAtencion(auto.Posicion);
            //cuboTesteo.pos = Vector3.Lerp(camarografo.camaraLuz.PuntoAtencion, camarografo.camaraLuz.posicion, MathF.Pow(MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds), 2));
            _simulacion.Timestep(1f/60f, ThreadDispatcher);
        }

        private void ActualizarJuego(GameTime gameTime)
        {
            switch ( estadoActualJuego )
            {
                case CASOS.JUEGO:
                Juego(gameTime);
                break;
                case CASOS.MENU_AYUDA:
                UpdateMenuDeAyuda(gameTime);
                break;
                case CASOS.MENU_INICIAL:
                MenuInicial(gameTime);
                break;
                case CASOS.PANTALLA_FINAL:
                UpdatePantallaFinal(gameTime);
                break;
            }
        }

        private void CambiarCaso(CASOS nuevoCaso)
        {
            if ( estadoActualJuego == CASOS.MENU_INICIAL && nuevoCaso == CASOS.JUEGO)
            {
                estadoActualJuego = CASOS.JUEGO;
                InicializarCasoJuego();
            }
            else if ( estadoActualJuego == CASOS.JUEGO && nuevoCaso == CASOS.MENU_AYUDA)
            {
                estadoActualJuego = CASOS.MENU_AYUDA;
            }
            else if ( estadoActualJuego == CASOS.MENU_AYUDA && nuevoCaso == CASOS.JUEGO)
            {
                estadoActualJuego = CASOS.JUEGO;
                camarografo.setPuntoAtencion(auto.Posicion);
            }else if ( estadoActualJuego == CASOS.JUEGO && nuevoCaso == CASOS.PANTALLA_FINAL)
            {
                estadoActualJuego = CASOS.PANTALLA_FINAL;
            }

        }

        private void InicializarCasoMenuInicial()
        {
            camarografo = new Control.Camarografo(new Vector3(0f, ALTURA_ESCENARIO + 300f, 0f),
                                                Vector3.Left * LONGITUD_ESCENARIO / 5f + Vector3.Up * ALTURA_ESCENARIO, 
                                                GraphicsDevice.Viewport.AspectRatio, 
                                                NEAR_PLANE, FAR_PLANE);
            
        }
        private void InicializarCasoJuego()
        {
            // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.
            // Apago el backface culling. (Lo acabo de prender)
            // Esto se hace por un problema en el diseno del modelo del logo de la materia.
            // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
            //creamos el estado de rasterizacion
            //que especifica datos de como dibujar la escena

            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,
                                                Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 
                                                NEAR_PLANE, FAR_PLANE);

        }
        private void InicializarJuego(CASOS casoDeInicio)
        {
            switch(casoDeInicio)
            {
                case CASOS.JUEGO:
                InicializarCasoJuego();
                break;
                case CASOS.MENU_INICIAL:
                InicializarCasoMenuInicial();
                break;
                case CASOS.MENU_AYUDA:
                break;
                case CASOS.PANTALLA_FINAL:
                InicializarPantallaFinal();
                break;
            }
            estadoActualJuego = casoDeInicio;
        }

        private bool start = false;

        private void MenuInicial(GameTime gameTime)
        {
            //para tener a la camara mirando a un punto particular
            
            camarografo.rotatePuntoAtencion(MathF.Tau / 30f * Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            if ( Keyboard.GetState().IsKeyDown(Keys.Enter) && !start)
            {
                start = true;
                MenuInicio.Botones[0].estaSeleccionado = true;
            }
            if ( Keyboard.GetState().IsKeyUp(Keys.Enter) && start)
            {
                start = false;
                MenuInicio.Botones[0].estaSeleccionado = false;
                MediaPlayer.Stop();
                CambiarCaso(CASOS.JUEGO);
            }

            if (musicaActual != menuMusic)
            {
                musicaActual = menuMusic;
                MediaPlayer.IsRepeating = true; //Loop
                MediaPlayer.Volume = 1f;     
                MediaPlayer.Play(menuMusic);
            }
            
            //_simulacion.Timestep(1/30f);
            
        }
        
        private void DibujarMenuInicial(GameTime gameTime)
        {
           //esto solo soporta hasta 4 render targes, a si que toca manejarlo a la antigua
            GraphicsDevice.SetRenderTargets(ScreenCuad.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            
            Escenario.LlenarEfectsBuffer(camarografo);
            auto.LlenarEfectsBuffer(camarografo);
            generadorConos.LlenarEfectsBuffer(camarografo);
            //adminNPCs.LlenarEfectsBuffer(camarografo);

            //primero hacemos el pass con tooodas las cosas de escena para dibujar sus BGbuffers
            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.BlanchedAlmond, 1f, 0);

            Escenario.LlenarGbuffer(camarografo);
            generadorConos.LlenarGbuffer(camarografo);
            //adminNPCs.LlenarGbuffer(camarografo);
            //auto.adminMetralleta.LlenarGbuffer(camarografo);
            //auto.adminMisiles.LlenarGbuffer(camarografo);

            //cuboTesteo.LlenarGbuffer(camarografo);

            //auto.LlenarGbuffer(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());

            //luego hacemos el pass con todas las luzes
            //Por ahora solo hay una, la idea es que cada auto tenga sus lucens con posicion
            //relativa a su centro, y simplemente sacarlas
            //de la lista de autos cuando llegue el momento
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ camarografo.AmbientLight};
            ScreenCuad.Dibujar(GraphicsDevice, lucesEnEscena, camarografo.getViewMatrix());

            MenuInicio.Write();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            base.Draw(gameTime);
        }
        private void DibujarMundoBasic()
        {
            //esto solo soporta hasta 4 render targes, a si que toca manejarlo a la antigua
            GraphicsDevice.SetRenderTargets(ScreenCuad.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            
            Escenario.LlenarEfectsBuffer(camarografo);
            auto.LlenarEfectsBuffer(camarografo);
            generadorConos.LlenarEfectsBuffer(camarografo);
            //adminNPCs.LlenarEfectsBuffer(camarografo);

            //primero hacemos el pass con tooodas las cosas de escena para dibujar sus BGbuffers
            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.BlanchedAlmond, 1f, 0);

            Escenario.LlenarGbuffer(camarografo);
            generadorConos.LlenarGbuffer(camarografo);
            //adminNPCs.LlenarGbuffer(camarografo);
            //auto.adminMetralleta.LlenarGbuffer(camarografo);
            //auto.adminMisiles.LlenarGbuffer(camarografo);
            //cuboTesteo.LlenarGbuffer(camarografo);

            //auto.LlenarGbuffer(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());

            //luego hacemos el pass con todas las luzes
            //Por ahora solo hay una, la idea es que cada auto tenga sus lucens con posicion
            //relativa a su centro, y simplemente sacarlas
            //de la lista de autos cuando llegue el momento
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ camarografo.AmbientLight};
            ScreenCuad.Dibujar(GraphicsDevice, lucesEnEscena, camarografo.getViewMatrix());
        }

        private void DibujarMenuAyuda()
        {
            /*Dibujar esta cosa
                TITULO
            -----------------
            |Boton|/(TTTTT)
            |Boton|/(EEEEE)
                /(XXXXX)
                /(TTTTT)
                /(OOOOO)
            */           //dibujar un rectangulo, dibujar
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            //dibujo de todo
            //Mundo
            DibujarMundoBasic();
            
            MenuDeAyuda.Write();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DibujarScreenCuad(GameTime gameTime)
        {
            //esto solo soporta hasta 4 render targes, a si que toca manejarlo a la antigua
            #region Datos Para Efectos
            GraphicsDevice.SetRenderTargets(ScreenCuad.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            auto.LlenarEfectsBuffer(camarografo);
            generadorConos.LlenarEfectsBuffer(camarografo);
            adminNPCs.LlenarEfectsBuffer(camarografo);

            cajaPowerUp1.LlenarEfectsBuffer(camarografo);
            cajaPowerUp2.LlenarEfectsBuffer(camarografo);
            cajaPowerUp3.LlenarEfectsBuffer(camarografo);
            cajaPowerUp4.LlenarEfectsBuffer(camarografo);
            Escenario.LlenarEfectsBuffer(camarografo);

            #endregion
            #region Dibujado en Geometry buffer
            //primero hacemos el pass con tooodas las cosas de escena para dibujar sus BGbuffers
            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.BlanchedAlmond, 1f, 0);
            
            auto.LlenarGbuffer(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
            generadorConos.LlenarGbuffer(camarografo);
            adminNPCs.LlenarGbuffer(camarografo);
            auto.adminMetralleta.LlenarGbuffer(camarografo);
            auto.adminMisiles.LlenarGbuffer(camarografo);

            cajaPowerUp1.LlenarGbuffer(camarografo);
            cajaPowerUp2.LlenarGbuffer(camarografo);
            cajaPowerUp3.LlenarGbuffer(camarografo);
            cajaPowerUp4.LlenarGbuffer(camarografo);

            Escenario.LlenarGbuffer(camarografo);

            //cuboTesteo.LlenarGbuffer(camarografo);

            #endregion
            #region Light pass para calcular iluminacion total
            //luego hacemos el pass con todas las luzes
            //Por ahora solo hay una, la idea es que cada auto tenga sus lucens con posicion
            //relativa a su centro, y simplemente sacarlas
            //de la lista de autos cuando llegue el momento
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ camarografo.AmbientLight};
            ScreenCuad.Dibujar(GraphicsDevice, lucesEnEscena, camarografo.getViewMatrix());

            toonTimer.Dibujar();

            SpriteBatch.Begin();
            SpriteBatch.DrawString(fuente, $"Cantidad Misiles: {auto.cantidadMisiles} ", new Vector2(20, Graphics.PreferredBackBufferHeight - 100), Color.Orange);
            SpriteBatch.DrawString(fuente, $"Cantidad Balas: {auto.cantidadBalas} ", new Vector2(20, Graphics.PreferredBackBufferHeight - 150), Color.Orange);
            SpriteBatch.DrawString(fuente, $"Score : {auto.score} ", new Vector2(Graphics.PreferredBackBufferWidth - 160, Graphics.PreferredBackBufferHeight - 100), Color.Orange);
            SpriteBatch.DrawString(fuente, $"Presiona P para el Menu de ayuda", new Vector2(Graphics.PreferredBackBufferWidth/2 - 190, 80), Color.Orange);
            SpriteBatch.End();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            
            #endregion
        }

        enum ButtonSelected
        {
            NONE = 0, FIRST = 1, SECOND = 2
        }
        ButtonSelected currentButton = ButtonSelected.NONE, oldCurrent = ButtonSelected.NONE;
        bool keyPressed = false;Keys keyHeld = Keys.None;
        private ButtonSelected increaseButton(ButtonSelected current)
        {
            switch(current)
            {
                case ButtonSelected.NONE:
                return ButtonSelected.FIRST;
                case ButtonSelected.FIRST:
                return ButtonSelected.SECOND;
                case ButtonSelected.SECOND:
                return ButtonSelected.FIRST;
            }
            return current;
        }
        private ButtonSelected decreaseButton(ButtonSelected current)
        {
            switch(current)
            {
                case ButtonSelected.SECOND:
                return ButtonSelected.FIRST;
                case ButtonSelected.FIRST:
                return ButtonSelected.SECOND;
                case ButtonSelected.NONE:
                return ButtonSelected.SECOND;
            }
            return current;
        }

        private void UpdateMenuDeAyuda(GameTime gameTime)
        {
            // el tiempo no pasa en este, osea que no hay _simulation.timestep()

            camarografo.rotatePuntoAtencion(MathF.Tau / 30f * Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            
            
            // Handle Escape key to change game state
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                CambiarCaso(CASOS.JUEGO);
                currentButton = ButtonSelected.NONE;
                camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,
                                                Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 
                                                NEAR_PLANE, FAR_PLANE);
            }
            
            // Handle key press for W and S
            if (Keyboard.GetState().IsKeyDown(Keys.W) && keyHeld == Keys.None)
            {
                currentButton = increaseButton(currentButton);
                keyHeld = Keys.W;
            } 
            else if (Keyboard.GetState().IsKeyDown(Keys.S) && keyHeld == Keys.None)
            {
                currentButton = decreaseButton(currentButton);
                keyHeld = Keys.S;   
            }

            // Update button selection state
            if (oldCurrent != currentButton)
            {
                if (oldCurrent != ButtonSelected.NONE)
                {
                    MenuDeAyuda.Botones[(int)oldCurrent - 1].estaSeleccionado = false;
                }

                oldCurrent = currentButton;

                if (currentButton != ButtonSelected.NONE)
                {
                    MenuDeAyuda.Botones[(int)currentButton - 1].estaSeleccionado = true;
                }
            }

            // Reset keyHeld if no longer pressed
            if (keyHeld != Keys.None && !Keyboard.GetState().IsKeyDown(keyHeld))
            {
                keyHeld = Keys.None;
            }
        }

        private void UpdatePantallaFinal(GameTime gameTime)
        {
            camarografo.rotatePuntoAtencion(MathF.Tau / 30f * Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            if (Keyboard.GetState().IsKeyDown(Keys.Back))
            {
                //Salgo del juego.
                Exit();
            }

        }
        private void DibujarPantallaFinal()
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            //dibujo de todo
            //Mundo
            DibujarMundoBasic();
            
            PantallaFinal.Write();//esto esta OK
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
        }
        private void InicializarPantallaFinal()
        {
            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,
                                                Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 
                                                NEAR_PLANE, FAR_PLANE);
        }

        private void Dibujar( GameTime gameTime )
        {
            switch ( estadoActualJuego )
            {
                case CASOS.JUEGO:
                //DibujarJuego(gameTime);
                DibujarScreenCuad(gameTime);
                break;
                case CASOS.MENU_INICIAL:
                DibujarMenuInicial(gameTime);
                break;
                case CASOS.MENU_AYUDA:
                DibujarMenuAyuda();
                break;
                case CASOS.PANTALLA_FINAL:
                DibujarPantallaFinal();
                break;
            }
        }
    }
}