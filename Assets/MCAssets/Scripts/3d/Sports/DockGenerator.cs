using UnityEngine;

public class DockGenerator : MonoBehaviour
{
    [Header("Dock Settings")]
    public Material woodMaterial;
    public Material metalMaterial;

    [ContextMenu("Generate Dock")]
    public void GenerateDock()
    {
        GameObject dock = new GameObject("Marina Dock");
        dock.transform.position = transform.position;

        // Main dock platform
        CreateDockPlatform(dock);

        // Dock posts/pilings
        CreateDockPilings(dock);

        // Boat slips
        CreateBoatSlips(dock);

        // Dock accessories
        CreateDockAccessories(dock);

        Debug.Log("Dock prefab generated!");
    }

    void CreateDockPlatform(GameObject parent)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Dock Platform";
        platform.transform.parent = parent.transform;
        platform.transform.localPosition = new Vector3(0, 0.25f, 0);
        platform.transform.localScale = new Vector3(30f, 0.5f, 4f);

        if (woodMaterial != null)
            platform.GetComponent<Renderer>().material = woodMaterial;
        else
        {
            Material defaultWood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultWood.color = new Color(0.6f, 0.4f, 0.2f);
            platform.GetComponent<Renderer>().material = defaultWood;
        }

        // Dock planks detail
        CreateDockPlanks(parent);
    }

    void CreateDockPlanks(GameObject parent)
    {
        GameObject planks = new GameObject("Dock Planks");
        planks.transform.parent = parent.transform;

        for (int i = 0; i < 15; i++)
        {
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = $"Dock Plank {i + 1}";
            plank.transform.parent = planks.transform;
            plank.transform.localPosition = new Vector3(-14f + (i * 2f), 0.3f, 0);
            plank.transform.localScale = new Vector3(1.8f, 0.1f, 4f);

            if (woodMaterial != null)
                plank.GetComponent<Renderer>().material = woodMaterial;
        }
    }

    void CreateDockPilings(GameObject parent)
    {
        GameObject pilings = new GameObject("Dock Pilings");
        pilings.transform.parent = parent.transform;

        Vector3[] pilingPositions = {
            new Vector3(-12f, -1f, 1.5f),
            new Vector3(-12f, -1f, -1.5f),
            new Vector3(0f, -1f, 1.5f),
            new Vector3(0f, -1f, -1.5f),
            new Vector3(12f, -1f, 1.5f),
            new Vector3(12f, -1f, -1.5f)
        };

        foreach (Vector3 pos in pilingPositions)
        {
            GameObject piling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            piling.name = "Dock Piling";
            piling.transform.parent = pilings.transform;
            piling.transform.localPosition = pos;
            piling.transform.localScale = new Vector3(0.3f, 3f, 0.3f);

            if (woodMaterial != null)
                piling.GetComponent<Renderer>().material = woodMaterial;
        }
    }

    void CreateBoatSlips(GameObject parent)
    {
        GameObject slips = new GameObject("Boat Slips");
        slips.transform.parent = parent.transform;

        // Create 4 boat slips
        for (int i = 0; i < 4; i++)
        {
            GameObject slip = new GameObject($"Boat Slip {i + 1}");
            slip.transform.parent = slips.transform;

            // Slip markers
            GameObject marker1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker1.name = "Slip Marker 1";
            marker1.transform.parent = slip.transform;
            marker1.transform.localPosition = new Vector3(-12f + (i * 6f), 0.5f, 3f);
            marker1.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

            GameObject marker2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker2.name = "Slip Marker 2";
            marker2.transform.parent = slip.transform;
            marker2.transform.localPosition = new Vector3(-9f + (i * 6f), 0.5f, 3f);
            marker2.transform.localScale = new Vector3(0.2f, 1f, 0.2f);

            // Mooring cleats
            CreateMooringCleat(slip, new Vector3(-12f + (i * 6f), 0.6f, 1.8f));
            CreateMooringCleat(slip, new Vector3(-9f + (i * 6f), 0.6f, 1.8f));
        }
    }

    void CreateMooringCleat(GameObject parent, Vector3 position)
    {
        GameObject cleat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cleat.name = "Mooring Cleat";
        cleat.transform.parent = parent.transform;
        cleat.transform.localPosition = position;
        cleat.transform.localScale = new Vector3(0.3f, 0.1f, 0.6f);

        if (metalMaterial != null)
            cleat.GetComponent<Renderer>().material = metalMaterial;
        else
        {
            Material defaultMetal = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultMetal.color = Color.gray;
            cleat.GetComponent<Renderer>().material = defaultMetal;
        }
    }

    void CreateDockAccessories(GameObject parent)
    {
        GameObject accessories = new GameObject("Dock Accessories");
        accessories.transform.parent = parent.transform;

        // Fuel pump
        CreateFuelPump(accessories, new Vector3(14f, 1f, 0));

        // Electrical pedestals
        CreateElectricalPedestal(accessories, new Vector3(-6f, 0.8f, 1.8f));
        CreateElectricalPedestal(accessories, new Vector3(6f, 0.8f, 1.8f));

        // Dock box
        CreateDockBox(accessories, new Vector3(0, 0.8f, 1.8f));

        // Safety equipment
        CreateLifeRing(accessories, new Vector3(-14f, 1.5f, 1.8f));
        CreateLifeRing(accessories, new Vector3(14f, 1.5f, 1.8f));
    }

    void CreateFuelPump(GameObject parent, Vector3 position)
    {
        GameObject pump = new GameObject("Fuel Pump");
        pump.transform.parent = parent.transform;
        pump.transform.localPosition = position;

        // Pump base
        GameObject pumpBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pumpBase.name = "Pump Base";
        pumpBase.transform.parent = pump.transform;
        pumpBase.transform.localPosition = Vector3.zero;
        pumpBase.transform.localScale = new Vector3(1f, 2f, 0.8f);

        // Pump nozzle
        GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzle.name = "Pump Nozzle";
        nozzle.transform.parent = pump.transform;
        nozzle.transform.localPosition = new Vector3(0.6f, 0.5f, 0);
        nozzle.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        nozzle.transform.Rotate(0, 0, 90);
    }

    void CreateElectricalPedestal(GameObject parent, Vector3 position)
    {
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = "Electrical Pedestal";
        pedestal.transform.parent = parent.transform;
        pedestal.transform.localPosition = position;
        pedestal.transform.localScale = new Vector3(0.4f, 1.6f, 0.3f);

        Material electricMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        electricMat.color = Color.yellow;
        pedestal.GetComponent<Renderer>().material = electricMat;
    }

    void CreateDockBox(GameObject parent, Vector3 position)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Dock Box";
        box.transform.parent = parent.transform;
        box.transform.localPosition = position;
        box.transform.localScale = new Vector3(1f, 0.6f, 0.6f);

        Material boxMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        boxMat.color = Color.white;
        box.GetComponent<Renderer>().material = boxMat;
    }

    void CreateLifeRing(GameObject parent, Vector3 position)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ring.name = "Life Ring";
        ring.transform.parent = parent.transform;
        ring.transform.localPosition = position;
        ring.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);

        Material ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ringMat.color = Color.red;
        ring.GetComponent<Renderer>().material = ringMat;
    }
}