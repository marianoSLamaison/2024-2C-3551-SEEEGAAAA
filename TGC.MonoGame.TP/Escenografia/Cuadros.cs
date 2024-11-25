using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;



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


class Cuadro
{
    SpriteFont TitleFont;
    SpriteFont MessagesFont;
    Boton[] Botones;
    int Width, Height;
    SpriteBatch spriteBatch;
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
    public void loadBotones(params fRectangle[] Botones)
    {
        this.Botones = new Boton[Botones.Length];
        fRectangle finalRect;
        for ( int i=0; i<Botones.Length; i++)
        {
            this.Botones[i] = new();
            finalRect = Botones[i];
            finalRect.x *= Width;
            finalRect.y *= Height;
            finalRect.Height *= Height;
            finalRect.Width  *= Width;
            this.Botones[i].rectanguloEnclaustro = finalRect.toIntRect();
        }
    }
    public void Write()
    {
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
        foreach( Boton boton in Botones )
            boton.Dibujar(spriteBatch);
        spriteBatch.End();
    }

    public void Load(ContentManager content)
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

    Texture2D fondo1, fondo2, mensaje;
    public Rectangle rectanguloEnclaustro;
    bool estaSeleccionado = false;
    public Boton()
    {

    }
    public void Dibujar(SpriteBatch sprBatch)
    {
        Texture2D fondoElegido = estaSeleccionado ? fondo2 : fondo1;
        sprBatch.Draw(fondoElegido, rectanguloEnclaustro, Color.OrangeRed);
        sprBatch.Draw(mensaje, rectanguloEnclaustro, Color.DarkBlue);
    }
    public void Load(ContentManager content)
    {
        fondo1 = content.Load<Texture2D>("Sprites/bcg_fondo1");
        fondo2 = content.Load<Texture2D>("Sprites/bcg_fondo2");
        mensaje = content.Load<Texture2D>("Sprites/msg_jugar");

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
}