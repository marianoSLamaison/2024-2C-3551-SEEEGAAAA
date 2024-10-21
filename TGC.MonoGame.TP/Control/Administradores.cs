using System;
using System.Collections.Generic;
using System.Timers;
using BepuPhysics;
using BepuUtilities.Memory;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Control
{
    /*TODO: Crear una nueva de estas para cuando tengamos NPCs que se muevan
    class AdministradorNPCs
    {
        static Random RNG = new Random();
        List<AutoNPC> npcs;
        //genera un monton de npcs al azar en el mapa ( suponiendo que es plano por ahora )
        public void generarNPCsV1(Vector3 minPos,Vector3 maxPos)
        {
            float ancho = maxPos.X - minPos.X;
            float alto = maxPos.Z - minPos.Z;
            npcs = new List<AutoNPC>(50);
            AutoNPC holder;
            float desplazamiento = Math.Max(Math.Min(ancho,alto),1f);
            int autos_linea = 0;
            for ( int i=0; i<ancho; i++)
            {
                autos_linea = 0;
                for ( int j=0; j<alto; j++)
                {
                    if ( autos_linea < 10)
                    {
                        holder = new AutoNPC(minPos + new Vector3(j,0f,i) * desplazamiento);
                        npcs.Add(holder);
                        autos_linea ++;
                    }
                    else{
                        
                        break;
                    }
                }
                if ( npcs.Count >= 50)
                {
                    break;
                }
            }
        }
        //crea un monton de autos identicos
        //este los genera en un circulo ( me gustan mas los escenarios circulares, todavia mas para este caso )
        public void generadorNPCsV2(Vector3 centro, float radio, int numeroNPCs)
        {
            float distanciaCentro, anguloDesdeCentro;
            Vector3 puntoPlano;
            npcs = new List<AutoNPC>(numeroNPCs);
            AutoNPC holder;
            for ( int i=0; i< numeroNPCs; i++)
            {
                distanciaCentro = (float)(RNG.NextDouble() * radio);
                anguloDesdeCentro = (float)(RNG.NextDouble() * Math.Tau);
                puntoPlano = Vector3.Transform(Vector3.Forward, Matrix.CreateRotationY(anguloDesdeCentro)) * distanciaCentro;
                //Console.WriteLine(distanciaCentro);
                holder = new AutoNPC(puntoPlano + centro, 
                0, 
                Convert.ToSingle(RNG.NextDouble() * Math.PI),
                0, 
                new Color( (float)RNG.NextDouble(), (float)RNG.NextDouble(), (float)RNG.NextDouble()));
                //Console.WriteLine(holder.getWorldMatrix());
                npcs.Add(holder);
            }
        }
        public void loadModelosAutos(String[] direccionesModelos, String[] direccionesEfectos, ContentManager content)
        {
            //cargamos todos los modelos al azar
            foreach( AutoNPC auto in npcs)
            {
                Random rangen = new Random();
                
                auto.loadModel(direccionesModelos[rangen.Next(direccionesModelos.Length)],
                direccionesEfectos[rangen.Next(direccionesEfectos.Length)],content);
            }
        }
        public void drawAutos(Matrix view, Matrix projeccion)
        {
            
            foreach( AutoNPC auto in npcs )
            {
                auto.dibujar(view, projeccion, auto.color);
            }
        }
    }
*/
    public class AdministradorConos
    {
        static Random RNG = new Random();
        List<Cono> conos;
        float alturaConos = 500f; // Altura fija para todos los conos

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

    