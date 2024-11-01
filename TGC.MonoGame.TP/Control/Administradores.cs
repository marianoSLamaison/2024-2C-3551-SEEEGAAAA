using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics;
using BepuUtilities.Memory;
using Escenografia;
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
        public IA(Vector3 initPos, float influencia)
        {
            AutoControlado = new AutoNPC(initPos);
            DistanciaInfluenciaCuadrada = influencia;
        }
        public Vector2 posicionObj;
        private AutoNPC AutoControlado;
        private float DistanciaInfluenciaCuadrada;
        public bool NecesitoNuevoObjetivo(float deltaTime)
        {
            AutoControlado.Update(deltaTime, posicionObj);
            if (Vector2.Dot(posicionObj, Utils.Matematicas.AssV2(AutoControlado.Posicion)) <= DistanciaInfluenciaCuadrada)
                return true;
            return false;
        }
        public AutoNPC GetAuto() => AutoControlado;
        
    }
 

        Random RNG = new Random();
        private Effect [] efectos;
        private Model[] modelos;
        List<IA> autos;
        public void generarAutos(int numero, Vector2 areaTrabajo)
        {//los genero de esta manera para reducir espacios sin nada en los bordes del escenario
            List<Vector2> puntosSpawn = new List<Vector2>();
            float radioSubDiscos = areaTrabajo.Length() / 4f;
            Vector2 direccionSpawnPointOrigen = new Vector2(1,1);
            const float anguloDeRotacion =  3.1415926539f / 2f;//PI/2
            Vector2 centroActual;
            //cargamos todos los puntos donde spawnear autos
            for ( int i=1; i<=4; i++)
            {
                centroActual = Vector2.Transform(direccionSpawnPointOrigen, Matrix.CreateRotationZ(anguloDeRotacion * i));
                centroActual *= Convert.ToSingle(radioSubDiscos * Math.Sqrt(2));
                puntosSpawn.Concat(Utils.Commons.map(GenerarPuntosPoissonDisk(radioSubDiscos, 500f, numero / 4),
                vector => {return vector + centroActual;}));
            }
            //creamos dichos autos
            foreach( Vector2 posicion in puntosSpawn )
            {
                IA auto = new IA(new Vector3(posicion.X, 400f, posicion.Y), 100f);
                autos.Add(auto);
            }
        }

        public void Update(float deltaTime)
        {
            //los autos se moveran al azar en lineas rectas a objetivos en su rango
            foreach( IA auto in autos )
            {
                //esto se encarga de moverlos ya
                if (auto.NecesitoNuevoObjetivo(deltaTime))
                    auto.posicionObj = RNGDentroDeCirculo(10000f);
            }
        }
        public void load(String [] efectos, String [] modelos, ContentManager content)
        {
            foreach( IA auto in autos )
            {//designamos modelos al azar
                String dEffecto = efectos[RNG.Next() % efectos.Length];
                String dModelo = modelos[RNG.Next() % modelos.Length];
                auto.GetAuto().loadModel(dModelo, dEffecto, content);
            }
        }
        public void draw(Matrix view, Matrix projeccion)
        {
            foreach( IA auto in autos )
                auto.GetAuto().dibujar(view, projeccion, Color.Navy);
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

    