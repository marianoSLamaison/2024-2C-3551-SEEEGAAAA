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
        private Primitiva cuboTesteo;
        RenderTarget2D shadowMap;
        private CASOS estadoActualJuego;//controla que se muestra y que logica se corre

        Luz luz;

        private AdministradorNPCs adminNPCs;

        private FullScreenCuad ScreenCuad;//para dibujar todo lo preprocesado
        private Cuadro MenuInicio;
        private const float ALTURA_ESCENARIO = 700f;
        private const float LONGITUD_ESCENARIO = 10000f;
        private const float NEAR_PLANE = 10f, FAR_PLANE = 10000f;

        private Dictionary<int, object> bodyHandleTags;
        private Dictionary<int, object> staticHandleTags;
        
        private float tiempoTranscurrido = 0f;

        private Song playingMusic;
        private Song menuMusic;
        private float Puntuacion = 0f;
         
        private SpriteFont fuente; // Fuente para el texto


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

            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);

            var callbacks = new CustomNarrowPhaseCallbacks(new SpringSettings(30f,1f), bodyHandleTags, staticHandleTags, auto);

            _simulacion = Simulation.Create(bufferPool, 
                                           callbacks, 
                                            new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));
            AyudanteSimulacion.simulacion = _simulacion;

  
            
            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);
            auto.CrearCollider(_simulacion, bufferPool);
            bodyHandleTags.Add(auto.handlerDeCuerpo.Value, "Auto");

            auto.adminMisiles = new AdminMisiles(_simulacion, bodyHandleTags);
            auto.adminMetralleta = new AdminMetralleta(_simulacion, bodyHandleTags);
            
           
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 6000f, 100, 1000f);
            Escenario = new AdminUtileria(LONGITUD_ESCENARIO, ALTURA_ESCENARIO, 26f, _simulacion);//el escenario tiene 100.000 unidades de lado es como 200 autos de largo


            //inicializamos la cosa que dibujaa todos los menues
            MenuInicio = new Cuadro(SpriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            MenuInicio.loadBotones(new fRectangle(0.4f, 0.62f, 0.2f, 0.2f));
            InicializarJuego(CASOS.MENU_INICIAL);
            
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
            
            
            adminNPCs = new AdministradorNPCs();
            adminNPCs.generarAutos(2, 7000f, _simulacion, bufferPool, bodyHandleTags); //Baje la cantidad de autos (por las pruebas) y le paso el diccionario para agregar sus handlers
            //cuboTesteo = Primitiva.Prisma(-new Vector3(1,1,1) * 1000, new Vector3(1,1,1) * 200);
            //cuboTesteo.setearCuerpoPrisma(-new Vector3(1,1,1), new Vector3(1,1,1),
              //          (new Vector3(0f, 0.5f, 1f) * 5000).Length() * 0.5f * Vector3.Normalize(-new Vector3(0f, 0.5f, 1f)) + new Vector3(0f, 0.5f, 1f) * 5000);



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
            //Escenario.CrearColliders(bufferPool, _simulacion);
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

            ScreenCuad.LoadTargets(renderer);//tenemos los targets cargados
            Escenario.loadPlataformas(ContentFolder3D + "Plataforma/Plataformas", ContentFolderEffects + "DeferredShadows", Content);
            Escenario.loadTerreno(renderer, Content);
            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "DeferredShadows", Content);
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "DeferredShadows", Content, bufferPool, _simulacion);
            adminNPCs.load(renderer, modelos, texturasParaAutos, Content);
            MenuInicio.Load(Content);
            cajaPowerUp1.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp2.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp3.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp4.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);            
            //cuboTesteo.loadPrimitiva(GraphicsDevice, renderer, Color.Violet);

            
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
            MENU_AYUDA
        }
        private void MenuAyuda(){}
        private void Juego(GameTime gameTime)
        {
            
            auto.Mover((float)gameTime.ElapsedGameTime.TotalSeconds);
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
                break;
                case CASOS.MENU_INICIAL:
                MenuInicial(gameTime);
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

/*
            if (MediaPlayer.State != MediaState.Playing)
            {
                MediaPlayer.IsRepeating = true; //Loop
                MediaPlayer.Volume = 1f;     
                MediaPlayer.Play(playingMusic);
            }
            
            auto.Mover(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            
            

            
            auto.adminMisiles.ActualizarMisiles(gameTime);
            auto.adminMetralleta.ActualizarMetralleta(gameTime);
            */
            //auto.Misil.ActualizarPowerUp(gameTime);
            //auto.Metralleta.ActualizarPowerUp(gameTime);

            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,
                                                Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 
                                                NEAR_PLANE, FAR_PLANE);
            //camarografo = new Camarografo(new Vector3(1.5f, 1, 1) * 1000f,
            //Vector3.Zero, 
            //2000, 1500, 1, 10000);
            //shadowMap = new RenderTarget2D(GraphicsDevice,  4096,  4096, false, SurfaceFormat.Single, DepthFormat.Depth24);
            //luz = new Luz(GraphicsDevice);
            //para tener ya todo iniciado
           
/*
            tiempoTranscurrido += (float)gameTime.ElapsedGameTime.TotalSeconds;

            adminNPCs.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds), new Vector2(auto.Posicion.X, auto.Posicion.Z));
            luz.BuildView(auto.Posicion);
            //para que el camarografo nos siga siempre
            camarografo.setPuntoAtencion(auto.Posicion);
            camarografo.GetInputs();
            _simulacion.Timestep(1/60f, ThreadDispatcher);//por ahora corre en el mismo thread que todo lo demas
            base.Update(gameTime);
        }
        */

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
            }
            estadoActualJuego = casoDeInicio;
        }
        private void MenuInicial(GameTime gameTime)
        {
            //para tener a la camara mirando a un punto particular
            
            camarografo.rotatePuntoAtencion(MathF.Tau / 30f * Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            if ( Keyboard.GetState().IsKeyDown(Keys.Enter))
                CambiarCaso(CASOS.JUEGO);
            
            _simulacion.Timestep(1/30f);
            
        }
        private void DibujarJuego(GameTime gameTime)
        {
            #region DibujadoDeSombras

            //APunte: Si ves los shaders, veras que se esta usando cosas como ShadowmpaData.r
            //eso es por que como hay solo guarda cosas como prfundidad, solo se usa el dato de
            //r que es donde se guarda eso ( esto es asi por que a si esta diseñado )
            //luego para las cosas projectadas desde la luz, el sistema esta arreglado para que 
            //la distancia con respecto de la luz seguarde en .z y el resto de las coordenadas
            //sean la posicion en pantalla

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //seteamos el render target al shadowmap para que dibuje en este lo que necesitamos
            GraphicsDevice.SetRenderTarget(shadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);

            auto.dibujarSombras(luz.lightView, luz.lightProjection);

            Escenario.dibujarSombras(luz.lightView, luz.lightProjection);
            //adminNPCs.dibujarSombreas(luz,lightView, luz.lightProjection);
            #endregion  
            #region "Dibujos normales"

            GraphicsDevice.SetRenderTarget(null);

            // Aca deberiamos poner toda la logica de renderizado del juego.
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.LightBlue, 1f, 0);

            
            Escenario.Dibujar(camarografo, GraphicsDevice);
            
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.GetFrustum());



            //El trabajo de optimizarse se lo dejo a cada objeto, 
            //Debimos acordar estandares para los objetos antes
            Escenario.Dibujar(camarografo, shadowMap);
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);
            terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.camaraAsociada.posicion, shadowMap);
            
            //auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            auto.adminMisiles.dibujarMisiles(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            auto.adminMetralleta.dibujarBalas(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Red);

            //auto.Metralleta.dibujar(camarografo.getViewMatrix(),camarografo.getProjectionMatrix(), Color.Red);

            cajaPowerUp1.dibujar(camarografo, new Vector3(6100,600,6100).ToNumerics()); 
            cajaPowerUp2.dibujar(camarografo, new Vector3(-6100,600,6100).ToNumerics()); 
            cajaPowerUp3.dibujar(camarografo, new Vector3(6100,600,-6100).ToNumerics()); 
            cajaPowerUp4.dibujar(camarografo, new Vector3(-6100,600,-6100).ToNumerics()); 

            adminNPCs.draw(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);

            SpriteBatch.Begin();
            SpriteBatch.DrawString(fuente, $"Tiempo Sobreviviendo: {tiempoTranscurrido:F2} ", new Vector2(10, 20), Color.White);
            SpriteBatch.DrawString(fuente, $"Puntuacion : {auto.score:F2} ", new Vector2(1500, 20), Color.White);
            SpriteBatch.End();


            #endregion
            Timer += ((float)gameTime.TotalGameTime.TotalSeconds) % 1f;
        }
        private void DibujarMenuInicial(GameTime gameTime)
        {
            //tenemos que hacer esto por que si no el sprite batch vuelve todo transparente
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            GraphicsDevice.SetRenderTargets(ScreenCuad.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);


            Escenario.LlenarEfectsBuffer(camarografo);

            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            Escenario.LlenarGbuffer(camarografo);
            //mandamos todo el render al ultimo target, donde mesclaremos todo
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ camarografo.AmbientLight };
            ScreenCuad.Dibujar(GraphicsDevice, lucesEnEscena, camarografo.getViewMatrix());


            MenuInicio.Write();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            base.Draw(gameTime);
        }

        private void DibujarScreenCuad(GameTime gameTime)
        {
            //esto solo soporta hasta 4 render targes, a si que toca manejarlo a la antigua
            #region Datos Para Efectos
            GraphicsDevice.SetRenderTargets(ScreenCuad.ShadowMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
            Escenario.LlenarEfectsBuffer(camarografo);
            auto.LlenarEfectsBuffer(camarografo);
            #endregion
            #region Dibujado en Geometry buffer
            //primero hacemos el pass con tooodas las cosas de escena para dibujar sus BGbuffers
            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.BlanchedAlmond, 1f, 0);

            Escenario.LlenarGbuffer(camarografo);
            generadorConos.LlenarGbuffer(camarografo);
            adminNPCs.LlenarGbuffer(camarografo);
            //cuboTesteo.LlenarGbuffer(camarografo);

            auto.LlenarGbuffer(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
            #endregion
            #region Light pass para calcular iluminacion total
            //luego hacemos el pass con todas las luzes
            //Por ahora solo hay una, la idea es que cada auto tenga sus lucens con posicion
            //relativa a su centro, y simplemente sacarlas
            //de la lista de autos cuando llegue el momento
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ camarografo.AmbientLight };
            ScreenCuad.Dibujar(GraphicsDevice, lucesEnEscena, camarografo.getViewMatrix());
            
            #endregion
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
                break;
            }
        }
    }
}