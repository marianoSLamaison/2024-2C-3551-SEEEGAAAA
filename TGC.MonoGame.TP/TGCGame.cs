﻿using System;
using System.Security.Cryptography.X509Certificates;
using Control;
using Escenografia;
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
        private Effect _boxShader;
        

        //Control.Camera camara;
        Control.Camarografo camarografo;
        Escenografia.AutoJugador auto;
        Control.AdministradorNPCs generadorPrueba;

        AdministradorConos generadorConos;

        
        Escenografia.Primitiva cuadrado;


        private Escenografia.Plano _plane { get; set; }
        private PrismaRectangularEditable _edificio {get; set;}
        private Model _plant { get; set; }

        private Cono _cono { get; set; }
        private Rampa _rampa { get; set; }
        private Cilindro _cilindro { get; set; }
        private Palmera _palmera { get; set; }

        private Terreno terreno;

        private AdminUtileria Escenario;
        private Escenografia.Plataforma _plataforma { get; set;}


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

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
        /// </summary>
        protected override void Initialize()
        {
            // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.
            // Apago el backface culling.
            // Esto se hace por un problema en el diseno del modelo del logo de la materia.
            // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
            var rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState; 

            //generadorPrueba = new Control.AdministradorNPCs();
            //generadorPrueba.generadorNPCsV2(Vector3.Zero, 16000f, 50);
            generadorConos = new AdministradorConos();
            generadorConos.generarConos(Vector3.Zero, 16000f, 200);

            auto = new Escenografia.AutoJugador(Vector3.Zero, Vector3.Backward, 1000f, Convert.ToSingle(Math.PI)/3f);
            auto.setLimites(-new Vector3(1f,1f,1f)*10000f, new Vector3(1f,1f,1f)*10000f);
            camarografo = new Control.Camarografo(new Vector3(1f,1f,1f) * 1500f,Vector3.Zero, GraphicsDevice.Viewport.AspectRatio, 1f, 6000f);
            Escenario = new AdminUtileria(-new Vector3(1f,0f,1f)*10000f, new Vector3(1f,0f,1f)*10000f);
            _plane = new Plano(GraphicsDevice, new Vector3(-11000, -200, -11000));
            terreno = new Terreno();

            //_plant = new Model(GraphicsDevice, );
            //_edificio = new PrismaRectangularEditable(GraphicsDevice, new Vector3(200f, 500f, 200f));
            //_cono = new Cono(new Vector3(0, 0 , 1000));
            //_rampa = new Rampa(GraphicsDevice, new Vector3(200f, 500f, 500f), Vector3.Backward * -1000f);
            //_cilindro = new Cilindro(GraphicsDevice, 2, 12); //Cilindro(graphics, radio  alto);
            //_palmera = new Palmera(GraphicsDevice, new Vector3(0, 0, 1000));
            //_plataforma = new Plataforma(Convert.ToSingle(Math.PI / 2f), new Vector3(1000f,0f,1000f));
            //Escenografia.Plataforma.setGScale(10f);
            base.Initialize();
        }

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
        ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
        ///     que podemos pre calcular para nuestro juego.
        /// </summary>
        protected override void LoadContent()
        {
            // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            String[] modelos = {ContentFolder3D + "Auto/RacingCar"};
            String[] efectos = {ContentFolderEffects + "BasicShader"};
            auto.loadModel(ContentFolder3D + "Auto/RacingCar", ContentFolderEffects + "VehicleShader", Content);

            cuadrado = Escenografia.Primitiva.RegPoligon(Vector3.Zero, 32, 1f);
            cuadrado.loadPrimitiva(GraphicsDevice, Content.Load<Effect>(efectos[0]), Color.Brown);

            _basicShader = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
            _vehicleShader = Content.Load<Effect>(ContentFolderEffects + "VehicleShader");
            _plane.SetEffect(_basicShader);
            //_boxShader = Content.Load<Effect>(ContentFolderEffects + "BoxShader");
            //Texture2D textureBox = Content.Load<Texture2D>(ContentFolder3D + "Cilindro/caja-madera-1");

            //_plataforma.loadModel(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "BasicShader", Content);
            Plataforma.setGScale(10f);
            Escenario.loadPlataformas(ContentFolder3D+"Plataforma/Plataforma", ContentFolderEffects + "BasicShader", Content);
            //generadorPrueba.loadModelosAutos(modelos, efectos, Content);
            generadorConos.loadModelosConos(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "BasicShader", Content);

            terreno.CargarTerreno(ContentFolder3D+"Terreno/height2",Content, 20f);
            terreno.SetEffect(_basicShader);
            
            //_plant = Content.Load<Model>(ContentFolder3D + "Plant/indoor plant_02_fbx/plant");
            //_cono.loadModel(ContentFolder3D + "Cono/Traffic Cone/Models and Textures/1", ContentFolderEffects + "BasicShader", Content);
            //_cono.SetScale(20f);
            //_edificio.SetEffect(_basicShader);
            //_rampa.SetEffect(_basicShader);
            //_rampa.SetRotacion(0f,0f,Convert.ToSingle(Math.PI/2));
            //_cilindro.SetEffect(_boxShader);
            //_cilindro.SetPosition(new Vector3(1000f,0f,2000f));
            //_cilindro.SetScale(100f);
            //_cilindro.SetRotacion(0, Convert.ToSingle(Math.PI/2), Convert.ToSingle(Math.PI/2));
            //_cilindro.SetTexture(textureBox);
            //_cilindro.ApplyTexturesToShader();
            //_palmera.loadModel(ContentFolder3D + "Palmera/palmera2", ContentFolderEffects + "BasicShader", Content);
            //_palmera.SetPosition(new Vector3(1500f, 0f, 1000f));
            //_palmera.SetScale(0.5f);


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
            auto.mover(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            //para que el camarografo nos siga siempre
            camarografo.setPuntoAtencion(auto.posicion);
            //camara.getInputs(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            base.Update(gameTime);
        }

        private float Timer{get;set;}= 0f;
        protected override void Draw(GameTime gameTime)
        {
            // Aca deberiamos poner toda la logia de renderizado del juego.
            GraphicsDevice.Clear(Color.LightBlue);

            auto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
            Escenario.Dibujar(camarografo);
            //_plane.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.DarkGray);
            generadorConos.drawConos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());

            terreno.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.DarkGray);
            
            //_plant.Draw(camarografo.getWorldMatrix(), camarografo.getViewMatrix(), camarografo.getProjectionMatrix());            
            //generadorPrueba.drawAutos(camarografo.getViewMatrix(), camarografo.getProjectionMatrix());           
            //_cono.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Orange);
            //_edificio.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
            //_rampa.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.SaddleBrown);
            //_cilindro.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.White);
            //_palmera.dibujar(camarografo.getViewMatrix(),camarografo.getProjectionMatrix(), Color.Green);
            //_plataforma.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Crimson);

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