using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;


class PantallaFinal

{
    public bool Victoria = true;
    Texture2D FinalTitle;
    SpriteBatch SpriteBatch;
    fRectangle posScreen;
    int screenWidth, screenHeight;
    public PantallaFinal(SpriteBatch SpriteBatch, fRectangle rectangulo, int screenWidth, int screenHeight)
    {
        this.SpriteBatch = SpriteBatch;
        posScreen = rectangulo;
        this.screenWidth = screenWidth;
        this.screenHeight = screenHeight;
    }
    public void Load(ContentManager cont)
    {
        FinalTitle = cont.Load<Texture2D>("PantallaFinal");
    }
    public void Write()
    {
        Rectangle pantFin = Victoria ? new Rectangle(1027,0,1028,256) : new Rectangle(0,0,1028,256);
        SpriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullNone);
        SpriteBatch.Draw(FinalTitle, posScreen.toIntRect(screenWidth, screenHeight), pantFin,Color.White);
        SpriteBatch.End();
    }
}
/*
La pantalla de titulo tiene la siguiente estructura

    TITULO
------------------
    |Boton|
    (texto)
La pantall de ayuda en cambio tiene la estructura
    TITULO
-----------------
|Boton|/(TTTTT)
|Boton|/(EEEEE)
       /(XXXXX)
       /(TTTTT)
       /(OOOOO)
*/
class AyudaMenu : Cuadro
{
    Texture2D titulo, fondo;

    public AyudaMenu(SpriteBatch spriteBatch, int Width, int Height) : base(spriteBatch, Width, Height)
    {
    }
    public override void Load(ContentManager content)
    {
        titulo = content.Load<Texture2D>("ayuda");
        fondo = content.Load<Texture2D>("Fondo");
        base.Load(content);
    }
    public override void Write()
    {
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
        if (Botones[0].estaSeleccionado)
            spriteBatch.Draw(fondo, new fRectangle(0.1f, 0.1f, 0.8f, 0.8f).toIntRect(Width, Height),new Rectangle(0, 0, 1027, 1027), Color.Purple);
        else if (Botones[1].estaSeleccionado)
            spriteBatch.Draw(fondo, new fRectangle(0.1f, 0.1f, 0.8f, 0.8f).toIntRect(Width, Height),new Rectangle(1027,0,1027,1027), Color.Purple);
        else 
            spriteBatch.Draw(fondo, new fRectangle(0.1f, 0.1f, 0.8f, 0.8f).toIntRect(Width, Height),new Rectangle(1027 + 1028,0,1028,1028), Color.Purple);
        
        Rectangle rectangulo = new fRectangle(0.31f, 0.07f, 0.35f, 0.2f).toIntRect(Width, Height);
        spriteBatch.Draw(titulo, rectangulo, Color.Orange);
        foreach( Boton boton in Botones )
            boton.Dibujar(spriteBatch);
        
        spriteBatch.End();
    }
}
class InicioMenu : Cuadro
{
    Texture2D titulo;
    Texture2D fondo = null;

    public InicioMenu(SpriteBatch spriteBatch, int Width, int Height) : base(spriteBatch, Width, Height)
    {

    }
    public override void Load(ContentManager content)
    {
        titulo = content.Load<Texture2D>("Titulo");
        base.Load(content);
    }
    public override void Write()
    {
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
        foreach( Boton boton in Botones )
            boton.Dibujar(spriteBatch);
        Rectangle rectangulo = new fRectangle(0.25f, 0.07f, 0.5f, 0.4f).toIntRect(Width, Height);
        spriteBatch.Draw(titulo, rectangulo, Color.Orange);
        spriteBatch.End();
    }

}

class Cuadro
{
    public Boton[] Botones;
    protected int Width, Height;
    protected SpriteBatch spriteBatch;

    public Cuadro(SpriteBatch spriteBatch, int Width, int Height)
    {   
        this.spriteBatch = spriteBatch;
        this.Height = Height;
        this.Width = Width;

    }
    /// <summary>
    /// Recibe una lista de botones dada como porcentajes
    /// No dices "quiero que este en posicion 20 20" 
    /// dices "quiero que este a 20% del ancho de la pantalla
    /// y que tenga un 5% del largo de la pantalla
    /// luego el cuadro se encarga de mandarlo a dimensiones de pantalla
    /// </summary>
    /// <param name="Botones"></param>
    private void loadBotones(int tipoLoad, params fRectangle[] Botones)
    {
        this.Botones = new Boton[Botones.Length];
        fRectangle finalRect;
        if (Botones.Length == 1)
        {
            finalRect = Botones[0];
            finalRect.x *= Width;
            finalRect.y *= Height;
            finalRect.Height *= Height;
            finalRect.Width  *= Width;
            this.Botones[0] = new(1);
            this.Botones[0].rectanguloEnclaustro = finalRect.toIntRect();
        }
        else{
            for ( int i=0; i<Botones.Length; i++)
            {
                this.Botones[i] = new(2 + i);//carga cada boton 
                finalRect = Botones[i];
                finalRect.x *= Width;
                finalRect.y *= Height;
                finalRect.Height *= Height;
                finalRect.Width  *= Width;
                this.Botones[i].rectanguloEnclaustro = finalRect.toIntRect();
            }

        }
    }
    public void loadBotonesMenuInicio()
    {
        loadBotones(1, 
            new fRectangle(0.4f, 0.6f, 0.2f, 0.2f)
        );
    }
    public void loadBotonesMenuAyuda()
    {
        loadBotones(2,
            new fRectangle(0.12f, 0.30f, 0.18f, 0.18f),
            new fRectangle(0.12f, 0.50f, 0.18f, 0.18f)
        );
    }
    public virtual void Write()
    {
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
        foreach( Boton boton in Botones )
            boton.Dibujar(spriteBatch);
        spriteBatch.End();
    }

    public virtual void Load(ContentManager content)
    {

        foreach(Boton boton in Botones)
            boton.Load(content);
    }
    
}

class Boton
{
    /*
    un boton es simplemente un conjunto de dos sprites 
   un mensaje y un fondo si una variable booleana que tiene se activa,
   su fondo cambia al secundario, la variable es controlada desde afuera
    */
    String 
    msJuego = "Sprites/msg_jugar",
    msControles = "controlsText",
    msResumen = "resumenText",
    msPropio;
    Texture2D fondo1, fondo2, mensaje;
    public Rectangle rectanguloEnclaustro;
    public bool estaSeleccionado = false;
    public Boton(int tipo)
    {
        switch ( tipo )
        {
            case 1:
            msPropio = msJuego;
            break;
            case 2:
            msPropio = msControles;
            break;
            case 3:
            msPropio = msResumen;
            break;
        }
    }
    public void Dibujar(SpriteBatch sprBatch)
    {
        Texture2D fondoElegido = estaSeleccionado ? fondo2 : fondo1;
        sprBatch.Draw(fondoElegido, rectanguloEnclaustro, Color.Orange);
        sprBatch.Draw(mensaje, rectanguloEnclaustro, Color.White);
    }
    public void Load(ContentManager content)
    {
        fondo1 = content.Load<Texture2D>("Sprites/bcg_fondo1");
        fondo2 = content.Load<Texture2D>("Sprites/bcg_fondo2");
        mensaje = content.Load<Texture2D>(msPropio);

    }

}

struct fRectangle
{
    public float x, y;
    public float Height, Width;
    public fRectangle(float x, float y, float Width, float Height)
    {
        this.x = x;
        this.y = y;
        this.Height = Height;
        this.Width = Width;
    }
    public Rectangle toIntRect()
    {
        return new Rectangle(
            (int)MathF.Truncate(x),
            (int)MathF.Truncate(y),
            (int)MathF.Truncate(Width),
            (int)MathF.Truncate(Height)
        );
    }
    public Rectangle toIntRect(float dimsx, float dimsy)
    {
        return new Rectangle(
            (int)MathF.Truncate(x*dimsx),
            (int)MathF.Truncate(y*dimsy),
            (int)MathF.Truncate(Width*dimsx),
            (int)MathF.Truncate(Height*dimsy)
        );
    }
}