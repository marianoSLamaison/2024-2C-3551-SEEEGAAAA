using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Utils
{
    static class Matematicas
    {
        public static Vector3 clampV(Vector3 valor, Vector3 minimo, Vector3 maximo)
        {
            valor.X = Math.Clamp(valor.X, minimo.X, maximo.X);
            valor.Y = Math.Clamp(valor.Y, minimo.Y, maximo.Y);
            valor.Z = Math.Clamp(valor.Z, minimo.Z, maximo.Z);
            return valor; 
        }
        public static Vector2 clampV(Vector2 valor, Vector2 maximo, Vector2 minimo)
        {
            valor.X = Math.Clamp(valor.X, minimo.X, maximo.X);
            valor.Y = Math.Clamp(valor.Y, minimo.Y, maximo.Y);
            return valor;
        }
        public static Vector3 AssV3(Vector2 v2) => new( v2.X, 0f, v2.Y);
        public static double wrapf(double value, double min, double max)
        {
            return value > max ? min : value < min ? max : value;
        }
        public static Vector2 AssV2(Vector3 input) => new(input.X, input.Z);
        public static Vector3 XZOrthogonal(Vector3 input) => new(-input.Z, input.Y, input.X);
        public static Vector3 AssXNA(System.Numerics.Vector3 input) => new(input.X, input.Y, input.Z);
    }
    static class Commons
    {

        public static List<T> map<T>(List<T> list, Func<T, T> func)
        {
            List<T> ret = new List<T>(list.Capacity);
            foreach(T item in list)
                ret.Add(func(item));
            return ret;
        }
        public static void map<T>( T[] lista, Action<T> func)
        {
            foreach(T item in lista)
            {
                func(item);
            }
        }
        public static int FindArrayValue<T>( T[] lista, Func<T,bool> checker )
        {
            int i = 0;
            foreach( T element in lista )
            {
                if (checker(element))
                    return i;
                i ++;
            }
            return -1;
        }
    }
}