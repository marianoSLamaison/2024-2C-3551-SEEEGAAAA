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

namespace Control
{
    public class AdministradorNPCs 
    {
        //leer ese ejmplo si que dio muchas ideas AJAJ
    private class IA//para el manejo de los autos
    {
        public IA(){}
        public IA(Vector2 initPos, float distanciaParaCambio, Simulation simulation, BufferPool bufferPool)
        {
            AutoControlado = new AutoNPC();
            DistanciaAPuntoCuadrada = distanciaParaCambio * distanciaParaCambio;
            AutoControlado.CrearCollider(simulation, bufferPool, initPos);
            AutoControlado.velocidad = 300f;
            AutoControlado.SetVelocidadAngular(MathF.PI / 2f);
            objetivo = Utils.Matematicas.AssV2(AutoControlado.Posicion);
        }
        public Vector2 objetivo;
        protected AutoNPC AutoControlado;
        private float DistanciaAPuntoCuadrada;
        public bool NecesitoNuevoObjetivo()
        {
            Vector2 autosXZPos = new(AutoControlado.Posicion.X, AutoControlado.Posicion.Z);
            float cuadradoDistActual = Vector2.DistanceSquared(objetivo, autosXZPos);
            return cuadradoDistActual - DistanciaAPuntoCuadrada < 0f;
        }

        public virtual void Update()
        {
            // movemos el auto
            AutoControlado.Mover(251);
            //actualizamos nuestra direccion
            Vector2 nuevaDireccion = Vector2.Normalize(objetivo - Utils.Matematicas.AssV2(AutoControlado.Posicion));
            AutoControlado.SetDireccion(Utils.Matematicas.AssV3(nuevaDireccion));
        }
        
        public Vector2 GetPos() => new(AutoControlado.Posicion.X, AutoControlado.Posicion.Z);
        public void SetDestino(Vector2 obj)
        {
            objetivo = obj;
        }
        public AutoNPC GetAuto() => AutoControlado;
    }

        class AgresiveIA : IA
        {
            public IA autoObjetivo;
            public AgresiveIA(){

            }
            public AgresiveIA(Vector2 initPos, float distanciaParaCambio, Simulation simulation, BufferPool bufferPool) : base(initPos, distanciaParaCambio, simulation, bufferPool)
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
            public override void Update()
            {
                // movemos el auto
                AutoControlado.Mover(251);
                //actualizamos nuestra direccion
                Vector2 nuevaDireccion = Vector2.Normalize(autoObjetivo.GetPos() - Utils.Matematicas.AssV2(AutoControlado.Posicion));
                AutoControlado.SetDireccion(Utils.Matematicas.AssV3(nuevaDireccion));
                Console.WriteLine(nuevaDireccion);
            }
        }


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
                auto = new IA(punto, 2000, simulation, bufferpool);
                autos.Add(auto);  
            }
            atacantes = new(1);
        }

        public void Update()
        {
            //chequeamos si se puede o no colocar mas enemigos en pantalla
            const int maximoAts = 1;
            
            if ( atacantes.Count < maximoAts && autos.Count != 0)
            {
                AgresiveIA nuevoAtacante;
                IA candidato;
                for (int i =0; i< maximoAts - atacantes.Count; i++)
                {//se saca un auto del control normal, y se lo envia a la lista de control de enemigos
                    candidato = autos.ElementAt<IA>(RNG.Next() % autos.Count);
                    nuevoAtacante = AgresiveIA.TurnIAAgresive(candidato);
                    autos.Remove(candidato);
                    nuevoAtacante.autoObjetivo = autos.Count != 0 ? autos.ElementAt<IA>(RNG.Next() % autos.Count) : null;
                    atacantes.Add(nuevoAtacante);
                }
            }
            //los autos se moveran al azar en lineas rectas a objetivos en su rango
            foreach( IA auto in autos )
            {
                auto.Update();
                if ( auto.NecesitoNuevoObjetivo() ){
                    auto.SetDestino(
                        Utils.Matematicas.clampV(puntoEnAnillo(1000f, 5000f) + Utils.Matematicas.AssV2(auto.GetAuto().Posicion), new Vector2(10000,10000), new Vector2(-10000,-10000)));
                }
            }
            foreach( AgresiveIA atacante in atacantes)
            {
                atacante.Update();
            }
            
        }

        public Vector3 GetPosFitsCar()
        {
            return autos.ElementAt(0).GetAuto().Posicion;
        }
        private Vector2 puntoEnAnillo(float radioAnillo, float minRad)
        {
            Vector2 ret = Vector2.Zero;
            float radioVerdadero = RNG.NextSingle() * radioAnillo + minRad;
            ret = Vector2.Transform(Vector2.UnitX, Matrix.CreateRotationZ(RNG.NextSingle() * MathF.Tau));
            return ret * radioVerdadero;
        }
        public void load(String [] efectos, String [] modelos, ContentManager content)
        {
            foreach( IA auto in autos )
            {//designamos modelos y efectos al azar, si necesitan que esten juntos, habria que dise
            //diseñar alguna sestructura que tenga a los dos para tener el modelo y la estruct juntos,
            //y pasar eso
                String dEffecto = efectos[RNG.Next() % efectos.Length];
                String dModelo = modelos[RNG.Next() % modelos.Length];
                auto.GetAuto().loadModel(dModelo, dEffecto, content);
            }
        }
        public void draw(Matrix view, Matrix projeccion)
        {
            foreach( IA auto in autos )
                auto.GetAuto().dibujar(view, projeccion, Color.Navy);
            foreach( AgresiveIA atacante in atacantes )
                atacante.GetAuto().dibujar(view, projeccion, Color.Navy);
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
    public class AdministradorConos
    {
        static Random RNG = new Random();
        List<Cono> conos;
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
                nuevoCono.SetScale(20f); // Ajustar escala de los conos
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
            // Cargar modelos de conos
            foreach (Cono cono in conos)
            {
                cono.loadModel(direccionesModelos, direccionesEfectos, content);
                cono.CrearCollider(bufferPool, simulacion, cono.posicion);
            }
        }

        public void drawConos(Matrix view, Matrix projection)
        {
            // Dibujar todos los conos
            foreach (Cono cono in conos)
            {
                cono.dibujar(view, projection, Color.Orange);
            }
        }
    }
}

    