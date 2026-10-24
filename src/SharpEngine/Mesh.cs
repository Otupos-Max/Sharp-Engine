using System.Numerics;

namespace SharpEngine;

public sealed class Mesh
{
    private readonly Vector3[] _vertices;
    private readonly int[] _triangleIndices;

    public Mesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangleIndices);

        _vertices = vertices.ToArray();
        _triangleIndices = triangleIndices.ToArray();
        if (_vertices.Length == 0)
        {
            throw new ArgumentException("A mesh must contain at least one vertex.", nameof(vertices));
        }

        if (_vertices.Any(vertex =>
                !float.IsFinite(vertex.X) ||
                !float.IsFinite(vertex.Y) ||
                !float.IsFinite(vertex.Z)))
        {
            throw new ArgumentException("Mesh vertices must be finite.", nameof(vertices));
        }

        if (_triangleIndices.Length == 0 || _triangleIndices.Length % 3 != 0)
        {
            throw new ArgumentException("Triangle indices must contain one or more complete triangles.", nameof(triangleIndices));
        }

        if (_triangleIndices.Any(index => index < 0 || index >= _vertices.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(triangleIndices), "Triangle indices must reference an existing vertex.");
        }
    }

    public ReadOnlyMemory<Vector3> Vertices => _vertices;

    public ReadOnlyMemory<int> TriangleIndices => _triangleIndices;

    public static Mesh CreateCube(float size = 1f)
    {
        if (!float.IsFinite(size) || size <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Cube size must be finite and positive.");
        }

        var half = size / 2f;
        return new Mesh(
            [
                new(-half, -half, -half),
                new(half, -half, -half),
                new(half, half, -half),
                new(-half, half, -half),
                new(-half, -half, half),
                new(half, -half, half),
                new(half, half, half),
                new(-half, half, half)
            ],
            [
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5
            ]);
    }
}
