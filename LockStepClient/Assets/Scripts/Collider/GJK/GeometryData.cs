using System;
using UnityEngine;
[Serializable]
public class Triangle2D : SupportFunction2D
{
    public Vector2 a;
    public Vector2 b;
    public Vector2 c;

    
    public override Vector2 centroid
    {
        get
        {
            return (a + b + c) / 3;
        }
        
    }
    
    
    //支撑函数，传入一个方向，返回该方向上最远的点
    public override Vector2 Support(Vector2 direction)
    {
        float dotA = Vector2.Dot(direction, a);
        float dotB = Vector2.Dot(direction, b);
        float dotC = Vector2.Dot(direction, c);

        if (dotA > dotB && dotA > dotC)
        {
            return a;
        }
        else if (dotB > dotA && dotB > dotC)
        {
            return b;
        }
        else
        {
            return c;
        }
    }
}
[Serializable]
public class Quad2D : SupportFunction2D
{
    public Vector2 a;
    public Vector2 b;
    public Vector2 c;
    public Vector2 d;

    public override Vector2 centroid
    {
        get
        {
            return (a + b + c + d) / 4;
        }
    }
    
    
    //支撑函数，传入一个方向，返回该方向上最远的点
    public override Vector2 Support(Vector2 direction)
    {
        float maxDot = float.NegativeInfinity;
        Vector2 supportPoint = Vector2.zero; // 或者 this.a 作为初始值

        float dotA = Vector2.Dot(direction, a);
        if (dotA > maxDot) { maxDot = dotA; supportPoint = a; }

        float dotB = Vector2.Dot(direction, b);
        if (dotB > maxDot) { maxDot = dotB; supportPoint = b; }

        float dotC = Vector2.Dot(direction, c);
        if (dotC > maxDot) { maxDot = dotC; supportPoint = c; }

        float dotD = Vector2.Dot(direction, d);
        if (dotD > maxDot) { maxDot = dotD; supportPoint = d; }

        return supportPoint;
    }
}

//用于检测的单纯形
public enum Simplex2DType
{
    None,
    Line,
    Triangle
}
public class Simplex2D
{
    public Vector2 newPoint;
    public Vector2 b;
    public Vector2 c;

    public Vector2 normal;

    private int count;

    public Simplex2DType type
    {
        get
        {
            switch (count)
            {
                case 0:
                case 1:
                    return Simplex2DType.None;
                case 2:
                    return Simplex2DType.Line;
                case 3:
                    return Simplex2DType.Triangle;
                default:
                    return Simplex2DType.None;
            }
        }
    }
    public void AppendPoint(Vector2 point)
    {
        c = b;
        b = newPoint;
        newPoint = point;
        count = Mathf.Min(count + 1 , 3);
    }

    public void RemoveB()
    {
        b = c;
        count = Mathf.Max(count - 1, 0);
    }

    public void RemoveC()
    {
        count = Mathf.Max(count - 1, 0);
    }

    public int Count()
    {
        return count;
    }
}
