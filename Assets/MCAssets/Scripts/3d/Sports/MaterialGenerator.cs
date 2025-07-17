using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MaterialGenerator : MonoBehaviour
{
    [Header("Material Generation")]
    [Tooltip("Folder path within Assets to save materials")]
    public string materialsFolderPath = "Materials/Sports Marina";

    [ContextMenu("Generate All Materials")]
    public void GenerateAllMaterials()
    {
#if UNITY_EDITOR
        // Ensure the materials folder exists
        string fullPath = "Assets/" + materialsFolderPath;
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }

        // Generate all materials
        CreateWaterMaterials();
        CreateCourtMaterials();
        CreateStructuralMaterials();
        CreateEquipmentMaterials();
        CreateSignageMaterials();
        CreateTrailMaterials();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"All materials generated and saved to: {fullPath}");
        Debug.Log("Materials are ready to assign to your generators!");
#else
        Debug.LogWarning("Material generation only works in the Unity Editor!");
#endif
    }

#if UNITY_EDITOR
    void CreateWaterMaterials()
    {
        // Water Material
        Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        waterMat.name = "Marina Water";
        waterMat.color = new Color(0.2f, 0.6f, 0.9f, 0.7f);
        waterMat.SetFloat("_Metallic", 0.1f);
        waterMat.SetFloat("_Smoothness", 0.9f);

        // Enable transparency
        waterMat.SetFloat("_Surface", 1); // Transparent
        waterMat.SetFloat("_Blend", 0); // Alpha
        waterMat.renderQueue = 3000;

        // Add subtle emission for water effect
        waterMat.EnableKeyword("_EMISSION");
        waterMat.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.5f, 1f) * 0.2f);

        SaveMaterial(waterMat, "Water_Material");

        // Pool Water (cleaner, more chlorinated look)
        Material poolWaterMat = new Material(waterMat);
        poolWaterMat.name = "Pool Water";
        poolWaterMat.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        poolWaterMat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.6f, 1f) * 0.15f);
        SaveMaterial(poolWaterMat, "Pool_Water_Material");
    }

    void CreateCourtMaterials()
    {
        // Tennis Court Surface
        Material tennisCourt = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        tennisCourt.name = "Tennis Court Surface";
        tennisCourt.color = new Color(0.1f, 0.5f, 0.2f); // Dark green
        tennisCourt.SetFloat("_Metallic", 0f);
        tennisCourt.SetFloat("_Smoothness", 0.3f);
        SaveMaterial(tennisCourt, "Tennis_Court_Surface");

        // Court Lines
        Material courtLines = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        courtLines.name = "Court Lines";
        courtLines.color = Color.white;
        courtLines.SetFloat("_Metallic", 0f);
        courtLines.SetFloat("_Smoothness", 0.2f);
        SaveMaterial(courtLines, "Court_Lines");

        // Tennis Net
        Material tennisNet = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        tennisNet.name = "Tennis Net";
        tennisNet.color = Color.white;
        tennisNet.SetFloat("_Metallic", 0f);
        tennisNet.SetFloat("_Smoothness", 0.1f);
        SaveMaterial(tennisNet, "Tennis_Net");

        // Sand Material (for beach tennis)
        Material sand = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sand.name = "Beach Sand";
        sand.color = new Color(0.9f, 0.8f, 0.6f); // Sandy beige
        sand.SetFloat("_Metallic", 0f);
        sand.SetFloat("_Smoothness", 0.1f);
        SaveMaterial(sand, "Sand_Material");

        // Grass Material
        Material grass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        grass.name = "Grass";
        grass.color = new Color(0.2f, 0.6f, 0.1f); // Vibrant green
        grass.SetFloat("_Metallic", 0f);
        grass.SetFloat("_Smoothness", 0.2f);
        SaveMaterial(grass, "Grass_Material");

        // Pool Deck
        Material poolDeck = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        poolDeck.name = "Pool Deck";
        poolDeck.color = new Color(0.8f, 0.8f, 0.75f); // Light concrete
        poolDeck.SetFloat("_Metallic", 0f);
        poolDeck.SetFloat("_Smoothness", 0.4f);
        SaveMaterial(poolDeck, "Pool_Deck_Material");

        // Lane Markers
        Material laneMarkers = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        laneMarkers.name = "Lane Markers";
        laneMarkers.color = new Color(0.2f, 0.4f, 0.8f); // Pool blue
        laneMarkers.SetFloat("_Metallic", 0f);
        laneMarkers.SetFloat("_Smoothness", 0.3f);
        SaveMaterial(laneMarkers, "Lane_Markers");
    }

    void CreateStructuralMaterials()
    {
        // Concrete
        Material concrete = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        concrete.name = "Concrete";
        concrete.color = new Color(0.6f, 0.6f, 0.6f); // Medium gray
        concrete.SetFloat("_Metallic", 0f);
        concrete.SetFloat("_Smoothness", 0.3f);
        SaveMaterial(concrete, "Concrete_Material");

        // Wood (for docks)
        Material wood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wood.name = "Marina Wood";
        wood.color = new Color(0.6f, 0.4f, 0.2f); // Weathered wood brown
        wood.SetFloat("_Metallic", 0f);
        wood.SetFloat("_Smoothness", 0.2f);
        SaveMaterial(wood, "Wood_Material");

        // Metal (for cleats, railings)
        Material metal = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        metal.name = "Marina Metal";
        metal.color = new Color(0.7f, 0.7f, 0.7f); // Stainless steel
        metal.SetFloat("_Metallic", 0.8f);
        metal.SetFloat("_Smoothness", 0.9f);
        SaveMaterial(metal, "Metal_Material");

        // Platform Material
        Material platform = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        platform.name = "Platform Surface";
        platform.color = new Color(0.5f, 0.5f, 0.5f); // Industrial gray
        platform.SetFloat("_Metallic", 0.1f);
        platform.SetFloat("_Smoothness", 0.4f);
        SaveMaterial(platform, "Platform_Material");

        // Railing Material
        Material railing = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        railing.name = "Safety Railing";
        railing.color = Color.white;
        railing.SetFloat("_Metallic", 0.2f);
        railing.SetFloat("_Smoothness", 0.7f);
        SaveMaterial(railing, "Railing_Material");

        // Equipment Storage
        Material equipment = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        equipment.name = "Equipment Material";
        equipment.color = new Color(0.3f, 0.4f, 0.6f); // Blue-gray
        equipment.SetFloat("_Metallic", 0.1f);
        equipment.SetFloat("_Smoothness", 0.5f);
        SaveMaterial(equipment, "Equipment_Material");
    }

    void CreateEquipmentMaterials()
    {
        // Net Posts
        Material netPost = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        netPost.name = "Net Posts";
        netPost.color = new Color(0.3f, 0.3f, 0.3f); // Dark gray
        netPost.SetFloat("_Metallic", 0.5f);
        netPost.SetFloat("_Smoothness", 0.7f);
        SaveMaterial(netPost, "Net_Post_Material");

        // Light Pole
        Material lightPole = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lightPole.name = "Light Pole";
        lightPole.color = new Color(0.4f, 0.4f, 0.4f); // Medium gray
        lightPole.SetFloat("_Metallic", 0.6f);
        lightPole.SetFloat("_Smoothness", 0.8f);
        SaveMaterial(lightPole, "Light_Pole_Material");

        // Light Fixture
        Material lightFixture = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lightFixture.name = "Light Fixture";
        lightFixture.color = new Color(0.2f, 0.2f, 0.2f); // Dark fixture
        lightFixture.SetFloat("_Metallic", 0.7f);
        lightFixture.SetFloat("_Smoothness", 0.9f);
        SaveMaterial(lightFixture, "Light_Fixture_Material");
    }

    void CreateSignageMaterials()
    {
        // Sign Board
        Material signBoard = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signBoard.name = "Sign Board";
        signBoard.color = Color.white;
        signBoard.SetFloat("_Metallic", 0f);
        signBoard.SetFloat("_Smoothness", 0.6f);
        SaveMaterial(signBoard, "Sign_Board_Material");

        // Sign Post
        Material signPost = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signPost.name = "Sign Post";
        signPost.color = new Color(0.5f, 0.5f, 0.5f); // Gray post
        signPost.SetFloat("_Metallic", 0.3f);
        signPost.SetFloat("_Smoothness", 0.6f);
        SaveMaterial(signPost, "Sign_Post_Material");

        // Sign Text
        Material signText = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signText.name = "Sign Text";
        signText.color = Color.black;
        signText.SetFloat("_Metallic", 0f);
        signText.SetFloat("_Smoothness", 0.1f);
        SaveMaterial(signText, "Sign_Text_Material");
    }

    void CreateTrailMaterials()
    {
        // Trail Surface
        Material trailSurface = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        trailSurface.name = "Trail Surface";
        trailSurface.color = new Color(0.4f, 0.3f, 0.2f); // Dirt brown
        trailSurface.SetFloat("_Metallic", 0f);
        trailSurface.SetFloat("_Smoothness", 0.1f);
        SaveMaterial(trailSurface, "Trail_Surface_Material");

        // Trail Markers
        Material trailMarker = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        trailMarker.name = "Trail Markers";
        trailMarker.color = new Color(1f, 0.5f, 0f); // Bright orange
        trailMarker.SetFloat("_Metallic", 0f);
        trailMarker.SetFloat("_Smoothness", 0.4f);

        // Add emission for visibility
        trailMarker.EnableKeyword("_EMISSION");
        trailMarker.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f) * 0.3f);
        SaveMaterial(trailMarker, "Trail_Marker_Material");
    }

    void SaveMaterial(Material material, string fileName)
    {
        string path = $"Assets/{materialsFolderPath}/{fileName}.mat";
        AssetDatabase.CreateAsset(material, path);
        Debug.Log($"Created material: {material.name} at {path}");
    }

    [ContextMenu("Create Material Assignment Guide")]
    public void CreateMaterialAssignmentGuide()
    {
        string guide = @"MATERIAL ASSIGNMENT GUIDE
=============================

MAIN MARINA GENERATOR:
- Water Material → Marina Water
- Sand Material → Beach Sand  
- Concrete Material → Concrete
- Grass Material → Grass
- Dock Material → Marina Wood

TENNIS COURT GENERATOR:
- Court Surface Material → Tennis Court Surface
- Line Material → Court Lines
- Net Material → Tennis Net
- Post Material → Net Posts

BEACH TENNIS GENERATOR:
- Sand Material → Beach Sand
- Line Material → Court Lines
- Net Material → Tennis Net

SWIMMING POOL GENERATOR:
- Water Material → Pool Water
- Pool Deck Material → Pool Deck
- Lane Material → Lane Markers

DOCK GENERATOR:
- Wood Material → Marina Wood
- Metal Material → Marina Metal

WATER SPORTS PLATFORM:
- Platform Material → Platform Surface
- Railing Material → Safety Railing
- Equipment Material → Equipment Material

STREET LIGHT GENERATOR:
- Pole Material → Light Pole
- Fixture Material → Light Fixture

SIGNAGE GENERATOR:
- Sign Material → Sign Board
- Post Material → Sign Post
- Text Material → Sign Text

BIKE TRAIL GENERATOR:
- Trail Material → Trail Surface
- Marker Material → Trail Markers

All materials saved to: Assets/" + materialsFolderPath + @"
";

        Debug.Log(guide);

        // Save to text file as well
        string filePath = $"Assets/{materialsFolderPath}/Material_Assignment_Guide.txt";
        System.IO.File.WriteAllText(filePath, guide);
        AssetDatabase.Refresh();
        Debug.Log($"Material assignment guide saved to: {filePath}");
    }
#endif
}