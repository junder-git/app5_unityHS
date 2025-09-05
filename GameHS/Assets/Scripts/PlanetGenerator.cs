using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlanetGenerator", menuName = "Planet/Generator")]
public class PlanetGenerator : ScriptableObject
{
    [Header("Planet Settings")]
    public float radius = 200f;
    public int resolution = 48;
    public float noiseScale = 0.15f;
    public int noiseOctaves = 3;
    public float terrainHeightScale = 15f;
    
    [Header("World Configuration")]
    public Vector3 worldCenter = new Vector3(500f, 500f, 500f);
    public float worldSize = 1000f;
    
    private float voxelSize => worldSize / resolution;
    
    public PlanetData GeneratePlanet()
    {
        Debug.Log("🌍 Generating planet mesh...");
        
        // Create signed distance field
        var sdf = GenerateSignedDistanceField();
        
        // Run marching cubes (Unity's built-in or custom implementation)
        var meshData = MarchingCubes(sdf);
        
        return new PlanetData
        {
            vertices = meshData.vertices,
            triangles = meshData.triangles,
            normals = meshData.normals,
            center = worldCenter,
            radius = radius
        };
    }
    
    private float[,,] GenerateSignedDistanceField()
    {
        var sdf = new float[resolution, resolution, resolution];
        int surfaceCrossings = 0; // Count how many voxels cross the surface
        
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 worldPos = new Vector3(
                        x * voxelSize,
                        y * voxelSize,
                        z * voxelSize
                    );
                    
                    float distance = Vector3.Distance(worldPos, worldCenter);
                    
                    // Add procedural noise
                    float noiseValue = GenerateNoise(worldPos);
                    
                    // Create signed distance field
                    // Positive = inside planet, Negative = outside planet
                    float sdfValue = (radius + noiseValue * terrainHeightScale) - distance;
                    sdf[x, y, z] = sdfValue;
                    
                    // Count surface crossings for debugging
                    if (Mathf.Abs(sdfValue) < voxelSize * 2f)
                    {
                        surfaceCrossings++;
                    }
                }
            }
        }
        
        Debug.Log($"🔧 SDF Generation Complete:");
        Debug.Log($"  - Voxel size: {voxelSize:F2}");
        Debug.Log($"  - World center: {worldCenter}");
        Debug.Log($"  - Planet radius: {radius}");
        Debug.Log($"  - Surface crossings: {surfaceCrossings} voxels");
        
        return sdf;
    }
    
    private float GenerateNoise(Vector3 position)
    {
        // Unity's Perlin noise equivalent to Python's noise library
        Vector3 normalized = (position - worldCenter).normalized;
        
        float noise = 0f;
        float amplitude = 1f;
        float frequency = noiseScale;
        
        for (int i = 0; i < noiseOctaves; i++)
        {
            noise += Mathf.PerlinNoise(
                normalized.x * frequency + 42f,
                normalized.z * frequency + 42f
            ) * amplitude;
            
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        
        return noise;
    }
    
    private MeshData MarchingCubes(float[,,] sdf)
    {
        // Use SebLague's GPU-accelerated marching cubes
        var marchingCubes = new MarchingCubes();
        
        // Convert 3D array to 1D array for compute shader
        float[] points = new float[resolution * resolution * resolution];
        int index = 0;
        
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    points[index] = sdf[x, y, z];
                    index++;
                }
            }
        }
        
        // Generate mesh using SebLague's marching cubes
        var mesh = marchingCubes.GenerateMesh(points, resolution, voxelSize);
        
        // Offset vertices to world position
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += worldCenter - Vector3.one * (worldSize * 0.5f);
        }
        
        Debug.Log($"SebLague Marching cubes generated: {vertices.Length} vertices, {mesh.triangles.Length / 3} triangles");
        
        return new MeshData
        {
            vertices = vertices,
            triangles = mesh.triangles,
            normals = mesh.normals
        };
    }
}

[System.Serializable]
public struct PlanetData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector3 center;
    public float radius;
}

public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
}