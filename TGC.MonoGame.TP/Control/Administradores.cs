using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BepuPhysics;
using BepuUtilities.Memory;
using Escenografia;
using Escenografia.TESTS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Design;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP;

namespace Control
{
    class AdministradorNPCs 
    {
        
    #region IAs
        //leer ese ejmplo si que dio muchas ideas AJAJ
    private class IA//para el manejo de los autos
    {
        private float fuerzaMov;
        private float aceleracion;
        private float maxVelocity;

        public Vector2 direccion;
        public IA(){}
        public IA(Vector2 initPos, Simulation simulation, BufferPool bufferPool)
        {
            AutoControlado = new AutoNPC();
            AutoControlado.CrearCollider(simulation, bufferPool, initPos);
            fuerzaMov = 251;//despues setear a un rango
            aceleracion = AutoControlado.DarAceleracion(fuerzaMov) / 2f;
            AutoControlado.velocidad = 0;
            maxVelocity = 2500f;
            SetDireccion(Utils.Matematicas.AssV2(AutoControlado.orientacion.Backward.ToNumerics()));
            AutoControlado.SetVelocidadAngular(MathF.Tau);
        }
        protected AutoNPC AutoControlado;

        private float tViaje;
        public bool NecesitoNuevoObjetivo() => tViaje <= 0f;

        public virtual void Update(float dtime)
        {
            // movemos el auto
            AutoControlado.Mover(fuerzaMov, dtime);
            tViaje -= dtime;
        }

        public Vector2 GetPos() => new(AutoControlado.Posicion.X, AutoControlado.Posicion.Z);
        public void SetDireccion(Vector2 direccion)
        {
            //actualizamos nuestra direccion
            //sacamos la direccion general
            //con esto sacamos la distancia teorica
            //con esto sacamos el tiempo aproximado de viaje
            float velocidad = AutoControlado.velocidad;
            //NOTA: como la distancia siempre es positiva podemos ignorar casos especiales de esta formula
            tViaje = 0.25f;

            //le damos su nueva direccion al ajuto controlado
            direccion.Normalize();
            this.direccion = direccion;
            AutoControlado.SetDireccion(new Vector3(direccion.X, 0f, direccion.Y));
            AutoControlado.velocidad = aceleracion * tViaje + velocidad;
            AutoControlado.velocidad = AutoControlado.velocidad > maxVelocity ? maxVelocity : AutoControlado.velocidad;

        }
        public AutoNPC GetAuto() => AutoControlado;
    }

        class AgresiveIA : IA
        {
            public IA autoObjetivo;
            public AgresiveIA(){

            }
            public AgresiveIA(Vector2 initPos, Simulation simulation, BufferPool bufferPool) : base(initPos, simulation, bufferPool)
            {
            }
            /*
                las ias agresivas simplemente atacan a un auto particular
            */
            public static AgresiveIA TurnIAAgresive(IA input)
            {
                AgresiveIA ret = new()
                {
                    AutoControlado = input.GetAuto()
                };
                return ret;
            }
            public override void Update(float deltaTime)
            {
                // movemos el auto
                AutoControlado.Mover(10, deltaTime);
                //actualizamos nuestra direccion
                Vector2 nuevaDireccion = Vector2.Normalize(autoObjetivo.GetPos() - Utils.Matematicas.AssV2(AutoControlado.Posicion));
                AutoControlado.SetDireccion(Utils.Matematicas.AssV3(nuevaDireccion));
            }
        }

    #endregion
        Random RNG = new Random();
        private Effect [] efectos;
        private Model[] modelos;
        List<IA> autos;
        List<AgresiveIA> atacantes;
        public void generarAutos(int numero, float radioArea, Simulation simulation, BufferPool bufferpool)
        {//los genero de esta manera para reducir espacios sin nada en los bordes del escenario
            int autosGen = numero / 4;
            int autosSob = numero % 4;
            float subRadio = radioArea / 2f;
            float anguloRot = MathF.PI / 2;
            Vector2 direccion_centro = Vector2.Normalize(new Vector2(1,1));
            Vector2 puntoTemp;
            List<Vector2> puntosAutos = new(numero);
            IA auto;
            autos = new(numero);
            for (int i=0; i<4; i++)
            {
                if ( autosGen != 0 )
                {
                    puntoTemp = Vector2.Transform(direccion_centro  * subRadio * MathF.Sqrt(2), Matrix.CreateRotationZ(anguloRot*i));
                    puntosAutos.AddRange(
                        Utils.Commons.map<Vector2>(GenerarPuntosPoissonDisk(radioArea, 1000f, autosGen),
                        (Vector) => {return Vector + puntoTemp;})
                        );
                }
            }
            if ( autosSob != 0)
                puntosAutos.AddRange(GenerarPuntosPoissonDisk(subRadio*(MathF.Sqrt(2) - 1), 1000, autosSob));
            foreach( Vector2 punto in puntosAutos )
            {
                auto = new IA(punto, simulation, bufferpool);
                autos.Add(auto);  
            }
            atacantes = new(1);
        }

        public void Update(float deltaTime)
        {
            //chequeamos si se puede o no colocar mas enemigos en pantalla
            const int maximoAts = 0;
            
            if ( atacantes.Count < maximoAts && autos.Count != 0)
            {
                AgresiveIA nuevoAtacante;
                IA candidato;
                for (int i =0; i< maximoAts - atacantes.Count; i++)
                {//se saca un auto del control normal, y se lo envia a la lista de control de enemigos
                    candidato = autos.ElementAt<IA>(0);
                    nuevoAtacante = AgresiveIA.TurnIAAgresive(candidato);
                    autos.Remove(candidato);
                    nuevoAtacante.autoObjetivo = autos.Count != 0 ? autos.ElementAt<IA>(RNG.Next() % autos.Count) : null;
                    atacantes.Add(nuevoAtacante);
                }
            }
            //los autos se moveran al azar en lineas rectas a objetivos en su rango
            foreach( IA auto in autos )
            {
                auto.Update(deltaTime);
                if ( auto.NecesitoNuevoObjetivo() ){
                    
                    auto.SetDireccion(500 * Utils.Matematicas.RandomTilt(auto.direccion, MathF.PI / 24f, -MathF.PI / 24f, RNG));
                    //auto.SetDireccion(
                      //  Utils.Matematicas.clampV(puntoEnAnillo(1000f, 3000f), new Vector2(10000,10000), new Vector2(-10000,-10000)));
                }
            }

            foreach( AgresiveIA atacante in atacantes)
            {
                //Necesita mas revision ( esta cambiando direccion, pero por algun motivo impacta mas de lo normal con tra el aire)
                //atacante.Update(deltaTime);
            }
            
        }

        public Vector3 GetPosFitsCar()
        {
            return Utils.Matematicas.AssV3(autos.ElementAt(0).GetPos());
        }
        private Vector2 puntoEnAnillo(float radioAnillo, float minRad)
        {
            Vector2 ret = Vector2.Zero;
            float radioVerdadero = RNG.NextSingle() * radioAnillo + minRad;
            ret = Vector2.Transform(Vector2.UnitX, Matrix.CreateRotationZ(RNG.NextSingle() * MathF.Tau));
            return ret * radioVerdadero;
        }
        public void load(Effect efecto, String [] modelos, Texture2D[] texturas, ContentManager content)
        {
            foreach( IA auto in autos )
            {//designamos modelos y efectos al azar, si necesitan que esten juntos, habria que dise
            //diseñar alguna sestructura que tenga a los dos para tener el modelo y la estruct juntos,
            //y pasar eso
                String dModelo = modelos[RNG.Next() % modelos.Length];
                Model modelo = content.Load<Model>(dModelo);

                auto.GetAuto().CargarModelo(efecto, modelo, texturas);
            }
        }
        public void LlenarGbuffer(Camarografo camarografo)
        {
            Matrix view = camarografo.getViewMatrix(), proj = camarografo.getProjectionMatrix();
            BoundingFrustum frustrumCamara = new BoundingFrustum(view * proj);
            AutoNPC autoR;
            foreach( IA auto in autos )
            {
                autoR = auto.GetAuto(); 
                autoR.BoundingVolume.Center = autoR.Posicion;
                if ( frustrumCamara.Intersects(autoR.BoundingVolume) )
                    auto.GetAuto().LlenarGbuffer(view, proj);
            }
            foreach( AgresiveIA atacante in atacantes )
            {
                autoR = atacante.GetAuto();
                autoR.BoundingVolume.Center = autoR.Posicion;
                if ( frustrumCamara.Intersects(autoR.BoundingVolume))
                    atacante.GetAuto().LlenarGbuffer(view, proj);
            }
        }
        public void draw(Matrix view, Matrix projeccion, RenderTarget2D shadowMap)
        {
            BoundingFrustum frustrumCamara = new BoundingFrustum(view * projeccion);
            AutoNPC autoR;
            foreach( IA auto in autos )
            {
                autoR = auto.GetAuto(); 
                autoR.BoundingVolume.Center = autoR.Posicion;
                if ( frustrumCamara.Intersects(autoR.BoundingVolume) )
                    auto.GetAuto().dibujar(view, projeccion, shadowMap);
            }
            foreach( AgresiveIA atacante in atacantes )
            {
                autoR = atacante.GetAuto();
                autoR.BoundingVolume.Center = autoR.Posicion;
                if ( frustrumCamara.Intersects(autoR.BoundingVolume))
                    atacante.GetAuto().dibujar(view, projeccion, shadowMap);
            }
        }

        ////funciones robadas de generador de conos ( no las lei pero se que funcionan )

        /// <summary>
        /// Genera puntos usando Poisson Disk Sampling en 2D (plano XZ).
        /// </summary>
        /// <param name="radio">Radio máximo del área.</param>
        /// <param name="distanciaMinima">Distancia mínima entre conos.</param>
        /// <param name="numeroNPCs">Número máximo de conos a generar.</param>
        /// <returns>Lista de puntos 2D en el plano XZ.</returns>
        private List<Vector2> GenerarPuntosPoissonDisk(float radio, float distanciaMinima, int numeroNPCs)
        {
            // Configuración inicial del algoritmo de Poisson Disk Sampling
            float cellSize = distanciaMinima / (float)Math.Sqrt(2);
            int gridSize = (int)Math.Ceiling(2 * radio / cellSize);
            Vector2?[,] grid = new Vector2?[gridSize, gridSize];
            List<Vector2> puntos = new List<Vector2>();
            List<Vector2> activos = new List<Vector2>();

            // Generar el primer punto aleatorio en el círculo
            Vector2 primerPunto = RNGDentroDeCirculo(radio);
            puntos.Add(primerPunto);
            activos.Add(primerPunto);

            int gridX = (int)((primerPunto.X + radio) / cellSize);
            int gridY = (int)((primerPunto.Y + radio) / cellSize);
            grid[gridX, gridY] = primerPunto;

            while (activos.Count > 0 && puntos.Count < numeroNPCs)
            {
                int indiceAleatorio = RNG.Next(activos.Count);
                Vector2 puntoActivo = activos[indiceAleatorio];
                bool puntoEncontrado = false;

                // Intentar generar nuevos puntos alrededor del activo
                for (int i = 0; i < 30; i++)
                {
                    Vector2 nuevoPunto = GenerarPuntoAleatorio(puntoActivo, distanciaMinima);

                    if (EsPuntoValido(nuevoPunto, grid, gridSize, cellSize, distanciaMinima, radio))
                    {
                        puntos.Add(nuevoPunto);
                        activos.Add(nuevoPunto);

                        int nuevoGridX = (int)((nuevoPunto.X + radio) / cellSize);
                        int nuevoGridY = (int)((nuevoPunto.Y + radio) / cellSize);
                        grid[nuevoGridX, nuevoGridY] = nuevoPunto;

                        puntoEncontrado = true;
                        break;
                    }
                }

                if (!puntoEncontrado)
                {
                    activos.RemoveAt(indiceAleatorio);
                }
            }

            return puntos;
        }
        private Vector2 RNGDentroDeCirculo(float radio)
        {
            float angulo = (float)(RNG.NextDouble() * Math.PI * 2);
            float distancia = (float)(RNG.NextDouble() * radio);
            return new Vector2(distancia * (float)Math.Cos(angulo), distancia * (float)Math.Sin(angulo));
        }
        private Vector2 GenerarPuntoAleatorio(Vector2 centro, float distanciaMinima)
        {
            float radioAleatorio = distanciaMinima * (1 + (float)RNG.NextDouble());
            float anguloAleatorio = (float)(RNG.NextDouble() * 2 * Math.PI);

            float nuevoX = centro.X + radioAleatorio * (float)Math.Cos(anguloAleatorio);
            float nuevoY = centro.Y + radioAleatorio * (float)Math.Sin(anguloAleatorio);

            return new Vector2(nuevoX, nuevoY);
        }
        private bool EsPuntoValido(Vector2 punto, Vector2?[,] grid, int gridSize, float cellSize, float distanciaMinima, float radio)
        {
            // Verificar que el punto está dentro del círculo
            if (punto.Length() > radio)
                return false;

            int gridX = (int)((punto.X + radio) / cellSize);
            int gridY = (int)((punto.Y + radio) / cellSize);

            if (gridX < 0 || gridX >= gridSize || gridY < 0 || gridY >= gridSize)
                return false;

            // Verificar las celdas vecinas
            for (int x = Math.Max(0, gridX - 2); x <= Math.Min(gridSize - 1, gridX + 2); x++)
            {
                for (int y = Math.Max(0, gridY - 2); y <= Math.Min(gridSize - 1, gridY + 2); y++)
                {
                    if (grid[x, y] != null)
                    {
                        float distancia = Vector2.Distance(punto, grid[x, y].Value);
                        if (distancia < distanciaMinima)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
    class AdministradorConos
    {
        static Random RNG = new Random();
        List<Cono> conos;
        BoundingSphere BoundingVolume;
        private const float EscalaDeConos = 20f;
        float alturaConos = 400f; // Altura fija para todos los conos

        public void generarConos(Vector3 centro, float radio, int numeroNPCs, float distanciaMinima)
        {
            conos = new List<Cono>(numeroNPCs);
            List<Vector2> puntosPoisson = GenerarPuntosPoissonDisk(radio, distanciaMinima, numeroNPCs);

            // Convertir puntos 2D (XZ) en puntos 3D con altura fija en Y
            foreach (var punto in puntosPoisson)
            {
                Vector3 puntoPlano = new Vector3(punto.X, alturaConos, punto.Y);
                Cono nuevoCono = new Cono(puntoPlano + centro);
                nuevoCono.SetScale(EscalaDeConos); // Ajustar escala de los conos
                conos.Add(nuevoCono);

            }
        }

        /// <summary>
        /// Genera puntos usando Poisson Disk Sampling en 2D (plano XZ).
        /// </summary>
        /// <param name="radio">Radio máximo del área.</param>
        /// <param name="distanciaMinima">Distancia mínima entre conos.</param>
        /// <param name="numeroNPCs">Número máximo de conos a generar.</param>
        /// <returns>Lista de puntos 2D en el plano XZ.</returns>
        private List<Vector2> GenerarPuntosPoissonDisk(float radio, float distanciaMinima, int numeroNPCs)
        {
            // Configuración inicial del algoritmo de Poisson Disk Sampling
            float cellSize = distanciaMinima / (float)Math.Sqrt(2);
            int gridSize = (int)Math.Ceiling(2 * radio / cellSize);
            Vector2?[,] grid = new Vector2?[gridSize, gridSize];
            List<Vector2> puntos = new List<Vector2>();
            List<Vector2> activos = new List<Vector2>();

            // Generar el primer punto aleatorio en el círculo
            Vector2 primerPunto = RNGDentroDeCirculo(radio);
            puntos.Add(primerPunto);
            activos.Add(primerPunto);

            int gridX = (int)((primerPunto.X + radio) / cellSize);
            int gridY = (int)((primerPunto.Y + radio) / cellSize);
            grid[gridX, gridY] = primerPunto;

            while (activos.Count > 0 && puntos.Count < numeroNPCs)
            {
                int indiceAleatorio = RNG.Next(activos.Count);
                Vector2 puntoActivo = activos[indiceAleatorio];
                bool puntoEncontrado = false;

                // Intentar generar nuevos puntos alrededor del activo
                for (int i = 0; i < 30; i++)
                {
                    Vector2 nuevoPunto = GenerarPuntoAleatorio(puntoActivo, distanciaMinima);

                    if (EsPuntoValido(nuevoPunto, grid, gridSize, cellSize, distanciaMinima, radio))
                    {
                        puntos.Add(nuevoPunto);
                        activos.Add(nuevoPunto);

                        int nuevoGridX = (int)((nuevoPunto.X + radio) / cellSize);
                        int nuevoGridY = (int)((nuevoPunto.Y + radio) / cellSize);
                        grid[nuevoGridX, nuevoGridY] = nuevoPunto;

                        puntoEncontrado = true;
                        break;
                    }
                }

                if (!puntoEncontrado)
                {
                    activos.RemoveAt(indiceAleatorio);
                }
            }

            return puntos;
        }

        private Vector2 GenerarPuntoAleatorio(Vector2 centro, float distanciaMinima)
        {
            float radioAleatorio = distanciaMinima * (1 + (float)RNG.NextDouble());
            float anguloAleatorio = (float)(RNG.NextDouble() * 2 * Math.PI);

            float nuevoX = centro.X + radioAleatorio * (float)Math.Cos(anguloAleatorio);
            float nuevoY = centro.Y + radioAleatorio * (float)Math.Sin(anguloAleatorio);

            return new Vector2(nuevoX, nuevoY);
        }

        private bool EsPuntoValido(Vector2 punto, Vector2?[,] grid, int gridSize, float cellSize, float distanciaMinima, float radio)
        {
            // Verificar que el punto está dentro del círculo
            if (punto.Length() > radio)
                return false;

            int gridX = (int)((punto.X + radio) / cellSize);
            int gridY = (int)((punto.Y + radio) / cellSize);

            if (gridX < 0 || gridX >= gridSize || gridY < 0 || gridY >= gridSize)
                return false;

            // Verificar las celdas vecinas
            for (int x = Math.Max(0, gridX - 2); x <= Math.Min(gridSize - 1, gridX + 2); x++)
            {
                for (int y = Math.Max(0, gridY - 2); y <= Math.Min(gridSize - 1, gridY + 2); y++)
                {
                    if (grid[x, y] != null)
                    {
                        float distancia = Vector2.Distance(punto, grid[x, y].Value);
                        if (distancia < distanciaMinima)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private Vector2 RNGDentroDeCirculo(float radio)
        {
            float angulo = (float)(RNG.NextDouble() * Math.PI * 2);
            float distancia = (float)(RNG.NextDouble() * radio);
            return new Vector2(distancia * (float)Math.Cos(angulo), distancia * (float)Math.Sin(angulo));
        }

        public void loadModelosConos(string direccionesModelos, string direccionesEfectos, ContentManager content, BufferPool bufferPool, Simulation simulacion)
        {
            Model modeloComun = content.Load<Model>(direccionesModelos);
            Effect efectoComun = content.Load<Effect>(direccionesEfectos);
            //OK lo chequee mono es listo, chequea si el modelo ya existe y si si lo hace
            //al llamar muchas veces a load, solamente devuelve el valor pedido.
            //no vuelvo a confiar en IAs nunca mas...

            /*NOTA Chequear de poner algo como esto para setear diferentes valores para el shader
            #region Setup de el efecto y el modelo comunes a los conos
            efectoComun.Parameters["lightPosition"]?.SetValue(new Vector3(7000,3000,2000));

            efectoComun.Parameters["ambientColor"]?.SetValue(new Vector3(0.5f, 0.2f, 0.15f));
            efectoComun.Parameters["diffuseColor"]?.SetValue(new Vector3(0.9f, 0.7f, 0.3f));
            efectoComun.Parameters["specularColor"]?.SetValue(new Vector3(1f, 1f, 1f));

            efectoComun.Parameters["KAmbient"]?.SetValue(0.4f);
            efectoComun.Parameters["KDiffuse"]?.SetValue(1.5f);
            efectoComun.Parameters["KSpecular"]?.SetValue(0.25f);
            efectoComun.Parameters["shininess"]?.SetValue(32.0f);
            #endregion
            */
            foreach ( ModelMesh mesh in modeloComun.Meshes )
            {
                foreach ( ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = efectoComun;
                }
            }
            //generamos una bounding box que encapsula al cono normal
            //luego solo la transofrmamos para chequear con cada cono
            BoundingVolume = MonoHelper.GenerarBoundingSphere(modeloComun, EscalaDeConos * 2.5f);

            // Cargar modelos de conos
            foreach (Cono cono in conos)
            {
                //cono.loadModel(direccionesModelos, direccionesEfectos, content);
                //de esta manera solo hay un modelo cargado en memoria relamente, simplemente esta siendo dibujado todo el rato igual
                cono.loadApariencia(modeloComun, efectoComun);
                cono.CrearCollider(bufferPool, simulacion, cono.posicion);
            }
        }
        public void LlenarGbuffer( Control.Camarografo juan)
        {
            Matrix view = juan.getViewMatrix(), proj = juan.getProjectionMatrix();
            BoundingFrustum frustrumCamara = new BoundingFrustum(view * proj);
            //chequear si los conos estan en el frustrum
            foreach(Cono cono in conos)
            {
                //como es una bounding sphere da igual que los conos esten rotados, 
                //solo tenemos que moverla de lugar
                BoundingVolume.Center = cono.posicion;
                if (frustrumCamara.Intersects(BoundingVolume))
                    cono.LlenarGbuffer(view, proj);
            }
        }
        public void drawConos(Matrix view, Matrix projection)
        {
            BoundingFrustum frustrumCamara = new BoundingFrustum(view * projection);
            // Dibujar todos los conos
            foreach (Cono cono in conos)
            {
                //ajustamos la esfera comunal a la posicion del cono
                BoundingVolume.Center = cono.Position;
                //si esta visible, la dibujamos
                if ( frustrumCamara.Intersects(BoundingVolume))
                    cono.dibujar(view, projection, Color.Orange);
            }
        }

    }
}

    