using UnityEngine;

public class WaterSportsPlatformGenerator : MonoBehaviour
{
    [Header("Platform Settings")]
    public Material platformMaterial;
    public Material railingMaterial;
    public Material equipmentMaterial;

    [ContextMenu("Generate Water Sports Platform")]
    public void GenerateWaterSportsPlatform()
    {
        GameObject platform = new GameObject("Water Sports Platform");
        platform.transform.position = transform.position;

        // Main platform deck
        CreatePlatformDeck(platform);

        // Safety railings
        CreateSafetyRailings(platform);

        // Equipment storage
        CreateEquipmentStorage(platform);

        // Launch ramp
        CreateLaunchRamp(platform);

        // Kite surfing equipment
        CreateKiteSurfingSetup(platform);

        // Surfboard racks
        CreateSurfboardRacks(platform);

        Debug.Log("Water Sports Platform prefab generated!");
    }

    void CreatePlatformDeck(GameObject parent)
    {
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Platform Deck";
        deck.transform.parent = parent.transform;
        deck.transform.localPosition = new Vector3(0, 0.5f, 0);
        deck.transform.localScale = new Vector3(12f, 1f, 8f);

        if (platformMaterial != null)
            deck.GetComponent<Renderer>().material = platformMaterial;
        else
        {
            Material defaultPlatform = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultPlatform.color = new Color(0.7f, 0.7f, 0.7f);
            deck.GetComponent<Renderer>().material = defaultPlatform;
        }

        // Support pillars
        CreateSupportPillars(parent);
    }

    void CreateSupportPillars(GameObject parent)
    {
        GameObject pillars = new GameObject("Support Pillars");
        pillars.transform.parent = parent.transform;

        Vector3[] pillarPositions = {
            new Vector3(-4f, -1f, -3f),
            new Vector3(4f, -1f, -3f),
            new Vector3(-4f, -1f, 3f),
            new Vector3(4f, -1f, 3f)
        };

        foreach (Vector3 pos in pillarPositions)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Support Pillar";
            pillar.transform.parent = pillars.transform;
            pillar.transform.localPosition = pos;
            pillar.transform.localScale = new Vector3(0.5f, 3f, 0.5f);

            Material pillarMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pillarMat.color = Color.gray;
            pillar.GetComponent<Renderer>().material = pillarMat;
        }
    }

    void CreateSafetyRailings(GameObject parent)
    {
        GameObject railings = new GameObject("Safety Railings");
        railings.transform.parent = parent.transform;

        // Railing posts
        Vector3[] postPositions = {
            new Vector3(-6f, 1.5f, -4f), new Vector3(-6f, 1.5f, 0f), new Vector3(-6f, 1.5f, 4f),
            new Vector3(6f, 1.5f, -4f), new Vector3(6f, 1.5f, 0f), new Vector3(6f, 1.5f, 4f),
            new Vector3(-3f, 1.5f, -4f), new Vector3(0f, 1.5f, -4f), new Vector3(3f, 1.5f, -4f)
        };

        foreach (Vector3 pos in postPositions)
        {
            CreateRailingPost(railings, pos);
        }

        // Horizontal rails
        CreateHorizontalRail(railings, new Vector3(-6f, 1.8f, 0), new Vector3(0.2f, 0.1f, 8f));
        CreateHorizontalRail(railings, new Vector3(6f, 1.8f, 0), new Vector3(0.2f, 0.1f, 8f));
        CreateHorizontalRail(railings, new Vector3(0, 1.8f, -4f), new Vector3(12f, 0.1f, 0.2f));
    }

    void CreateRailingPost(GameObject parent, Vector3 position)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Railing Post";
        post.transform.parent = parent.transform;
        post.transform.localPosition = position;
        post.transform.localScale = new Vector3(0.1f, 1f, 0.1f);

        if (railingMaterial != null)
            post.GetComponent<Renderer>().material = railingMaterial;
        else
        {
            Material defaultRailing = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultRailing.color = Color.white;
            post.GetComponent<Renderer>().material = defaultRailing;
        }
    }

    void CreateHorizontalRail(GameObject parent, Vector3 position, Vector3 scale)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "Horizontal Rail";
        rail.transform.parent = parent.transform;
        rail.transform.localPosition = position;
        rail.transform.localScale = scale;

        if (railingMaterial != null)
            rail.GetComponent<Renderer>().material = railingMaterial;
    }

    void CreateEquipmentStorage(GameObject parent)
    {
        GameObject storage = new GameObject("Equipment Storage");
        storage.transform.parent = parent.transform;

        // Storage shed
        GameObject shed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shed.name = "Equipment Shed";
        shed.transform.parent = storage.transform;
        shed.transform.localPosition = new Vector3(3f, 1.5f, 2f);
        shed.transform.localScale = new Vector3(3f, 2f, 2f);

        if (equipmentMaterial != null)
            shed.GetComponent<Renderer>().material = equipmentMaterial;

        // Shed door
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Shed Door";
        door.transform.parent = storage.transform;
        door.transform.localPosition = new Vector3(1.6f, 1.5f, 2f);
        door.transform.localScale = new Vector3(0.1f, 1.8f, 1f);

        Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        doorMat.color = new Color(0.4f, 0.2f, 0.1f);
        door.GetComponent<Renderer>().material = doorMat;
    }

    void CreateLaunchRamp(GameObject parent)
    {
        GameObject ramp = new GameObject("Launch Ramp");
        ramp.transform.parent = parent.transform;

        // Ramp surface
        GameObject rampSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampSurface.name = "Ramp Surface";
        rampSurface.transform.parent = ramp.transform;
        rampSurface.transform.localPosition = new Vector3(0, 0.25f, 5f);
        rampSurface.transform.localScale = new Vector3(4f, 0.5f, 2f);
        rampSurface.transform.Rotate(15f, 0, 0); // Angled for launching

        // Ramp supports
        for (int i = 0; i < 3; i++)
        {
            GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cube);
            support.name = $"Ramp Support {i + 1}";
            support.transform.parent = ramp.transform;
            support.transform.localPosition = new Vector3(-1.5f + i * 1.5f, -0.2f, 5.5f);
            support.transform.localScale = new Vector3(0.2f, 1f, 0.5f);
        }
    }

    void CreateKiteSurfingSetup(GameObject parent)
    {
        GameObject kiteSetup = new GameObject("Kite Surfing Setup");
        kiteSetup.transform.parent = parent.transform;

        // Wind measurement pole
        GameObject windPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        windPole.name = "Wind Measurement Pole";
        windPole.transform.parent = kiteSetup.transform;
        windPole.transform.localPosition = new Vector3(-4f, 2f, -2f);
        windPole.transform.localScale = new Vector3(0.1f, 3f, 0.1f);

        // Wind vane
        GameObject windVane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windVane.name = "Wind Vane";
        windVane.transform.parent = kiteSetup.transform;
        windVane.transform.localPosition = new Vector3(-4f, 3.5f, -2f);
        windVane.transform.localScale = new Vector3(0.5f, 0.1f, 1f);

        // Kite storage hooks
        for (int i = 0; i < 4; i++)
        {
            GameObject hook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hook.name = $"Kite Hook {i + 1}";
            hook.transform.parent = kiteSetup.transform;
            hook.transform.localPosition = new Vector3(-3f, 2f + (i * 0.3f), -3f);
            hook.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            Material hookMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            hookMat.color = Color.red;
            hook.GetComponent<Renderer>().material = hookMat;
        }

        // Launch area marking
        GameObject launchArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        launchArea.name = "Kite Launch Area";
        launchArea.transform.parent = kiteSetup.transform;
        launchArea.transform.localPosition = new Vector3(-2f, 1.1f, 0);
        launchArea.transform.localScale = new Vector3(4f, 0.1f, 4f);

        Material launchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        launchMat.color = Color.yellow;
        launchArea.GetComponent<Renderer>().material = launchMat;
    }

    void CreateSurfboardRacks(GameObject parent)
    {
        GameObject racks = new GameObject("Surfboard Racks");
        racks.transform.parent = parent.transform;

        // Create 2 surfboard racks
        for (int rack = 0; rack < 2; rack++)
        {
            GameObject surfRack = new GameObject($"Surfboard Rack {rack + 1}");
            surfRack.transform.parent = racks.transform;
            surfRack.transform.localPosition = new Vector3(2f + rack * 2f, 1f, -2f);

            // Rack frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Rack Frame";
            frame.transform.parent = surfRack.transform;
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(0.2f, 2f, 1.5f);

            // Surfboard slots
            for (int slot = 0; slot < 6; slot++)
            {
                GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
                board.name = $"Surfboard {slot + 1}";
                board.transform.parent = surfRack.transform;
                board.transform.localPosition = new Vector3(0.3f, -0.5f + slot * 0.3f, 0);
                board.transform.localScale = new Vector3(3f, 0.1f, 0.3f);

                // Random surfboard colors
                Material boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                boardMat.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                board.GetComponent<Renderer>().material = boardMat;
            }
        }
    }
}