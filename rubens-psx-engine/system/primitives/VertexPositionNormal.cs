using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public struct VertexPositionNormal : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionNormal(Vector3 pos, Vector3 normal)
    {
        Position = pos;
        Normal = normal;
    }
}
