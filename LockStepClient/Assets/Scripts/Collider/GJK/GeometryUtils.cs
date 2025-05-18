using UnityEngine;

public static class GeometryUtils
{
    
    public static Vector3 TripleProd(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(Vector3.Cross(a, b), c);
    }
    
    
    public static bool Case2D(SupportFunction2D a,SupportFunction2D b,int iterStep = 20)
    {
        Vector2 centroidA = a.centroid;
        Vector2 centroidB = b.centroid;

        //初始的查询向量用两个质心的连线向量
        Vector2 initialNormal = (centroidA - centroidB).normalized;
        
        return GJK_2D(a, b, initialNormal, out Simplex2D finalSimplex2D,iterStep);
        
        
    }
    
    private static bool GJK_2D<TA,TB>(TA a,TB b, Vector2 normal , out Simplex2D finalSimplex2D,int iterStep)
    where TA : SupportFunction2D where TB : SupportFunction2D
    {
        //初始化状态,找到第一个点
        //首先获取两个凸型的支持点
        //MaX(Support(A - B, d)) = Support(A, d) - Support(B, -d)
        Vector2 pointOnA = a.Support(normal);
        Vector2 pointOnB = b.Support(-normal);
        //计算闵差
        Vector2 mSub = pointOnA - pointOnB;
        Simplex2D simplex2D = new Simplex2D();
        simplex2D.AppendPoint(mSub);
        normal = Vector2.zero - mSub;
        finalSimplex2D = null;
        for (int i = 0; i < iterStep; ++i)
        {
            pointOnA = a.Support(normal);
            pointOnB = b.Support(-normal);
            mSub = pointOnA - pointOnB;
            simplex2D.AppendPoint(mSub);
            
            //大于零说明,新的计算出的单纯型的极点，它和原来的那个单纯性极点，分别在原来的normal的垂线的两侧
            //这个点不在原点与法线构成的正空间
            if (Vector2.Dot(mSub, normal) < 0)
            {
                finalSimplex2D = simplex2D;
                return false;
            }

            bool hasZero = CheckSimplex2D(ref simplex2D, ref normal);
            if (hasZero)
            {
                finalSimplex2D = simplex2D;
                return true;
            }
            
            if(i == iterStep - 1)
            {
            }
        }

        return false;
    }

    private static bool CheckSimplex2D(ref Simplex2D simplex2D, ref Vector2 normal)
    {
        if (simplex2D.type == Simplex2DType.Line)
        {
            Vector2 ab = simplex2D.b - simplex2D.newPoint;
            Vector2 ao = Vector2.zero - simplex2D.newPoint;
            normal = GeometryUtils.TripleProd(ab,ao,ab);
            return false;
        }
        else if (simplex2D.type == Simplex2DType.Triangle)
        {
            //单纯形是一个三角形，判断原点落在哪一个区域。
            var ab = simplex2D.b - simplex2D.newPoint;
            var ac = simplex2D.c - simplex2D.newPoint;
            var ao = Vector2.zero -simplex2D.newPoint;
            var normal_ab = GeometryUtils.TripleProd(ac, ab, ab);
            var normal_ac =  GeometryUtils.TripleProd(ab, ac, ac);

            if (Vector2.Dot(normal_ab, ao) > 0) //region ab
            {
                simplex2D.RemoveC();
                normal = normal_ab;
                return false;
            }
            else if (Vector2.Dot(normal_ac, ao) > 0) //region ac
            {
                simplex2D.RemoveB();
                normal = normal_ac;
                return false;
            }
            return true;
        }
        return false;
    }

}
