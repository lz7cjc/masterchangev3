using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

[System.Serializable]
public class SportZone
{
    public string zoneName;
    public Vector3 position;
    public Vector3 size;
    public Color zoneColor = Color.white;
    public GameObject[] prefabsToSpawn;
}

public class SportsMarinaGenerator : MonoBehaviour
{
    [Header("Marina Layout")]
    public float marinaWidth = 200f;
    public float marinaLength = 300f;
    public float waterLevel = 0f;

    [Header("Dock Settings")]
    public int numberOfDocks = 6;
    public float dockWidth = 4f;
    public float dockLength = 30f;
    public Material dockMaterial;

    [Header("Sport Zones")]
    public SportZone[] sportZones;

    [Header("Prefabs")]
    public GameObject dockPrefab;
    public GameObject poolPrefab;
    public GameObject tennisCourtPrefab;
    public GameObject beachTennisCourtPrefab;
    public GameObject bikeTrailPrefab;
    public GameObject waterSportsPlatformPrefab;
    public GameObject lightPrefab;
    public GameObject signagePrefab;

    [Header("Materials")]
    public Material waterMaterial;
    public Material sandMaterial;
    public Material concreteMaterial;
    public Material grassMaterial;

    [Header("Lighting")]
    public bool generateLighting = true;
    public float lightSpacing = 25f;
    public Color lightColor = Color.white;

    private GameObject marinaParent;

    void Start()
    {
        if (sportZones == null || sportZones.Length == 0)
        {
            InitializeDefaultSportZones();
        }

        GenerateMarina();
    }

    void InitializeDefaultSportZones()
    {
        sportZones = new SportZone[]
        {
            new SportZone
            {
                zoneName = "Water Sports Hub",
                position = new Vector3(-80, 0, 50),
                size = new Vector3(60, 5, 80),
                zoneColor = Color.blue
            },
            new SportZone
            {
                zoneName = "Tennis Complex",
                position = new Vector3(50, 0, 80),
                size = new Vector3(80, 5, 60),
                zoneColor = Color.green
            },
            new SportZone
            {
                zoneName = "Beach Tennis Area",
                position = new Vector3(20, 0, -30),
                size = new Vector3(50, 5, 40),
                zoneColor = Color.yellow
            },
            new SportZone
            {
                zoneName = "Swimming Complex",
                position = new Vector3(-20, 0, -80),
                size = new Vector3(70, 5, 50),
                zoneColor = Color.cyan
            },
            new SportZone
            {
                zoneName = "Mountain Bike Trails",
                position = new Vector3(80, 0, -60),
                size = new Vector3(60, 5, 80),
                zoneColor = Color.red
            }
        };
    }

    void GenerateMarina()
    {
        // Create parent object
        marinaParent = new GameObject("Sports Marina");
        marinaParent.transform.position = transform.position;

        // Generate base terrain
        GenerateBaseTerrain();

        // Generate water areas
        GenerateWaterAreas();

        // Generate docks
        GenerateDocks();

        // Generate sport zones
        GenerateSportZones();

        // Generate walkways
        GenerateWalkways();

        // Generate lighting
        if (generateLighting)
        {
            GenerateLighting();
        }

        // Generate signage
        GenerateSignage();

        Debug.Log("Sports Marina generated successfully!");
    }

    void GenerateBaseTerrain()
    {
        GameObject terrain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terrain.name = "Marina Base";
        terrain.transform.parent = marinaParent.transform;
        terrain.transform.position = Vector3.zero;
        terrain.transform.localScale = new Vector3(marinaWidth, 1f, marinaLength);

        if (concreteMaterial != null)
        {
            terrain.GetComponent<Renderer>().material = concreteMaterial;
        }
    }

    void GenerateWaterAreas()
    {
        // Main marina water
        GameObject mainWater = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainWater.name = "Marina Water";
        mainWater.transform.parent = marinaParent.transform;
        mainWater.transform.position = new Vector3(-marinaWidth * 0.3f, waterLevel, 0);
        mainWater.transform.localScale = new Vector3(marinaWidth * 0.4f, 0.1f, marinaLength * 0.8f);

        if (waterMaterial != null)
        {
            mainWater.GetComponent<Renderer>().material = waterMaterial;
        }

        // Ocean connection
        GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ocean.name = "Ocean";
        ocean.transform.parent = marinaParent.transform;
        ocean.transform.position = new Vector3(-marinaWidth * 0.7f, waterLevel, 0);
        ocean.transform.localScale = new Vector3(marinaWidth * 0.6f, 0.1f, marinaLength);

        if (waterMaterial != null)
        {
            ocean.GetComponent<Renderer>().material = waterMaterial;
        }
    }

    void GenerateDocks()
    {
        GameObject docksParent = new GameObject("Docks");
        docksParent.transform.parent = marinaParent.transform;

        float dockSpacing = marinaLength * 0.8f / numberOfDocks;

        for (int i = 0; i < numberOfDocks; i++)
        {
            GameObject dock;

            if (dockPrefab != null)
            {
                dock = Instantiate(dockPrefab, docksParent.transform);
                dock.name = $"Dock {i + 1}";
            }
            else
            {
                // Fallback: create simple dock
                dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dock.name = $"Dock {i + 1}";
                dock.transform.parent = docksParent.transform;
                dock.transform.localScale = new Vector3(dockLength, 0.5f, dockWidth);

                if (dockMaterial != null)
                {
                    dock.GetComponent<Renderer>().material = dockMaterial;
                }
            }

            dock.transform.position = new Vector3(
                -marinaWidth * 0.15f,
                waterLevel + 0.3f,
                -marinaLength * 0.4f + i * dockSpacing
            );
        }
    }

    void GenerateSportZones()
    {
        GameObject zonesParent = new GameObject("Sport Zones");
        zonesParent.transform.parent = marinaParent.transform;

        foreach (SportZone zone in sportZones)
        {
            GameObject zoneObject = new GameObject(zone.zoneName);
            zoneObject.transform.parent = zonesParent.transform;
            zoneObject.transform.position = zone.position;

            // Create zone base
            GameObject zoneBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zoneBase.name = zone.zoneName + " Base";
            zoneBase.transform.parent = zoneObject.transform;
            zoneBase.transform.localPosition = Vector3.zero;
            zoneBase.transform.localScale = zone.size;

            // Apply zone-specific materials and create facilities
            CreateZoneFacilities(zoneObject, zone);

            // Add zone marker
            CreateZoneMarker(zoneObject, zone);
        }
    }

    void CreateZoneFacilities(GameObject zoneObject, SportZone zone)
    {
        switch (zone.zoneName)
        {
            case "Water Sports Hub":
                CreateWaterSportsFacilities(zoneObject);
                break;
            case "Tennis Complex":
                CreateTennisFacilities(zoneObject);
                break;
            case "Beach Tennis Area":
                CreateBeachTennisFacilities(zoneObject);
                break;
            case "Swimming Complex":
                CreateSwimmingFacilities(zoneObject);
                break;
            case "Mountain Bike Trails":
                CreateBikeFacilities(zoneObject);
                break;
        }
    }

    void CreateWaterSportsFacilities(GameObject parent)
    {
        // Use water sports platform prefab if available
        if (waterSportsPlatformPrefab != null)
        {
            GameObject platform = Instantiate(waterSportsPlatformPrefab, parent.transform);
            platform.transform.localPosition = new Vector3(-15, 0.5f, -10);
            platform.name = "Water Sports Platform";
        }
        else
        {
            // Fallback: create simple platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Water Sports Platform";
            platform.transform.parent = parent.transform;
            platform.transform.localPosition = new Vector3(-15, 0.5f, -10);
            platform.transform.localScale = new Vector3(12f, 1f, 8f);
        }

        // Create beach access regardless
        GameObject beach = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beach.name = "Beach Access";
        beach.transform.parent = parent.transform;
        beach.transform.localPosition = new Vector3(0, 0, -20);
        beach.transform.localScale = new Vector3(40, 0.5f, 15);

        if (sandMaterial != null)
        {
            beach.GetComponent<Renderer>().material = sandMaterial;
        }
    }

    void CreateTennisFacilities(GameObject parent)
    {
        // Create multiple tennis courts using prefab if available
        for (int i = 0; i < 3; i++)
        {
            GameObject court;
            if (tennisCourtPrefab != null)
            {
                court = Instantiate(tennisCourtPrefab, parent.transform);
                court.name = $"Tennis Court {i + 1}";
            }
            else
            {
                // Fallback: create simple court
                court = GameObject.CreatePrimitive(PrimitiveType.Cube);
                court.name = $"Tennis Court {i + 1}";
                court.transform.parent = parent.transform;
                court.transform.localScale = new Vector3(23.77f, 0.1f, 10.97f);

                if (grassMaterial != null)
                {
                    court.GetComponent<Renderer>().material = grassMaterial;
                }
            }

            court.transform.localPosition = new Vector3(i * 30 - 30, 0.1f, 0);
        }

        // Add clubhouse
        GameObject clubhouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
        clubhouse.name = "Tennis Clubhouse";
        clubhouse.transform.parent = parent.transform;
        clubhouse.transform.localPosition = new Vector3(0, 2, 25);
        clubhouse.transform.localScale = new Vector3(20, 4, 10);
    }

    void CreateBeachTennisFacilities(GameObject parent)
    {
        // Create beach tennis courts using prefab if available
        for (int i = 0; i < 2; i++)
        {
            GameObject court;
            if (beachTennisCourtPrefab != null)
            {
                court = Instantiate(beachTennisCourtPrefab, parent.transform);
                court.name = $"Beach Tennis Court {i + 1}";
            }
            else
            {
                // Fallback: create simple court
                court = GameObject.CreatePrimitive(PrimitiveType.Cube);
                court.name = $"Beach Tennis Court {i + 1}";
                court.transform.parent = parent.transform;
                court.transform.localScale = new Vector3(16f, 0.1f, 8f);

                if (sandMaterial != null)
                {
                    court.GetComponent<Renderer>().material = sandMaterial;
                }
            }

            court.transform.localPosition = new Vector3(i * 20 - 10, 0.1f, 0);
        }
    }

    void CreateSwimmingFacilities(GameObject parent)
    {
        // Use swimming pool prefab if available
        if (poolPrefab != null)
        {
            GameObject pool = Instantiate(poolPrefab, parent.transform);
            pool.transform.localPosition = new Vector3(0, 0, 0);
            pool.name = "Swimming Pool Complex";
        }
        else
        {
            // Fallback: create simple pool
            GameObject olympicPool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            olympicPool.name = "Olympic Pool";
            olympicPool.transform.parent = parent.transform;
            olympicPool.transform.localPosition = new Vector3(0, 1f, 0);
            olympicPool.transform.localScale = new Vector3(50f, 2f, 25f);

            if (waterMaterial != null)
            {
                olympicPool.GetComponent<Renderer>().material = waterMaterial;
            }

            // Add diving pool
            GameObject divingPool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divingPool.name = "Diving Pool";
            divingPool.transform.parent = parent.transform;
            divingPool.transform.localPosition = new Vector3(-30, 1.5f, 0);
            divingPool.transform.localScale = new Vector3(15f, 3f, 15f);

            if (waterMaterial != null)
            {
                divingPool.GetComponent<Renderer>().material = waterMaterial;
            }
        }
    }

    void CreateBikeFacilities(GameObject parent)
    {
        // Use bike trail prefab if available
        if (bikeTrailPrefab != null)
        {
            GameObject trail = Instantiate(bikeTrailPrefab, parent.transform);
            trail.transform.localPosition = Vector3.zero;
            trail.name = "Mountain Bike Trail System";
        }
        else
        {
            // Fallback: create simple trail markers
            GameObject rentalStation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rentalStation.name = "Bike Rental Station";
            rentalStation.transform.parent = parent.transform;
            rentalStation.transform.localPosition = new Vector3(20, 1, 20);
            rentalStation.transform.localScale = new Vector3(8, 2, 6);

            // Simple trail markers
            for (int i = 0; i < 5; i++)
            {
                GameObject trailMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trailMarker.name = $"Trail Marker {i + 1}";
                trailMarker.transform.parent = parent.transform;
                trailMarker.transform.localPosition = new Vector3(
                    -20 + i * 10,
                    1f,
                    -30 + i * 15
                );
                trailMarker.transform.localScale = new Vector3(1, 2, 1);

                Material markerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                markerMat.color = new Color(1f, 0.5f, 0f); // Orange
                trailMarker.GetComponent<Renderer>().material = markerMat;
            }
        }
    }

    void CreateZoneMarker(GameObject zoneObject, SportZone zone)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = zone.zoneName + " Marker";
        marker.transform.parent = zoneObject.transform;
        marker.transform.localPosition = new Vector3(0, 3, zone.size.z * 0.5f + 5);
        marker.transform.localScale = new Vector3(2, 6, 2);

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        Material markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMaterial.color = zone.zoneColor;
        markerRenderer.material = markerMaterial;

        // Add emission for visibility
        markerMaterial.EnableKeyword("_EMISSION");
        markerMaterial.SetColor("_EmissionColor", zone.zoneColor * 0.5f);
    }

    void GenerateWalkways()
    {
        GameObject walkwaysParent = new GameObject("Walkways");
        walkwaysParent.transform.parent = marinaParent.transform;

        // Main promenade along the water
        GameObject promenade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        promenade.name = "Main Promenade";
        promenade.transform.parent = walkwaysParent.transform;
        promenade.transform.position = new Vector3(10, 0.1f, 0);
        promenade.transform.localScale = new Vector3(6, 0.2f, marinaLength * 0.9f);

        if (concreteMaterial != null)
        {
            promenade.GetComponent<Renderer>().material = concreteMaterial;
        }

        // Connecting walkways between zones
        for (int i = 0; i < sportZones.Length - 1; i++)
        {
            CreateConnectingWalkway(walkwaysParent, sportZones[i], sportZones[i + 1], i);
        }
    }

    void CreateConnectingWalkway(GameObject parent, SportZone from, SportZone to, int index)
    {
        Vector3 start = from.position;
        Vector3 end = to.position;
        Vector3 midPoint = (start + end) / 2;
        float distance = Vector3.Distance(start, end);

        GameObject walkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walkway.name = $"Connecting Walkway {index + 1}";
        walkway.transform.parent = parent.transform;
        walkway.transform.position = midPoint + new Vector3(0, 0.1f, 0);
        walkway.transform.localScale = new Vector3(3, 0.2f, distance);
        walkway.transform.LookAt(end);

        if (concreteMaterial != null)
        {
            walkway.GetComponent<Renderer>().material = concreteMaterial;
        }
    }

    void GenerateLighting()
    {
        GameObject lightingParent = new GameObject("Marina Lighting");
        lightingParent.transform.parent = marinaParent.transform;

        // Perimeter lighting
        for (float x = -marinaWidth / 2; x <= marinaWidth / 2; x += lightSpacing)
        {
            for (float z = -marinaLength / 2; z <= marinaLength / 2; z += lightSpacing)
            {
                if (Mathf.Abs(x) >= marinaWidth / 2 - 5 || Mathf.Abs(z) >= marinaLength / 2 - 5)
                {
                    CreateStreetLight(lightingParent, new Vector3(x, 0, z));
                }
            }
        }

        // Zone-specific lighting
        foreach (SportZone zone in sportZones)
        {
            CreateZoneLighting(lightingParent, zone);
        }
    }

    void CreateStreetLight(GameObject parent, Vector3 position)
    {
        GameObject lightPost;

        if (lightPrefab != null)
        {
            lightPost = Instantiate(lightPrefab, parent.transform);
            lightPost.transform.position = position;
            lightPost.name = "Street Light";
        }
        else
        {
            // Create simple light post fallback
            lightPost = new GameObject("Street Light");
            lightPost.transform.parent = parent.transform;
            lightPost.transform.position = position;

            // Post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.parent = lightPost.transform;
            post.transform.localPosition = new Vector3(0, 4, 0);
            post.transform.localScale = new Vector3(0.2f, 4, 0.2f);

            // Light
            GameObject lightObj = new GameObject("Light");
            lightObj.transform.parent = lightPost.transform;
            lightObj.transform.localPosition = new Vector3(0, 8, 0);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = lightColor;
            light.intensity = 2f;
            light.range = 15f;
            light.shadows = LightShadows.Soft;
        }
    }

    void CreateZoneLighting(GameObject parent, SportZone zone)
    {
        // Create flood lights for sports zones
        GameObject floodLight = new GameObject($"{zone.zoneName} Flood Light");
        floodLight.transform.parent = parent.transform;
        floodLight.transform.position = zone.position + new Vector3(0, 12, 0);

        Light light = floodLight.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = lightColor;
        light.intensity = 3f;
        light.range = 30f;
        light.spotAngle = 60f;
        light.shadows = LightShadows.Soft;

        // Point the light down at the zone
        floodLight.transform.LookAt(zone.position);
    }

    void GenerateSignage()
    {
        GameObject signageParent = new GameObject("Signage");
        signageParent.transform.parent = marinaParent.transform;

        // Main marina sign
        CreateMainSign(signageParent);

        // Zone signs
        foreach (SportZone zone in sportZones)
        {
            CreateZoneSign(signageParent, zone);
        }

        // Directional signs
        CreateDirectionalSigns(signageParent);
    }

    void CreateMainSign(GameObject parent)
    {
        GameObject mainSign;

        if (signagePrefab != null)
        {
            mainSign = Instantiate(signagePrefab, parent.transform);
            mainSign.transform.position = new Vector3(0, 5, marinaLength / 2 - 10);
            mainSign.name = "Marina Main Sign";
        }
        else
        {
            // Fallback: create simple sign
            mainSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainSign.name = "Marina Main Sign";
            mainSign.transform.parent = parent.transform;
            mainSign.transform.position = new Vector3(0, 5, marinaLength / 2 - 10);
            mainSign.transform.localScale = new Vector3(15, 8, 1);
        }

        // Add text component for Unity UI
        CreateSignText(mainSign, "SPORTS MARINA", 24);
    }

    void CreateZoneSign(GameObject parent, SportZone zone)
    {
        GameObject zoneSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zoneSign.name = $"{zone.zoneName} Sign";
        zoneSign.transform.parent = parent.transform;
        zoneSign.transform.position = zone.position + new Vector3(0, 3, zone.size.z / 2 + 8);
        zoneSign.transform.localScale = new Vector3(8, 4, 0.5f);

        CreateSignText(zoneSign, zone.zoneName.ToUpper(), 16);
    }

    void CreateDirectionalSigns(GameObject parent)
    {
        Vector3[] signPositions = {
            new Vector3(20, 2, 0),
            new Vector3(-20, 2, 50),
            new Vector3(0, 2, -50)
        };

        string[] signTexts = {
            "TENNIS COURTS →",
            "← WATER SPORTS",
            "SWIMMING POOL ↑"
        };

        for (int i = 0; i < signPositions.Length; i++)
        {
            GameObject dirSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirSign.name = $"Directional Sign {i + 1}";
            dirSign.transform.parent = parent.transform;
            dirSign.transform.position = signPositions[i];
            dirSign.transform.localScale = new Vector3(6, 2, 0.3f);

            CreateSignText(dirSign, signTexts[i], 12);
        }
    }

    void CreateSignText(GameObject sign, string text, int fontSize)
    {
        // This is a placeholder for text creation
        // In a real implementation, you'd use TextMeshPro or UI Canvas
        GameObject textObj = new GameObject("Sign Text");
        textObj.transform.parent = sign.transform;
        textObj.transform.localPosition = new Vector3(0, 0, -0.6f);

        // Add a simple colored cube as text placeholder
        GameObject textCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        textCube.name = text;
        textCube.transform.parent = textObj.transform;
        textCube.transform.localPosition = Vector3.zero;
        textCube.transform.localScale = new Vector3(0.8f, 0.3f, 0.1f);

        Material textMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        textMaterial.color = Color.white;
        textCube.GetComponent<Renderer>().material = textMaterial;
    }

    [ContextMenu("Regenerate Marina")]
    public void RegenerateMarina()
    {
        // Clean up existing marina
        if (marinaParent != null)
        {
            DestroyImmediate(marinaParent);
        }

        // Generate new marina
        GenerateMarina();
    }

    [ContextMenu("Clear Marina")]
    public void ClearMarina()
    {
        if (marinaParent != null)
        {
            DestroyImmediate(marinaParent);
        }
    }
}