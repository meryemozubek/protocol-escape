using UnityEngine;
using UnityEngine.UI;

/// <summary>Native uGUI geometry. Uses the standard UI material; no shader, texture or package dependency.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MainMenuChromeGraphic : MaskableGraphic
{
    public enum Shape { Surface, Slashes, PlayIcon, BookIcon, PowerIcon, Corner }
    public Shape shape;
    public Color fillTop = new Color32(9, 26, 39, 240);
    public Color fillBottom = new Color32(5, 17, 27, 247);
    public Color edge = new Color32(74, 227, 244, 230);
    [Min(0f)] public float corner = 28f;
    [Min(0.5f)] public float stroke = 1.8f;
    public bool glow = true;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width < 1f || r.height < 1f) return;
        if (shape == Shape.Surface)
        {
            Polygon(vh, r, corner, fillTop, fillBottom);
            if (glow)
            {
                Ring(vh, Expand(r, 5f), corner + 5f, 7f, Alpha(edge, 0.07f));
                Ring(vh, Expand(r, 2f), corner + 2f, 4f, Alpha(edge, 0.14f));
            }
            Ring(vh, r, corner, stroke, edge);
        }
        else if (shape == Shape.Slashes)
        {
            float step = r.width / 5f, skew = r.height * 0.8f;
            for (int i = 0; i < 4; i++)
            {
                float x = r.xMin + i * step;
                Quad(vh, new Vector2(x,r.yMin), new Vector2(x+step*0.7f,r.yMin),
                    new Vector2(x+step*0.7f+skew,r.yMax), new Vector2(x+skew,r.yMax),
                    Alpha(edge, 0.3f + i*0.2f));
            }
        }
        else if (shape == Shape.PlayIcon)
        {
            int n = vh.currentVertCount;
            Vertex(vh,new Vector2(r.xMin,r.yMin),edge); Vertex(vh,new Vector2(r.xMin,r.yMax),edge);
            Vertex(vh,new Vector2(r.xMax,r.center.y),edge); vh.AddTriangle(n,n+1,n+2);
        }
        else if (shape == Shape.BookIcon)
        {
            Vector2 c=r.center; float x=r.width*0.45f,y=r.height*0.42f;
            Vector2[] left={new Vector2(c.x,c.y-y),new Vector2(c.x-x,c.y-y*0.65f),
                new Vector2(c.x-x,c.y+y),new Vector2(c.x,c.y+y*0.7f),new Vector2(c.x,c.y-y)};
            Vector2[] right={new Vector2(c.x,c.y-y),new Vector2(c.x+x,c.y-y*0.65f),
                new Vector2(c.x+x,c.y+y),new Vector2(c.x,c.y+y*0.7f)};
            Path(vh,left,stroke,edge); Path(vh,right,stroke,edge);
        }
        else if (shape == Shape.PowerIcon)
        {
            float radius=Mathf.Min(r.width,r.height)*0.42f;
            Vector2 previous=Circle(r.center,radius,135f);
            for(int i=1;i<=32;i++)
            {
                Vector2 next=Circle(r.center,radius,135f+i*(270f/32f));
                Line(vh,previous,next,stroke,edge);previous=next;
            }
            Line(vh,new Vector2(r.center.x,r.center.y+radius*0.1f),
                new Vector2(r.center.x,r.center.y+radius*1.25f),stroke,edge);
        }
        else
        {
            Path(vh,new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),
                new Vector2(r.xMax,r.yMax)},stroke,edge);
        }
    }
    static Vector2 Circle(Vector2 c,float radius,float degrees)
    {float a=degrees*Mathf.Deg2Rad;return c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
    static Color Alpha(Color c,float multiplier){c.a*=multiplier;return c;}
    static Rect Expand(Rect r,float amount){return Rect.MinMaxRect(r.xMin-amount,r.yMin-amount,r.xMax+amount,r.yMax+amount);}
    static Vector2[] Corners(Rect r,float cut)
    {
        float b=Mathf.Clamp(cut,0f,Mathf.Min(r.width,r.height)*0.45f);
        return new[]{new Vector2(r.xMin+b,r.yMin),new Vector2(r.xMax-b,r.yMin),
            new Vector2(r.xMax,r.yMin+b),new Vector2(r.xMax,r.yMax-b),
            new Vector2(r.xMax-b,r.yMax),new Vector2(r.xMin+b,r.yMax),
            new Vector2(r.xMin,r.yMax-b),new Vector2(r.xMin,r.yMin+b)};
    }
    void Vertex(VertexHelper vh,Vector2 p,Color tint)
    {var v=UIVertex.simpleVert;v.position=p;v.color=tint*color;v.uv0=Vector2.zero;vh.AddVert(v);}
    void Polygon(VertexHelper vh,Rect r,float cut,Color top,Color bottom)
    {
        int n=vh.currentVertCount;Vertex(vh,r.center,Color.Lerp(bottom,top,0.5f));
        foreach(var p in Corners(r,cut))Vertex(vh,p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)));
        for(int i=0;i<8;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%8);
    }
    void Ring(VertexHelper vh,Rect r,float cut,float width,Color tint)
    {
        Rect inner=Rect.MinMaxRect(r.xMin+width,r.yMin+width,r.xMax-width,r.yMax-width);
        if(inner.width<=0f||inner.height<=0f)return;
        var outer=Corners(r,cut);var inside=Corners(inner,Mathf.Max(0f,cut-width*0.6f));
        for(int i=0;i<8;i++)Quad(vh,outer[i],outer[(i+1)%8],inside[(i+1)%8],inside[i],tint);
    }
    void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
    {
        int n=vh.currentVertCount;Vertex(vh,a,tint);Vertex(vh,b,tint);Vertex(vh,c,tint);Vertex(vh,d,tint);
        vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
    }
    void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
    {Vector2 delta=b-a;if(delta.sqrMagnitude<0.0001f)return;var p=new Vector2(-delta.y,delta.x).normalized*width*0.5f;Quad(vh,a-p,b-p,b+p,a+p,tint);}
    void Path(VertexHelper vh,Vector2[] points,float width,Color tint)
    {for(int i=1;i<points.Length;i++)Line(vh,points[i-1],points[i],width,tint);}
}
