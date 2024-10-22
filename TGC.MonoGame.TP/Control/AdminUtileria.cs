using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
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
        private List<Escenografia.Escenografia3D> objetosFijos;

        private Texture2D texturePlataforma1;
        private Texture2D texturePlataforma2;
        private Texture2D texturePlataforma3;
        private Texture2D texturePlataforma4;

        private Escenografia.Plataforma plataforma1;
        private Escenografia.Plataforma plataforma2;
        private Escenografia.Plataforma plataforma3;
        private Escenografia.Plataforma plataforma4;
        public AdminUtileria(Vector3 minLims, Vector3 maxLims)
        {
            limites = new Escenografia.LimBox(minLims, maxLims);
            Vector3 dimensiones = maxLims - minLims;
            objetosFijos = new List<Escenografia.Escenografia3D>
            {
                new Escenografia.Plataforma(Convert.ToSingle(3*Math.PI / 2), minLims),
                new Escenografia.Plataforma(Convert.ToSingle(Math.PI), new Vector3(minLims.X + dimensiones.X,500f, minLims.Z)),
                new Escenografia.Plataforma(Convert.ToSingle(0),  new Vector3(minLims.X, 500f, minLims.Z + dimensiones.Z)),
                new Escenografia.Plataforma(Convert.ToSingle(Math.PI / 2), maxLims)
            };
        }

    

    public void SetTexturePlataform(Escenografia.Plataforma unaPlataforma, Texture2D unaTextura){
        unaPlataforma.SetTexture(unaTextura);
    }
    public void loadPlataformas(string direcionModelo, string direccionEfecto, ContentManager contManager)
    {
        if (objetosFijos.Count > 4) throw new Exception("Esto era un metodo de prueba");
    
        foreach(Plataforma plataforma in objetosFijos)
        {
            plataforma.loadModel(direcionModelo,direccionEfecto,contManager);
        }
    }

    public void CrearColliders(BufferPool bufferPool, Simulation simulacion){
        foreach(Plataforma plataforma in objetosFijos)
        {
            plataforma.CrearCollider(bufferPool, simulacion);
        }
    }

    public void Dibujar(Camarografo camarografo)
    {
        foreach (Escenografia.Escenografia3D objeto in objetosFijos)
        {
            objeto.dibujar(camarografo.getViewMatrix(), camarografo.getProjectionMatrix(), Color.Purple);
        }
    }
    }
}