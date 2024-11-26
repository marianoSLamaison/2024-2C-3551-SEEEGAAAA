using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

class Contador{
    public float time;
    public int startingTime;
    Texture2D numeros;
    SpriteBatch sprBatch;
    fRectangle[] rectangulos;
    Vector2 dims;
    public Contador(int time, float vWidth, float vHeight, SpriteBatch sprBatch)
    {
        rectangulos = new fRectangle[5]{
            new fRectangle(0.4f,0.01f, 0.05f, 0.05f),
            new fRectangle(0.45f,0.01f, 0.05f, 0.05f),
            new fRectangle(0.50f,0.01f, 0.05f, 0.05f),
            new fRectangle(0.55f,0.01f, 0.05f, 0.05f),
            new fRectangle(0.60f,0.01f, 0.05f, 0.05f)
        };
        this.time = time;
        startingTime = time;
        dims = new(vWidth, vHeight);
        this.sprBatch = sprBatch;
    }
    public void Load(ContentManager cont)
    {
        numeros = cont.Load<Texture2D>("Numeros");
    }
    private String getStringTime()
    {
        int minutos = (int)time / 60;
        int segundos = (int)time % 60;
        return $"{minutos:D2}:{segundos:D2}";
        
    }
    private int getSimbol(char c)
    {
        int ret = 0;
        switch (c)
        {
            case '0':
            ret = 0;
            break;
            case '1':
            ret = 1;
            break;
            case '2':
            ret = 2;
            break;
            case '3':
            ret = 3;
            break;
            case '4':
            ret = 4;
            break;
            case '5':
            ret = 5;
            break;
            case '6':
            ret = 6;
            break;
            case '7':
            ret = 7;
            break;
            case '8':
            ret = 8;
            break;
            case '9':
            ret = 9;
            break;
            default:
            ret = 10;
            break;
        }
        return ret;
    }
    public void Dibujar()
    {
        sprBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null);
        String numer = getStringTime();
        for ( int i = 0; i<5; i++)
        {
            sprBatch.Draw(numeros, rectangulos[i].toIntRect(dims.X, dims.Y),
                    new Rectangle(84 * getSimbol(numer[i]), 0, 82,82 ),
                    Color.Orange
                    );

        }
        sprBatch.End();
    }
}