using UnityEngine;
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
    
    /// <summary>
    /// Generate planet data including mesh vertices, triangles, and normals
    /// </summary>
    public PlanetData GeneratePlanet()
    {
        Debug.Log("🌍 Starting planet generation...");
        Debug.Log($"  - Planet radius: {radius}");
        Debug.Log($"  - Resolution: {resolution}³ = {resolution * resolution * resolution:N0} voxels");
        Debug.Log($"  - World center: {worldCenter}");
        Debug.Log($"  - Voxel size: {voxelSize:F2}");
        
        // Create signed distance field
        var sdf = GenerateSignedDistanceField();
        
        // Run custom marching cubes (correct method call - 2 parameters)
        var meshData = MarchingCubes.GenerateMesh(sdf, voxelSize);
        
        Debug.Log($"✅ Planet generation complete!");
        Debug.Log($"  - Generated {meshData.vertices.Length:N0} vertices");
        Debug.Log($"  - Generated {meshData.triangles.Length / 3:N0} triangles");
        
        return new PlanetData
        {
            vertices = meshData.vertices,
            triangles = meshData.triangles,
            normals = meshData.normals,
            center = worldCenter,
            radius = radius
        };
    }
    
    /// <summary>
    /// Generate 3D signed distance field for the planet
    /// </summary>
    private float[,,] GenerateSignedDistanceField()
    {
        var sdf = new float[resolution, resolution, resolution];
        int surfaceCrossings = 0; // Count voxels near surface for debugging
        int insideCount = 0;
        int outsideCount = 0;
        
        Debug.Log("🔄 Generating signed distance field...");
        
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
                    
                    // Add procedural noise for terrain variation
                    float noiseValue = GenerateNoise(worldPos);
                    
                    // Create signed distance field
                    // Positive = inside planet, Negative = outside planet
                    float effectiveRadius = radius + noiseValue * terrainHeightScale;
                    float sdfValue = effectiveRadius - distance;
                    
                    sdf[x, y, z] = sdfValue;
                    
                    // Count for debugging
                    if (sdfValue > 0)
                        insideCount++;
                    else
                        outsideCount++;
                    
                    // Count surface crossings for debugging
                    if (Mathf.Abs(sdfValue) < voxelSize * 2f)
                    {
                        surfaceCrossings++;
                    }
                }
            }
        }
        
        Debug.Log($"📊 SDF Statistics:");
        Debug.Log($"  - Inside voxels: {insideCount:N0}");
        Debug.Log($"  - Outside voxels: {outsideCount:N0}");
        Debug.Log($"  - Surface crossings: {surfaceCrossings:N0}");
        Debug.Log($"  - Surface ratio: {(float)surfaceCrossings / (resolution * resolution * resolution) * 100f:F1}%");
        
        if (surfaceCrossings == 0)
        {
            Debug.LogWarning("⚠️ No surface crossings detected! Planet may be too small or too large for the voxel grid.");
            Debug.LogWarning($"   Try adjusting: Radius={radius}, WorldSize={worldSize}, Resolution={resolution}");
        }
        
        return sdf;
    }
    
    /// <summary>
    /// Generate procedural noise for terrain variation
    /// </summary>
    private float GenerateNoise(Vector3 position)
    {
        // Normalize position relative to planet center for consistent noise
        Vector3 normalized = (position - worldCenter);
        float distance = normalized.magnitude;
        
        if (distance == 0) return 0f;
        
        normalized = normalized / distance;
        
        float noise = 0f;
        float amplitude = 1f;
        float frequency = noiseScale;
        
        // Generate fractal noise using multiple octaves
        for (int i = 0; i < noiseOctaves; i++)
        {
            // Use spherical coordinates for seamless noise on sphere surface
            float noiseX = normalized.x * frequency + 42f;
            float noiseY = normalized.z * frequency + 42f; // Use Z for Y to avoid pole issues
            
            noise += Mathf.PerlinNoise(noiseX, noiseY) * amplitude;
            
            amplitude *= 0.5f; // Persistence
            frequency *= 2f;   // Lacunarity
        }
        
        // Normalize noise to [-1, 1] range
        return (noise - 0.5f) * 2f;
    }
    
    /// <summary>
    /// Get spawn position for player (above planet surface)
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return worldCenter + Vector3.up * (radius + terrainHeightScale + 20f);
    }
    
    /// <summary>
    /// Get planet information for other systems
    /// </summary>
    public PlanetInfo GetPlanetInfo()
    {
        return new PlanetInfo
        {
            center = worldCenter,
            radius = radius,
            worldSize = worldSize,
            spawnPosition = GetSpawnPosition()
        };
    }
}

/// <summary>
/// Data structure for generated planet mesh
/// </summary>
[System.Serializable]
public struct PlanetData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector3 center;
    public float radius;
}

/// <summary>
/// Planet information for other game systems
/// </summary>
[System.Serializable]
public struct PlanetInfo
{
    public Vector3 center;
    public float radius;
    public float worldSize;
    public Vector3 spawnPosition;
}