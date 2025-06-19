using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class PrimitiveCube
{
    public VertexBuffer VertexBuffer;
    public IndexBuffer IndexBuffer;

    public PrimitiveCube(GraphicsDevice graphicsDevice)
    {
        var verts = new List<VertexPositionNormal>();
        var indices = new List<ushort>();

        Vector3[] corners = {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
        };

        int[][] faces = {
            new[] { 0, 1, 2, 3 }, // Back
            new[] { 5, 4, 7, 6 }, // Front
            new[] { 4, 0, 3, 7 }, // Left
            new[] { 1, 5, 6, 2 }, // Right
            new[] { 3, 2, 6, 7 }, // Top
            new[] { 4, 5, 1, 0 }  // Bottom
        };

        Vector3[] normals = {
            Vector3.Backward,
            Vector3.Forward,
            Vector3.Left,
            Vector3.Right,
            Vector3.Up,
            Vector3.Down
        };

        ushort idx = 0;
        for (int f = 0; f < 6; f++)
        {
            var normal = normals[f];
            var face = faces[f];

            verts.Add(new VertexPositionNormal(corners[face[0]], normal));
            verts.Add(new VertexPositionNormal(corners[face[1]], normal));
            verts.Add(new VertexPositionNormal(corners[face[2]], normal));
            verts.Add(new VertexPositionNormal(corners[face[3]], normal));

            indices.Add((ushort)(idx + 0));
            indices.Add((ushort)(idx + 1));
            indices.Add((ushort)(idx + 2));

            indices.Add((ushort)(idx + 2));
            indices.Add((ushort)(idx + 3));
            indices.Add((ushort)(idx + 0));
            idx += 4;
        }

        VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormal), verts.Count, BufferUsage.WriteOnly);
        VertexBuffer.SetData(verts.ToArray());

        IndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
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
