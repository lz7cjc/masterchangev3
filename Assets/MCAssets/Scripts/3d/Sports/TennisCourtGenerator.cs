using UnityEngine;

public class TennisCourtGenerator : MonoBehaviour
{
    [Header("Tennis Court Settings")]
    public Material courtSurfaceMaterial;
    public Material lineMaterial;
    public Material netMaterial;
    public Material postMaterial;

    [ContextMenu("Generate Tennis Court")]
    public void GenerateTennisCourt()
    {
        GameObject court = new GameObject("Tennis Court");
        court.transform.position = transform.position;

        // Court surface (23.77m x 10.97m)
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "Court Surface";
        surface.transform.parent = court.transform;
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localScale = new Vector3(23.77f, 0.1f, 10.97f);

        if (courtSurfaceMaterial != null)
            surface.GetComponent<Renderer>().material = courtSurfaceMaterial;
        else
        {
            Material defaultCourt = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultCourt.color = new Color(0.1f, 0.5f, 0.1f); // Green court
            surface.GetComponent<Renderer>().material = defaultCourt;
        }

        // Court lines
        CreateCourtLines(court);

        // Net and posts
        CreateTennisNet(court);

        // Seating areas
        CreateSeating(court, 4); // 4 bench sets

        // Lighting poles (for night play)
        CreateCourtLighting(court);

        Debug.Log("Tennis Court prefab generated! Save it as a prefab for reuse.");
    }

    void CreateCourtLines(GameObject parent)
    {
        GameObject linesParent = new GameObject("Court Lines");
        linesParent.transform.parent = parent.transform;

        // Baseline (back lines)
        CreateLine(linesParent, "Baseline 1", new Vector3(0, 0.11f, 5.485f), new Vector3(23.77f, 0.05f, 0.1f));
        CreateLine(linesParent, "Baseline 2", new Vector3(0, 0.11f, -5.485f), new Vector3(23.77f, 0.05f, 0.1f));

        // Sidelines
        CreateLine(linesParent, "Sideline 1", new Vector3(11.885f, 0.11f, 0), new Vector3(0.1f, 0.05f, 10.97f));
        CreateLine(linesParent, "Sideline 2", new Vector3(-11.885f, 0.11f, 0), new Vector3(0.1f, 0.05f, 10.97f));

        // Service lines
        CreateLine(linesParent, "Service Line 1", new Vector3(0, 0.11f, 2.135f), new Vector3(18.29f, 0.05f, 0.1f));
        CreateLine(linesParent, "Service Line 2", new Vector3(0, 0.11f, -2.135f), new Vector3(18.29f, 0.05f, 0.1f));

        // Center service line
        CreateLine(linesParent, "Center Service Line", new Vector3(0, 0.11f, 0), new Vector3(0.1f, 0.05f, 4.27f));

        // Center mark
        CreateLine(linesParent, "Center Mark 1", new Vector3(0, 0.11f, 5.485f), new Vector3(0.1f, 0.05f, 0.3f));
        CreateLine(linesParent, "Center Mark 2", new Vector3(0, 0.11f, -5.485f), new Vector3(0.1f, 0.05f, 0.3f));
    }

    void CreateLine(GameObject parent, string name, Vector3 position, Vector3 scale)
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

    void CreateTennisNet(GameObject parent)
    {
        GameObject netSystem = new GameObject("Net System");
        netSystem.transform.parent = parent.transform;

        // Net posts
        CreateNetPost(netSystem, new Vector3(12.8f, 1.07f, 0));
        CreateNetPost(netSystem, new Vector3(-12.8f, 1.07f, 0));

        // Net
        GameObject net = GameObject.CreatePrimitive(PrimitiveType.Cube);
        net.name = "Tennis Net";
        net.transform.parent = netSystem.transform;
        net.transform.localPosition = new Vector3(0, 0.95f, 0);
        net.transform.localScale = new Vector3(25.6f, 0.05f, 1.07f);

        Material netMat = netMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));
        netMat.color = Color.white;
        net.GetComponent<Renderer>().material = netMat;
    }

    void CreateNetPost(GameObject parent, Vector3 position)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Net Post";
        post.transform.parent = parent.transform;
        post.transform.localPosition = position;
        post.transform.localScale = new Vector3(0.2f, 2.14f, 0.2f);

        Material postMat = postMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));
        postMat.color = Color.gray;
        post.GetComponent<Renderer>().material = postMat;
    }

    void CreateSeating(GameObject parent, int benchCount)
    {
        GameObject seatingParent = new GameObject("Seating Area");
        seatingParent.transform.parent = parent.transform;

        for (int i = 0; i < benchCount; i++)
        {
            GameObject bench = new GameObject($"Bench {i + 1}");
            bench.transform.parent = seatingParent.transform;

            // Bench seat
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Bench Seat";
            seat.transform.parent = bench.transform;
            seat.transform.localPosition = new Vector3(0, 0.5f, 0);
            seat.transform.localScale = new Vector3(3f, 0.1f, 0.4f);

            // Bench back
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Bench Back";
            back.transform.parent = bench.transform;
            back.transform.localPosition = new Vector3(0, 1f, -0.15f);
            back.transform.localScale = new Vector3(3f, 1f, 0.1f);

            // Position benches around court
            float angle = (360f / benchCount) * i;
            float radius = 15f;
            bench.transform.localPosition = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );
            bench.transform.LookAt(parent.transform.position);
        }
    }

    void CreateCourtLighting(GameObject parent)
    {
        GameObject lightingParent = new GameObject("Court Lighting");
        lightingParent.transform.parent = parent.transform;

        Vector3[] lightPositions = {
            new Vector3(15, 8, 8),
            new Vector3(-15, 8, 8),
            new Vector3(15, 8, -8),
            new Vector3(-15, 8, -8)
        };

        foreach (Vector3 pos in lightPositions)
        {
            GameObject lightPole = CreateLightPole(lightingParent, pos);
        }
    }

    GameObject CreateLightPole(GameObject parent, Vector3 position)
    {
        GameObject pole = new GameObject("Light Pole");
        pole.transform.parent = parent.transform;
        pole.transform.localPosition = position;

        // Pole
        GameObject poleBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poleBody.name = "Pole Body";
        poleBody.transform.parent = pole.transform;
        poleBody.transform.localPosition = new Vector3(0, -4, 0);
        poleBody.transform.localScale = new Vector3(0.3f, 8f, 0.3f);

        // Light fixture
        GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = "Light Fixture";
        fixture.transform.parent = pole.transform;
        fixture.transform.localPosition = new Vector3(0, 1, 0);
        fixture.transform.localScale = new Vector3(1f, 0.3f, 0.5f);

        // Light component
        GameObject lightObj = new GameObject("Light");
        lightObj.transform.parent = pole.transform;
        lightObj.transform.localPosition = Vector3.zero;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.intensity = 2f;
        light.range = 20f;
        light.spotAngle = 45f;
        light.transform.LookAt(parent.transform.position);

        return pole;
    }
}
