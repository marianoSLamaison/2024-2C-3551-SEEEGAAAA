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

            private Escenografia.Plano _plane { get; set; }
            private AdminUtileria Escenario;
            Primitiva Colisionable1;
            private Escenografia.Plataforma _plataforma { get; set;}
            private Turbo turboPowerUp;
            private BepuPhysics.Collidables.Box _box {get; set;}
            private PrismaRectangularEditable _boxVisual {get; set;}
            private BepuPhysics.Collidables.Box _hitboxAuto {get; set;}
            private BufferPool bufferPool;
            private ThreadDispatcher ThreadDispatcher;

            private Terreno terreno;

            public Luz luz;

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

                var carProperties = new CollidableProperty<CarBodyProperties>();

                _simulacion = Simulation.Create(bufferPool, 
                                                new NarrowPhaseCallbacks(new SpringSettings(30f,1f)), 
                                                //new CarCallbacks() { Properties = carProperties},
                                                new PoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                                //new DemoPoseIntegratorCallbacks(new Vector3(0f, -1000f, 0f).ToNumerics()),
                                                new SolveDescription(8,1));

                AyudanteSimulacion.simulacion = _simulacion;

                auto = new Escenografia.AutoJugador( Vector3.Backward,(Convert.ToSingle(Math.PI)/2f) * 5, 15f);
                auto.Misil = new Misil();
                
                //seteamos un colisionador para el auto
                auto.CrearCollider(_simulacion, bufferPool);
                auto.Misil.CrearColliderMisil(_simulacion);
        
                //Colisionable1 = Primitiva.Prisma(new Vector3(100,100,100),- new Vector3(100,100,100));
                //AyudanteSimulacion.agregarCuerpoStatico(new RigidPose(Vector3.UnitZ.ToNumerics() * -500f),
                //                        _simulacion.Shapes.Add(new Sphere(100f)));

                AyudanteSimulacion.SetScenario();

                auto.Metralleta = new Metralleta();
                auto.Metralleta.CrearColliderMetralleta(_simulacion);
                
                generadorConos = new AdministradorConos();
                generadorConos.generarConos(Vector3.Zero, 11000f, 100, 1100f);
                camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1000f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
                Escenario = new AdminUtileria(-new Vector3(1f,0f,1f)*8500f, new Vector3(1f,0f,1f)*8500f);
                luz = new Luz(GraphicsDevice, new Vector3(7100f, 1000f,7100f), new Vector3(-1f, -1f, 1f), Color.White, 1);
                //_plane = new Plano(GraphicsDevice, new Vector3(-11000, -200, -11000));

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
                
                Plataforma.setGScale(15f);
                Escenario.loadPlataformas(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "TerrenoShader", Content);
                //Escenario.CrearColliders(bufferPool, _simulacion);

                //terreno.CargarTerreno(ContentFolder3D+"Terreno/height2",Content, 10f);
                //terreno.CrearCollider(bufferPool, _simulacion, new Vector3(-10000f, 0f, -10000f));
                terreno.CrearCollider(bufferPool, _simulacion, ThreadDispatcher);
                terreno.SetEffect(_terrenoShader, Content);

                auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);
                
                //Colisionable1.loadPrimitiva(Graphics.GraphicsDevice, _basicShader, Color.DarkCyan);
                auto.Misil.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "BasicShader", Content);
                //auto.Metralleta.loadModel(ContentFolder3D + "Misil/Misil", ContentFolderEffects + "BasicShader", Content);
                auto.Metralleta.loadModel(ContentFolder3D + "Bullet/sphere", ContentFolderEffects + "BasicShader", Content);
                luz.cargarLuz(ContentFolderEffects+"BasicShader", ContentFolderEffects+"TerrenoShader", Content);
                generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "TerrenoShader", Content, bufferPool, _simulacion);

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
                //para que el camarografo nos siga siempre
                camarografo.setPuntoAtencion(auto.Posicion);
                camarografo.GetInputs();
                _simulacion.Timestep(1/60f, ThreadDispatcher);//por ahora corre en el mismo thread que todo lo demas
                base.Update(gameTime);
            }

            private float Timer{get;set;}= 0f;
            protected override void Draw(GameTime gameTime)
            {
                // Aca deberiamos poner toda la logia de renderizado del juego.
                GraphicsDevice.Clear(Color.LightBlue);

                Escenario.Dibujar(camarografo);

                generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());
                terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);

                //Colisionable1.dibujar(camarografo, new Vector3(0, 0, -500).ToNumerics());
                
                auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
                auto.Misil.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Cyan);
                auto.Metralleta.dibujar(camarografo.getProjectionMatrix(),camarografo.getProjectionMatrix(), Color.Red);
                //luz.SetLightEffect(_terrenoShader, _basicShader);
                _terrenoShader.Parameters["lightPosition"].SetValue(luz.getPosition());
                luz.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
 
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