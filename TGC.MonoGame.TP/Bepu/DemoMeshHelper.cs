using BepuPhysics.Collidables;
using System;
using System.Numerics;
using BepuUtilities.Memory;
using BepuUtilities;
using BepuPhysics.Trees;
using System.Runtime.CompilerServices;

namespace DemoContentLoader
{
    public static class DemoMeshHelper
{
    public static Mesh LoadModel(ContentArchive content, BufferPool pool, string contentName, Vector3 scaling)
    {
        var meshContent = content.Load<MeshContent>(contentName);
        pool.Take<Triangle>(meshContent.Triangles.Length, out var triangles);
        for (int i = 0; i < meshContent.Triangles.Length; ++i)
        {
            triangles[i] = new Triangle(meshContent.Triangles[i].A, meshContent.Triangles[i].B, meshContent.Triangles[i].C);
        }
        return new Mesh(triangles, scaling, pool);
    }

    public static Mesh CreateFan(int triangleCount, float radius, Vector3 scaling, BufferPool pool)
    {
        var anglePerTriangle = 2 * MathF.PI / triangleCount;
        pool.Take<Triangle>(triangleCount, out var triangles);

        for (int i = 0; i < triangleCount; ++i)
        {
            var firstAngle = i * anglePerTriangle;
            var secondAngle = ((i + 1) % triangleCount) * anglePerTriangle;

            ref var triangle = ref triangles[i];
            triangle.A = new Vector3(radius * MathF.Cos(firstAngle), 0, radius * MathF.Sin(firstAngle));
            triangle.B = new Vector3(radius * MathF.Cos(secondAngle), 0, radius * MathF.Sin(secondAngle));
            triangle.C = new Vector3();
        }
        return new Mesh(triangles, scaling, pool);
    }

    public static Mesh CreateDeformedPlane(int width, int height, Func<int, int, Vector3> deformer, Vector3 scaling, BufferPool pool, IThreadDispatcher dispatcher = null)
    {
        pool.Take<Vector3>(width * height, out var vertices);
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                vertices[width * j + i] = deformer(i, j);
            }
        }

        var quadWidth = width - 1;
        var quadHeight = height - 1;
        var triangleCount = quadWidth * quadHeight * 2;
        pool.Take<Triangle>(triangleCount, out var triangles);

        for (int i = 0; i < quadWidth; ++i)
        {
            for (int j = 0; j < quadHeight; ++j)
            {
                var triangleIndex = (j * quadWidth + i) * 2;
                ref var triangle0 = ref triangles[triangleIndex];
                ref var v00 = ref vertices[width * j + i];
                ref var v01 = ref vertices[width * j + i + 1];
                ref var v10 = ref vertices[width * (j + 1) + i];
                ref var v11 = ref vertices[width * (j + 1) + i + 1];
                triangle0.A = v00;
                triangle0.B = v01;
                triangle0.C = v10;
                ref var triangle1 = ref triangles[triangleIndex + 1];
                triangle1.A = v01;
                triangle1.B = v11;
                triangle1.C = v10;
            }
        }
        pool.Return(ref vertices);
        return new Mesh(triangles, scaling, pool);
    }

    /// <summary>
    /// Creates a bunch of nodes and associates them with leaves with absolutely no regard for where the leaves are.
    /// </summary>
    static void CreateDummyNodes(ref Tree tree, int nodeIndex, int nodeLeafCount, ref int leafCounter)
    {
        ref var node = ref tree.Nodes[nodeIndex];
        node.A.LeafCount = nodeLeafCount / 2;
        if (node.A.LeafCount > 1)
        {
            node.A.Index = nodeIndex + 1;
            tree.Metanodes[node.A.Index] = new Metanode { IndexInParent = 0, Parent = nodeIndex };
            CreateDummyNodes(ref tree, node.A.Index, node.A.LeafCount, ref leafCounter);
        }
        else
        {
            tree.Leaves[leafCounter] = new Leaf(nodeIndex, 0);
            node.A.Index = Tree.Encode(leafCounter++);
        }
        node.B.LeafCount = nodeLeafCount - node.A.LeafCount;
        if (node.B.LeafCount > 1)
        {
            node.B.Index = nodeIndex + node.A.LeafCount;
            tree.Metanodes[node.B.Index] = new Metanode { IndexInParent = 1, Parent = nodeIndex };
            CreateDummyNodes(ref tree, node.B.Index, node.B.LeafCount, ref leafCounter);
        }
        else
        {
            tree.Leaves[leafCounter] = new Leaf(nodeIndex, 1);
            node.B.Index = Tree.Encode(leafCounter++);
        }
    }

}

struct RaceTrack
    {
        public float QuadrantRadius;
        public Vector2 Center;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetClosestPoint(in Vector2 point, float laneOffset, out Vector2 closestPoint, out Vector2 flowDirection)
        {
            var localPoint = point - Center;
            var quadrantCenter = new Vector2(localPoint.X < 0 ? -QuadrantRadius : QuadrantRadius, localPoint.Y < 0 ? -QuadrantRadius : QuadrantRadius);
            var quadrantCenterToPoint = new Vector2(localPoint.X, localPoint.Y) - quadrantCenter;
            var distanceToQuadrantCenter = quadrantCenterToPoint.Length();
            var on01Or10 = localPoint.X * localPoint.Y < 0;
            var signedLaneOffset = on01Or10 ? -laneOffset : laneOffset;
            var toCircleEdgeDirection = distanceToQuadrantCenter > 0 ? quadrantCenterToPoint * (1f / distanceToQuadrantCenter) : new Vector2(QuadrantRadius + signedLaneOffset, 0);
            var offsetFromQuadrantCircle = (QuadrantRadius + signedLaneOffset) * toCircleEdgeDirection;
            closestPoint = quadrantCenter + offsetFromQuadrantCircle;
            var perpendicular = new Vector2(toCircleEdgeDirection.Y, -toCircleEdgeDirection.X);
            flowDirection = on01Or10 ? perpendicular : -perpendicular;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDistance(in Vector2 point)
        {
            GetClosestPoint(point, 0, out var closest, out _);
            return Vector2.Distance(closest, point);
        }
    }
}




