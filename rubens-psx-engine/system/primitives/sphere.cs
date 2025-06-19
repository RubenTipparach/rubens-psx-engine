using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public class PrimitiveSphere
{
    public VertexBuffer VertexBuffer;
    public IndexBuffer IndexBuffer;

    public PrimitiveSphere(GraphicsDevice device, int tessellation = 12)
    {
        var verts = new List<VertexPositionNormal>();
        var indices = new List<ushort>();

        for (int i = 0; i <= tessellation; i++)
        {
            float lat = MathF.PI * i / tessellation;
            float y = MathF.Cos(lat);
            float r = MathF.Sin(lat);

            for (int j = 0; j <= tessellation; j++)
            {
                float lon = 2 * MathF.PI * j / tessellation;
                float x = r * MathF.Cos(lon);
                float z = r * MathF.Sin(lon);

                Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));
                verts.Add(new VertexPositionNormal(normal, normal));
            }
        }

        int stride = tessellation + 1;
        for (int i = 0; i < tessellation; i++)
        {
            for (int j = 0; j < tessellation; j++)
            {
                int a = i * stride + j;
                int b = (i + 1) * stride + j;
                int c = (i + 1) * stride + j + 1;
                int d = i * stride + j + 1;

                indices.Add((ushort)a);
                indices.Add((ushort)b);
                indices.Add((ushort)c);

                indices.Add((ushort)c);
                indices.Add((ushort)d);
                indices.Add((ushort)a);
            }
        }

        VertexBuffer = new VertexBuffer(device, typeof(VertexPositionNormal), verts.Count, BufferUsage.WriteOnly);
        VertexBuffer.SetData(verts.ToArray());

        IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        IndexBuffer.SetData(indices.ToArray());
    }

    public void Draw(GraphicsDevice device, BasicEffect effect)
    {
        device.SetVertexBuffer(VertexBuffer);
        device.Indices = IndexBuffer;

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);
        }
    }
}
