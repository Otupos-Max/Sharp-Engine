using System.Numerics;

namespace SharpEngine;

public sealed class Camera3D
{
    public Camera3D(Vector3 position, Vector3 target)
    {
        Position = position;
        Target = target;
    }

    public Vector3 Position { get; set; }

    public Vector3 Target { get; set; }

    public Vector3 Up { get; set; } = Vector3.UnitY;

    public float FieldOfViewDegrees { get; set; } = 60f;

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 1000f;

    internal Matrix4x4 CreateViewProjection(float aspectRatio)
    {
        if (Vector3.DistanceSquared(Position, Target) < 0.000001f)
        {
            throw new InvalidOperationException("The camera position and target must be different.");
        }

        if (!float.IsFinite(FieldOfViewDegrees) || FieldOfViewDegrees <= 0f || FieldOfViewDegrees >= 180f)
        {
            throw new InvalidOperationException("Camera field of view must be between 0 and 180 degrees.");
        }

        if (!float.IsFinite(NearPlane) ||
            !float.IsFinite(FarPlane) ||
            NearPlane <= 0f ||
            FarPlane <= NearPlane)
        {
            throw new InvalidOperationException("Camera planes must satisfy 0 < near < far.");
        }

        if (!float.IsFinite(Position.X) ||
            !float.IsFinite(Position.Y) ||
            !float.IsFinite(Position.Z) ||
            !float.IsFinite(Target.X) ||
            !float.IsFinite(Target.Y) ||
            !float.IsFinite(Target.Z) ||
            !float.IsFinite(Up.X) ||
            !float.IsFinite(Up.Y) ||
            !float.IsFinite(Up.Z) ||
            Up.LengthSquared() < 0.000001f ||
            Vector3.Cross(Target - Position, Up).LengthSquared() < 0.000001f)
        {
            throw new InvalidOperationException("Camera position, target and up vector must define a finite view direction.");
        }

        return Matrix4x4.CreateLookAt(Position, Target, Up) *
               Matrix4x4.CreatePerspectiveFieldOfView(
                   MathF.PI * FieldOfViewDegrees / 180f,
                   aspectRatio,
                   NearPlane,
                   FarPlane);
    }
}
