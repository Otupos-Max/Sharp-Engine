using System.Numerics;
using SharpEngine;
using Xunit;

namespace SharpEngine.Tests;

public sealed class MeshTests
{
    [Fact]
    public void CubeContainsEightVerticesAndTwelveTriangles()
    {
        var cube = Mesh.CreateCube(2f);

        Assert.Equal(8, cube.Vertices.Length);
        Assert.Equal(36, cube.TriangleIndices.Length);
        Assert.All(cube.Vertices.ToArray(), vertex => Assert.InRange(vertex.X, -1f, 1f));
    }

    [Fact]
    public void MeshRejectsIndicesOutsideVertexArray()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Mesh([Vector3.Zero, Vector3.One], [0, 1, 2]));
    }

    [Fact]
    public void MeshRejectsIncompleteTriangles()
    {
        Assert.Throws<ArgumentException>(() =>
            new Mesh([Vector3.Zero, Vector3.One, Vector3.UnitX], [0, 1]));
    }
}
