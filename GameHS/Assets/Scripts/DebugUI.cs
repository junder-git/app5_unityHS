using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    [Header("UI References")]
    public Text debugText;
    public GameObject debugPanel;
    
    private PlayerController player;
    private bool isVisible = true;
    
    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        
        if (debugPanel == null)
        {
            CreateDebugUI();
        }
    }
    
    private void CreateDebugUI()
    {
        // Create debug UI canvas
        GameObject canvasObject = new GameObject("Debug Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // Create debug panel
        debugPanel = new GameObject("Debug Panel");
        debugPanel.transform.SetParent(canvasObject.transform, false);
        
        RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(400, 200);
        
        // Create debug text
        GameObject textObject = new GameObject("Debug Text");
        textObject.transform.SetParent(debugPanel.transform, false);
        
        debugText = textObject.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        debugText.fontSize = 12;
        debugText.color = Color.yellow;
        debugText.alignment = TextAnchor.UpperLeft;
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    private void Update()
    {
        if (debugText != null && player != null && isVisible)
        {
            UpdateDebugInfo();
        }
    }
    
    private void UpdateDebugInfo()
    {
        Vector3 pos = player.GetPosition();
        Vector3 vel = player.GetVelocity();
        float distanceFromCenter = player.GetDistanceFromCenter();
        bool grounded = player.IsGrounded();
        float fps = 1f / Time.deltaTime;
        
        string debugInfo = $"Position: ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})\n" +
                          $"Distance from center: {distanceFromCenter:F1}\n" +
                          $"Planet radius: {FindObjectOfType<GameManager>().PlanetRadius:F0}\n" +
                          $"Grounded: {grounded} | FPS: {fps:F1}\n" +
                          $"Move speed: {player.moveSpeed:F0} | Jump: {player.jumpForce:F0}\n" +
                          $"Velocity: {vel.magnitude:F1}\n" +
                          $"Camera: Third Person\n" +
                          $"World: 1000×1000×1000 units";
        
        debugText.text = debugInfo;
    }
    
    public void ToggleDisplay()
    {
        isVisible = !isVisible;
        if (debugPanel != null)
        {
            debugPanel.SetActive(isVisible);
        }
        Debug.Log($"🐛 Debug info {(isVisible ? "shown" : "hidden")}");
    }
}