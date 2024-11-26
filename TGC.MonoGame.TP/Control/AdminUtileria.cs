using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Escenografia;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Control
{
    class AdminUtileria
    {
        Escenografia.LimBox limites;
        private List<Plataforma> objetosFijos;
        Terreno suelo;
        Texture2D plataformasColor;
        ThreadDispatcher threadDispatcher;
        public AdminUtileria(float ScuareSide, float desiredHeigth, float desiredScale, Simulation simulacion)
        {
            var minLims = new Vector3(-6100f,400f,-6100f);
            var maxLims = new Vector3(6100f,400f,6100f);
            Vector3 dimensiones = maxLims - minLims;
            limites = new Escenografia.LimBox(minLims, maxLims);

            //creamos un dos puntos que definen los limites del area total
            //cada 
            /*
            float sqr2 = MathF.Sqrt(2);
            const float bRotation = MathF.PI / 2f;
            Vector3 esquina = new Vector3(sqr2, 0f, sqr2) * ScuareSide / 2f;
            Vector3 bHeigth = new Vector3(0f, desiredHeigth, 0f);
            limites = new Escenografia.LimBox(-esquina , esquina );
            objetosFijos = new List<Plataforma>{
                new Escenografia.Plataforma(bRotation, esquina + bHeigth),
                new Escenografia.Plataforma(
                    bRotation * 2f, Vector3.Transform(esquina, Microsoft.Xna.Framework.Matrix.CreateRotationY(bRotation)) + bHeigth),
                new Escenografia.Plataforma(
                    bRotation * 3f, Vector3.Transform(esquina, Microsoft.Xna.Framework.Matrix.CreateRotationY(bRotation * 2f)) + bHeigth),
                new Escenografia.Plataforma(
                    0, Vector3.Transform(esquina, Microsoft.Xna.Framework.Matrix.CreateRotationY( bRotation * 3f)) + bHeigth)
            };*/

            objetosFijos = new List<Escenografia.Plataforma>
            {
                new Escenografia.Plataforma(3*MathF.PI / 2, minLims),
                new Escenografia.Plataforma(MathF.PI, new Vector3(minLims.X + dimensiones.X, 400f, minLims.Z)),
                new Escenografia.Plataforma(0,  new Vector3(minLims.X, 400f, minLims.Z + dimensiones.Z)),
                new Escenografia.Plataforma(MathF.PI / 2, maxLims)
            };
            Escenografia.Plataforma.setGScale(15 * 1.75f);
            suelo = new Terreno();
        }

        public void setThreadDispatcher(ThreadDispatcher dispatcher)
        {
            threadDispatcher = dispatcher;
        }
    private void SetParedes(Simulation simulacion)
    {
        /*
        const float grosorPared = 500f;
        float ladoParedEjeZ = MathF.Abs(limites.maxVertice.X - limites.minVertice.X) + grosorPared;
        float ladoParedEjeX = MathF.Abs(limites.maxVertice.Z - limites.minVertice.Z) + grosorPared;
        var paredEjeX = simulacion.Shapes.Add(new Box(grosorPared, ladoParedEjeX, ladoParedEjeX));
        var paredEjeZ = simulacion.Shapes.Add(new Box(ladoParedEjeZ, ladoParedEjeZ, grosorPared));

        RigidPose poseParedEjeX1 = new RigidPose(Vector3.UnitX.ToNumerics() * ladoParedEjeZ / 2f);
        RigidPose poseParedEjeX2 = new RigidPose(-1 * Vector3.UnitX.ToNumerics() * ladoParedEjeZ / 2f); 
        RigidPose poseParedEjeZ1 = new RigidPose(Vector3.UnitZ.ToNumerics() * ladoParedEjeX / 2f);
        RigidPose poseParedEjeZ2 = new RigidPose(-1 * Vector3.UnitZ.ToNumerics() * ladoParedEjeX / 2f);

        simulacion.Statics.Add(new StaticDescription(poseParedEjeX1, paredEjeX));
        simulacion.Statics.Add(new StaticDescription(poseParedEjeX2, paredEjeX));
        simulacion.Statics.Add(new StaticDescription(poseParedEjeZ1, paredEjeZ));
        simulacion.Statics.Add(new StaticDescription(poseParedEjeZ2, paredEjeZ));
        */

        var Pared1 = new Box(1, 1000000, 1000000);
        // Add the plane to the simulation
        AyudanteSimulacion.agregarCuerpoStatico(new RigidPose((Vector3.UnitX*8800).ToNumerics()), simulacion.Shapes.Add(Pared1));
        var Pared2 = new Box(1, 1000000, 1000000);
        // Add the plane to the simulation
        AyudanteSimulacion.agregarCuerpoStatico(new RigidPose((Vector3.UnitX*-8800).ToNumerics()), simulacion.Shapes.Add(Pared2));
        var Pared3 = new Box(1000000, 1000000, 1);
        // Add the plane to the simulation
        AyudanteSimulacion.agregarCuerpoStatico(new RigidPose((Vector3.UnitZ*8800).ToNumerics()), simulacion.Shapes.Add(Pared3));
        var Pared4 = new Box(1000000, 1000000, 1);
        // Add the plane to the simulation
        AyudanteSimulacion.agregarCuerpoStatico(new RigidPose((Vector3.UnitZ*-8800).ToNumerics()), simulacion.Shapes.Add(Pared4));
    }

    public void SetTexturePlataform(Escenografia.Plataforma unaPlataforma, Texture2D unaTextura){
        unaPlataforma.SetTexture(unaTextura);
    }
    public void loadPlataformas(string direcionModelo, string direccionEfecto, ContentManager contManager)
    {
        if (objetosFijos.Count > 4) throw new Exception("Esto era un metodo de prueba");
        plataformasColor = contManager.Load<Texture2D>("Models/Plataforma/PlataformasColor");
        Effect efecto = contManager.Load<Effect>(direccionEfecto);
        Model modelo = contManager.Load<Model>(direcionModelo);

        foreach(Plataforma plataforma in objetosFijos)
        {
            plataforma.loadModel(modelo,efecto);
        }
    }
    public void loadTerreno(Effect efecto, ContentManager content)
    {
        //suelo.SetEffect(efecto, content);
        suelo.setEffect2(efecto, content);
    }

    public void CrearColliders(BufferPool bufferPool, Simulation simulacion){
        //creamos los coliders de todas las plataformas
        foreach(Plataforma plataforma in objetosFijos)
            plataforma.CrearCollider(bufferPool, simulacion);
        //creamos el colider del suelo
        suelo.CrearCollider(bufferPool, simulacion, threadDispatcher,
         (int)MathF.Abs(limites.maxVertice.X - limites.minVertice.X), 
         (int)MathF.Abs(limites.maxVertice.Z - limites.minVertice.Z));
        
        //creamos las paredes
        SetParedes(simulacion);
    }

        public void Dibujar(Camarografo camarografo, RenderTarget2D shadowMap)
        {
            foreach (Plataforma objeto in objetosFijos)
            {
                objeto.dibujarPlataforma(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Silver);
            }
            suelo.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), camarografo.camaraAsociada.posicion, shadowMap);
        }
        public void LlenarGbuffer(Camarografo camarografo)
        {
            Microsoft.Xna.Framework.Matrix view = camarografo.getViewMatrix(), 
            proj = camarografo.getProjectionMatrix(),
            ligthViewProj = camarografo.GetLigthViewProj();
            suelo.LlenarGbuffer(view, proj, ligthViewProj);
            foreach(Plataforma obj in objetosFijos)
            {
                obj.LlenarGbufferPlataforma(view, proj, ligthViewProj, plataformasColor);
            }
        }
        public void LlenarEfectsBuffer(Camarografo camarografo)
        {
            Microsoft.Xna.Framework.Matrix view = camarografo.getViewMatrix(), 
            proj = camarografo.getProjectionMatrix(),
            ligthViewProj = camarografo.GetLigthViewProj();
            suelo.LlenarEfectsBuffer(view, proj, ligthViewProj);
            foreach(Plataforma obj in objetosFijos)
            {
                obj.LlenarEfectsBuffer(view, proj, ligthViewProj);
            }
        }
        public void dibujarSombras(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection)
        {
            suelo.dibujarSombras(view, projection);
        }
    }
}