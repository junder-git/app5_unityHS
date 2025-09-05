using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Planet Generation")]
    public PlanetGenerator planetGenerator;
    
    [Header("Prefabs")]
    public GameObject playerPrefab;
    
    [Header("Materials")]
    public Material planetMaterial;
    
    public Vector3 PlanetCenter { get; private set; }
    public float PlanetRadius { get; private set; }
    
    private GameObject planetObject;
    private PlayerController playerController;
    
    private void Start()
    {
        Debug.Log("🚀 Starting Unity Spherical Planet Game");
        
        GeneratePlanet();
        SpawnPlayer();
        SetupScene();
        
        Debug.Log("✅ Game initialization complete!");
    }
    
    private void GeneratePlanet()
    {
        if (planetGenerator == null)
        {
            Debug.LogError("❌ Planet Generator not assigned!");
            return;
        }
        
        // Generate planet data
        PlanetData planetData = planetGenerator.GeneratePlanet();
        PlanetCenter = planetData.center;
        PlanetRadius = planetData.radius;
        
        // Create planet mesh
        Mesh planetMesh = new Mesh();
        planetMesh.vertices = planetData.vertices;
        planetMesh.triangles = planetData.triangles;
        planetMesh.normals = planetData.normals;
        planetMesh.RecalculateBounds();
        
        // Create planet GameObject
        planetObject = new GameObject("Planet");
        var meshFilter = planetObject.AddComponent<MeshFilter>();
        var meshRenderer = planetObject.AddComponent<MeshRenderer>();
        var meshCollider = planetObject.AddComponent<MeshCollider>();
        
        meshFilter.mesh = planetMesh;
        meshRenderer.material = planetMaterial;
        meshCollider.sharedMesh = planetMesh;
        
        Debug.Log($"✅ Planet generated: {planetData.vertices.Length} vertices, {planetData.triangles.Length/3} triangles");
    }
    
    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player Prefab not assigned!");
            return;
        }
        
        // Spawn player above planet surface
        Vector3 spawnPosition = PlanetCenter + Vector3.up * (PlanetRadius + 20f);
        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerController = playerObject.GetComponent<PlayerController>();
        
        Debug.Log($"🚀 Player spawned at: {spawnPosition}");
    }
    
    private void SetupScene()
    {
        // Setup lighting
        RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.4f);
        
        // Create directional light (sun)
        GameObject sunObject = new GameObject("Sun");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.8f);
        sun.intensity = 1.2f;
        sunObject.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        
        // Setup camera
        Camera.main.farClipPlane = 3000f;
        Camera.main.fieldOfView = 75f;
        
        Debug.Log("✅ Scene setup complete");
    }
}