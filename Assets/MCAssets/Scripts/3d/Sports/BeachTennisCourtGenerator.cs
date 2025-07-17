using UnityEngine;

public class BeachTennisCourtGenerator : MonoBehaviour
{
    [Header("Beach Tennis Settings")]
    public Material sandMaterial;
    public Material lineMaterial;
    public Material netMaterial;

    [ContextMenu("Generate Beach Tennis Court")]
    public void GenerateBeachTennisCourt()
    {
        GameObject court = new GameObject("Beach Tennis Court");
        court.transform.position = transform.position;

        // Court surface (16m x 8m - smaller than regular tennis)
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "Beach Court Surface";
        surface.transform.parent = court.transform;
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localScale = new Vector3(16f, 0.1f, 8f);

        if (sandMaterial != null)
            surface.GetComponent<Renderer>().material = sandMaterial;
        else
        {
            Material defaultSand = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultSand.color = new Color(0.8f, 0.7f, 0.5f); // Sandy color
            surface.GetComponent<Renderer>().material = defaultSand;
        }

        // Beach tennis lines
        CreateBeachTennisLines(court);

        // Lower net (1.7m high)
        CreateBeachTennisNet(court);

        // Beach accessories
        CreateBeachAccessories(court);

        Debug.Log("Beach Tennis Court prefab generated!");
    }

    void CreateBeachTennisLines(GameObject parent)
    {
        GameObject linesParent = new GameObject("Beach Court Lines");
        linesParent.transform.parent = parent.transform;

        // Court perimeter
        CreateBeachLine(linesParent, "End Line 1", new Vector3(0, 0.11f, 4f), new Vector3(16f, 0.05f, 0.1f));
        CreateBeachLine(linesParent, "End Line 2", new Vector3(0, 0.11f, -4f), new Vector3(16f, 0.05f, 0.1f));
        CreateBeachLine(linesParent, "Side Line 1", new Vector3(8f, 0.11f, 0), new Vector3(0.1f, 0.05f, 8f));
        CreateBeachLine(linesParent, "Side Line 2", new Vector3(-8f, 0.11f, 0), new Vector3(0.1f, 0.05f, 8f));

        // Service lines (no service area in beach tennis, but marking playing zones)
        CreateBeachLine(linesParent, "Center Line", new Vector3(0, 0.11f, 0), new Vector3(0.1f, 0.05f, 8f));
    }

    void CreateBeachLine(GameObject parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.parent = parent.transform;
        line.transform.localPosition = position;
        line.transform.localScale = scale;

        Material lineMat = lineMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lineMat.color = Color.white;
        line.GetComponent<Renderer>().material = lineMat;
    }

    void CreateBeachTennisNet(GameObject parent)
    {
        GameObject netSystem = new GameObject("Beach Net System");
        netSystem.transform.parent = parent.transform;

        // Net posts (1.7m high)
        CreateBeachNetPost(netSystem, new Vector3(8.5f, 0.85f, 0));
        CreateBeachNetPost(netSystem, new Vector3(-8.5f, 0.85f, 0));

        // Net (lower than tennis)
        GameObject net = GameObject.CreatePrimitive(PrimitiveType.Cube);
        net.name = "Beach Tennis Net";
        net.transform.parent = netSystem.transform;
        net.transform.localPosition = new Vector3(0, 0.85f, 0);
        net.transform.localScale = new Vector3(17f, 0.05f, 1.7f);

        Material netMat = netMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));
        netMat.color = Color.white;
        net.GetComponent<Renderer>().material = netMat;
    }

    void CreateBeachNetPost(GameObject parent, Vector3 position)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Beach Net Post";
        post.transform.parent = parent.transform;
        post.transform.localPosition = position;
        post.transform.localScale = new Vector3(0.15f, 1.7f, 0.15f);

        Material postMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        postMat.color = Color.white;
        post.GetComponent<Renderer>().material = postMat;
    }

    void CreateBeachAccessories(GameObject parent)
    {
        GameObject accessories = new GameObject("Beach Accessories");
        accessories.transform.parent = parent.transform;

        // Beach umbrellas
        CreateBeachUmbrella(accessories, new Vector3(12, 0, 6));
        CreateBeachUmbrella(accessories, new Vector3(-12, 0, 6));

        // Water station
        GameObject waterStation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waterStation.name = "Water Station";
        waterStation.transform.parent = accessories.transform;
        waterStation.transform.localPosition = new Vector3(10, 1, -6);
        waterStation.transform.localScale = new Vector3(1f, 2f, 1f);
    }

    void CreateBeachUmbrella(GameObject parent, Vector3 position)
    {
        GameObject umbrella = new GameObject("Beach Umbrella");
        umbrella.transform.parent = parent.transform;
        umbrella.transform.localPosition = position;

        // Umbrella pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Umbrella Pole";
        pole.transform.parent = umbrella.transform;
        pole.transform.localPosition = new Vector3(0, 2f, 0);
        pole.transform.localScale = new Vector3(0.1f, 4f, 0.1f);

        // Umbrella top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = "Umbrella Top";
        top.transform.parent = umbrella.transform;
        top.transform.localPosition = new Vector3(0, 4f, 0);
        top.transform.localScale = new Vector3(4f, 1f, 4f);

        Material umbrellaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        umbrellaMat.color = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
        top.GetComponent<Renderer>().material = umbrellaMat;
    }
}