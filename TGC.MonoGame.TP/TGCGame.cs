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
using Microsoft.Xna.Framework.Audio;



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
        RenderTarget2D shadowMap;
        RenderTarget2D StaticShadowMap { get; set;}// para no tener que dibujar todo el rato el terreno entero
        private CASOS estadoActualJuego;//controla que se muestra y que logica se corre

        Luz luz;

        private Primitiva cajaPowerUp1;
        private Primitiva cajaPowerUp2;
        private Primitiva cajaPowerUp3;
        private Primitiva cajaPowerUp4;
        private AdministradorNPCs adminNPCs;
        private FullScreenCuad ScreenCuad;//para dibujar todo lo preprocesado

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
            // Apago el backface culling. (Lo acabo de prender)
            // Esto se hace por un problema en el diseno del modelo del logo de la materia.
            // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
            #region Temas de graficos
            //creamos el estado de rasterizacion
            //que especifica datos de como dibujar la escena
            var rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = rasterizerState; 

            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
            shadowMap = new RenderTarget2D(GraphicsDevice,  4096,  4096, false, SurfaceFormat.Single, DepthFormat.Depth24);
            luz = new Luz(GraphicsDevice);
            //para tener ya todo iniciado
            MonoHelper.Initialize(GraphicsDevice);
            #endregion
            bufferPool = new BufferPool();

            _simulacion = Simulation.Create(bufferPool, 
                                            new NarrowPhaseCallbacks(new SpringSettings(30f,1f)), 
                                            //new CarCallbacks() { Properties = carProperties},
                                            new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            //new DemoPoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                            new SolveDescription(8,1));
            AyudanteSimulacion.simulacion = _simulacion;

            #region inicializacion de objetos
            auto = new AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);
            auto.CrearCollider(_simulacion, bufferPool);
            auto.Misil = new Misil();
            auto.Misil.CrearColliderMisil(_simulacion);
            auto.Metralleta = new Metralleta();
            auto.Metralleta.CrearColliderMetralleta(_simulacion);
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 6000f, 100, 1000f);
            Escenario = new AdminUtileria(10000f, 700f, 26f, _simulacion);//el escenario tiene 100.000 unidades de lado es como 200 autos de largo
            adminNPCs = new AdministradorNPCs();
            adminNPCs.generarAutos(5, 7000f, _simulacion, bufferPool);
            #endregion
            #region cosas experimentales
            cajaPowerUp1 = Primitiva.Prisma(new Vector3(50, 50, 50), -new Vector3(50, 50, 50));
            cajaPowerUp2 = Primitiva.Prisma(new Vector3(50, 50, 50), -new Vector3(50, 50, 50));
            cajaPowerUp3 = Primitiva.Prisma(new Vector3(50, 50, 50), -new Vector3(50, 50, 50));
            cajaPowerUp4 = Primitiva.Prisma(new Vector3(50, 50, 50), -new Vector3(50, 50, 50));
            _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,6100)),_simulacion.Shapes.Add(new Box(100,100,100))));
            _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,6100)),_simulacion.Shapes.Add(new Box(100,100,100))));
            _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (6100,600,-6100)),_simulacion.Shapes.Add(new Box(100,100,100))));
            _simulacion.Statics.Add(new StaticDescription(new RigidPose(new System.Numerics.Vector3 (-6100,600,-6100)),_simulacion.Shapes.Add(new Box(100,100,100))));

            ScreenCuad = new FullScreenCuad(GraphicsDevice);//para ya inicializar todo

            #endregion
            base.Initialize();
            //TODO: 
            //agregar el shader de terreno a los muros
            //ver por que el glitching en la pared
            //modificar forma del auto para que toque el suelo
        }

        protected override void LoadContent()
        {

            /*
            #region carga de datos
            // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            String[] efectos = {ContentFolderEffects + "VehicleShader"};

            Effect efectoDeAutos = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            Texture2D[] texturasParaAutos = {
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_basecolor_0"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_normal"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_metallic"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_rougness"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_ao"),
                Content.Load<Texture2D>("Models/Auto/" + "Vehicle_emission")
            };
            _basicShader = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
            _vehicleShader = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            //_vehicleCombatShader = Content.Load<Effect>(ContentFolderEffects + "VehicleCombatShader");
            _terrenoShader = Content.Load<Effect>(ContentFolderEffects + "TerrenoShader");
            #endregion
            //Plataforma.setGScale(15f*1.75f);
            #region verdadera carga de modelos
            //Nota esto toma el string, pero es por que maneja el tema del cargado de plataformas
            //en el metodo dado eso incluye un loop para cargar todas las plataformas con mismo modelo
            camarografo.loadTextFont(ContentFolderEffects, Content);
            Escenario.loadTerreno(_terrenoShader, Content);
            Escenario.CrearColliders(bufferPool, _simulacion);
            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);
            #endregion
            #region Testeo de cosas nuevas
            cajaPowerUp1.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp2.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp3.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
            cajaPowerUp4.loadPrimitiva(GraphicsDevice, _basicShader, Color.DarkGreen);
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
            Escenario.CrearColliders(bufferPool, _simulacion);
            auto.Misil.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "DeferredShadows", Content);
            auto.Metralleta.loadModel(ContentFolder3D + "Bullet/sphere", ContentFolderEffects + "DeferredShadows", Content);
            adminNPCs.load(renderer, modelos, texturasParaAutos, Content);
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
            }
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

            // Aca deberiamos poner toda la logia de renderizado del juego.
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.LightBlue, 1f, 0);


            //El trabajo de optimizarse se lo dejo a cada objeto, 
            //Debimos acordar estandares para los objetos antes
            Escenario.Dibujar(camarografo, shadowMap);
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);
            auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
            auto.Metralleta.dibujar(camarografo.getViewMatrix(),camarografo.getProjectionMatrix(), Color.Red);
            adminNPCs.draw(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), shadowMap);
            #endregion
            Timer += ((float)gameTime.TotalGameTime.TotalSeconds) % 1f;           
        }
        
        private void DibujarMenuInicial()
        {
            
        }
        private void DibujarScreenCuad(GameTime gameTime)
        {
            #region Calculo de shadowMap
            //NOTA: solo tenemos una luz que produce sombras,
            //mi computadora no puede manejar hacer 41 shadowmaps en tan poco tiempo
            #endregion
            #region Dibujado en Geometry buffer
            //primero hacemos el pass con tooodas las cosas de escena para dibujar sus BGbuffers
            GraphicsDevice.SetRenderTargets(ScreenCuad.positions, ScreenCuad.normals, ScreenCuad.albedo, ScreenCuad.especular);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.BlanchedAlmond, 1f, 0);
            //NOTA: Esta implementacion esta dejando conos y plataforma por igual a la merced de la luz de ambiente
            //y las luces normales, despues agregar una manera de resolver el problema 
            //un render target de 1x1 con un color solido tal vez podria ser una solucion
            //si no, se podria intentar modificar el shader para pasarle un color y hacer un calculo
            //simple con step para seleccionar el color si es que no esta lellendo nada desde memoria
            //despues agrego Texturas a la plataforma, ya que ya quedo arreglada en teoria ( ahora es olo una pieza )

            Escenario.LlenarGbuffer(camarografo);
            generadorConos.LlenarGbuffer(camarografo);
            adminNPCs.LlenarGbuffer(camarografo);

            auto.LlenarGbuffer(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
            #endregion
            #region Light pass para calcular iluminacion total
            //luego hacemos el pass con todas las luzes
            //Por ahora solo hay una, la idea es que cada auto tenga sus lucens con posicion
            //relativa a su centro, y simplemente sacarlas
            //de la lista de autos cuando llegue el momento
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Red, 1f, 0);
            List<luzConica> lucesEnEscena = new List<luzConica>{ Escenario.luna };
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
                DibujarMenuInicial();
                break;
                case CASOS.MENU_AYUDA:
                break;
            }
        }
    }
}