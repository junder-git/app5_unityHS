using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom Marching Cubes implementation for Unity
/// Generates mesh from 3D signed distance field
/// </summary>
public class MarchingCubes
{
    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
    }
    
    /// <summary>
    /// Generate mesh from signed distance field using marching cubes algorithm
    /// </summary>
    /// <param name="sdf">3D array of signed distance values</param>
    /// <param name="voxelSize">Size of each voxel in world units</param>
    /// <returns>Generated mesh data</returns>
    public static MeshData GenerateMesh(float[,,] sdf, float voxelSize)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        
        int resolution = sdf.GetLength(0);
        
        Debug.Log($"🔧 Starting marching cubes on {resolution}³ grid...");
        
        // Iterate through each cube in the voxel grid
        for (int x = 0; x < resolution - 1; x++)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int z = 0; z < resolution - 1; z++)
                {
                    ProcessCube(x, y, z, sdf, voxelSize, vertices, triangles, normals);
                }
            }
        }
        
        Debug.Log($"⚡ Marching cubes complete: {vertices.Count} vertices, {triangles.Count / 3} triangles");
        
        return new MeshData
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray()
        };
    }
    
    /// <summary>
    /// Process a single cube in the voxel grid
    /// </summary>
    private static void ProcessCube(int x, int y, int z, float[,,] sdf, float voxelSize,
                                   List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        // Get 8 corner values of the cube
        float[] cubeValues = new float[8]
        {
            sdf[x, y, z],           // 0: bottom-left-back
            sdf[x + 1, y, z],       // 1: bottom-right-back
            sdf[x + 1, y, z + 1],   // 2: bottom-right-front
            sdf[x, y, z + 1],       // 3: bottom-left-front
            sdf[x, y + 1, z],       // 4: top-left-back
            sdf[x + 1, y + 1, z],   // 5: top-right-back
            sdf[x + 1, y + 1, z + 1], // 6: top-right-front
            sdf[x, y + 1, z + 1]    // 7: top-left-front
        };
        
        // Calculate cube configuration (which corners are inside surface)
        int cubeConfig = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeValues[i] > 0) cubeConfig |= 1 << i;
        }
        
        // Skip if cube is entirely inside or outside surface
        if (cubeConfig == 0 || cubeConfig == 255) return;
        
        // Get cube corner positions in world space
        Vector3[] cubeCorners = new Vector3[8]
        {
            new Vector3(x * voxelSize, y * voxelSize, z * voxelSize),
            new Vector3((x + 1) * voxelSize, y * voxelSize, z * voxelSize),
            new Vector3((x + 1) * voxelSize, y * voxelSize, (z + 1) * voxelSize),
            new Vector3(x * voxelSize, y * voxelSize, (z + 1) * voxelSize),
            new Vector3(x * voxelSize, (y + 1) * voxelSize, z * voxelSize),
            new Vector3((x + 1) * voxelSize, (y + 1) * voxelSize, z * voxelSize),
            new Vector3((x + 1) * voxelSize, (y + 1) * voxelSize, (z + 1) * voxelSize),
            new Vector3(x * voxelSize, (y + 1) * voxelSize, (z + 1) * voxelSize)
        };
        
        // Find edge intersections and create triangles
        CreateTrianglesForCube(cubeConfig, cubeCorners, cubeValues, vertices, triangles, normals);
    }
    
    /// <summary>
    /// Create triangles for a single cube based on its configuration
    /// </summary>
    private static void CreateTrianglesForCube(int cubeConfig, Vector3[] corners, float[] values,
                                              List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        // Define the 12 edges of a cube (each edge connects two corners)
        int[,] edgeConnection = new int[12, 2]
        {
            {0,1}, {1,2}, {2,3}, {3,0}, // bottom face edges (y=0)
            {4,5}, {5,6}, {6,7}, {7,4}, // top face edges (y=1)
            {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
        };
        
        // Find intersection points on cube edges
        Vector3[] edgeVertices = new Vector3[12];
        bool[] hasIntersection = new bool[12];
        
        for (int i = 0; i < 12; i++)
        {
            int corner1 = edgeConnection[i, 0];
            int corner2 = edgeConnection[i, 1];
            
            float val1 = values[corner1];
            float val2 = values[corner2];
            
            // Check if edge crosses the surface (different signs)
            if ((val1 > 0) != (val2 > 0))
            {
                hasIntersection[i] = true;
                
                // Linear interpolation to find exact intersection point
                float t = Mathf.Abs(val1) / (Mathf.Abs(val1) + Mathf.Abs(val2));
                edgeVertices[i] = Vector3.Lerp(corners[corner1], corners[corner2], t);
            }
        }
        
        // Collect intersection points
        List<Vector3> intersectionPoints = new List<Vector3>();
        List<int> intersectionEdges = new List<int>();
        
        for (int i = 0; i < 12; i++)
        {
            if (hasIntersection[i])
            {
                intersectionPoints.Add(edgeVertices[i]);
                intersectionEdges.Add(i);
            }
        }
        
        // Better triangulation: Create triangles to form a coherent surface
        if (intersectionPoints.Count >= 3)
        {
            // Calculate centroid of intersection points
            Vector3 centroid = Vector3.zero;
            foreach (var point in intersectionPoints)
            {
                centroid += point;
            }
            centroid /= intersectionPoints.Count;
            
            // Create triangles from centroid to each pair of adjacent points
            for (int i = 0; i < intersectionPoints.Count; i++)
            {
                int nextIndex = (i + 1) % intersectionPoints.Count;
                
                // Only create triangle if the points are reasonably spaced
                float distance = Vector3.Distance(intersectionPoints[i], intersectionPoints[nextIndex]);
                if (distance > 0.01f) // Avoid degenerate triangles
                {
                    // Add vertices
                    int startIndex = vertices.Count;
                    vertices.Add(centroid);
                    vertices.Add(intersectionPoints[i]);
                    vertices.Add(intersectionPoints[nextIndex]);
                    
                    // Add triangle indices
                    triangles.Add(startIndex);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                    
                    // Calculate normal
                    Vector3 edge1 = intersectionPoints[i] - centroid;
                    Vector3 edge2 = intersectionPoints[nextIndex] - centroid;
                    Vector3 normal = Vector3.Cross(edge1, edge2).normalized;
                    
                    // Ensure normal points outward (away from planet center)
                    // Assume planet center is roughly at the cube center
                    Vector3 cubeCenter = Vector3.zero;
                    for (int c = 0; c < 8; c++)
                    {
                        cubeCenter += corners[c];
                    }
                    cubeCenter /= 8f;
                    
                    Vector3 outwardDirection = (centroid - cubeCenter).normalized;
                    if (Vector3.Dot(normal, outwardDirection) < 0)
                    {
                        normal = -normal;
                        // Flip triangle winding
                        int temp = triangles[triangles.Count - 2];
                        triangles[triangles.Count - 2] = triangles[triangles.Count - 1];
                        triangles[triangles.Count - 1] = temp;
                    }
                    
                    // Add normals for all three vertices
                    normals.Add(normal);
                    normals.Add(normal);
                    normals.Add(normal);
                }
            }
            
            // For cases with exactly 4 intersection points, also create a quad
            if (intersectionPoints.Count == 4)
            {
                // Create two additional triangles to form a proper quad
                int startIndex = vertices.Count;
                vertices.Add(intersectionPoints[0]);
                vertices.Add(intersectionPoints[2]);
                vertices.Add(intersectionPoints[1]);
                vertices.Add(intersectionPoints[3]);
                
                // First triangle
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 2);
                
                // Second triangle
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 2);
                
                // Calculate normals for quad
                Vector3 edge1 = intersectionPoints[1] - intersectionPoints[0];
                Vector3 edge2 = intersectionPoints[2] - intersectionPoints[0];
                Vector3 quadNormal = Vector3.Cross(edge1, edge2).normalized;
                
                // Add normals for quad vertices
                for (int i = 0; i < 6; i++) // 6 vertices for 2 triangles
                {
                    normals.Add(quadNormal);
                }
            }
        }
    }
}