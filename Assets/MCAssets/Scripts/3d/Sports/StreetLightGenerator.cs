using UnityEngine;

public class StreetLightGenerator : MonoBehaviour
{
    [Header("Light Settings")]
    public Material poleMaterial;
    public Material fixtureMaterial;
    public Color lightColor = Color.white;
    public float lightIntensity = 2f;
    public float lightRange = 15f;

    [ContextMenu("Generate Street Light")]
    public void GenerateStreetLight()
    {
        GameObject streetLight = new GameObject("Street Light");
        streetLight.transform.position = transform.position;

        // Light pole
        CreateLightPole(streetLight);

        // Light fixture
        CreateLightFixture(streetLight);

        // Light component
        CreateLightComponent(streetLight);

        // Base/foundation
        CreateLightBase(streetLight);

        Debug.Log("Street Light prefab generated!");
    }

    void CreateLightPole(GameObject parent)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Light Pole";
        pole.transform.parent = parent.transform;
        pole.transform.localPosition = new Vector3(0, 4f, 0);
        pole.transform.localScale = new Vector3(0.3f, 8f, 0.3f);

        if (poleMaterial != null)
            pole.GetComponent<Renderer>().material = poleMaterial;
        else
        {
            Material defaultPole = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultPole.color = Color.gray;
            pole.GetComponent<Renderer>().material = defaultPole;
        }

        // Pole taper (smaller at top)
        CreatePoleTaper(parent);
    }

    void CreatePoleTaper(GameObject parent)
    {
        GameObject taper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        taper.name = "Pole Taper";
        taper.transform.parent = parent.transform;
        taper.transform.localPosition = new Vector3(0, 7f, 0);
        taper.transform.localScale = new Vector3(0.2f, 2f, 0.2f);

        if (poleMaterial != null)
            taper.GetComponent<Renderer>().material = poleMaterial;
    }

    void CreateLightFixture(GameObject parent)
    {
        GameObject fixture = new GameObject("Light Fixture");
        fixture.transform.parent = parent.transform;
        fixture.transform.localPosition = new Vector3(0, 8f, 0);

        // Fixture head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Fixture Head";
        head.transform.parent = fixture.transform;
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        if (fixtureMaterial != null)
            head.GetComponent<Renderer>().material = fixtureMaterial;
        else
        {
            Material defaultFixture = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultFixture.color = Color.black;
            head.GetComponent<Renderer>().material = defaultFixture;
        }

        // Fixture arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Fixture Arm";
        arm.transform.parent = fixture.transform;
        arm.transform.localPosition = new Vector3(0.5f, 0, 0);
        arm.transform.localScale = new Vector3(1f, 0.2f, 0.2f);

        if (fixtureMaterial != null)
            arm.GetComponent<Renderer>().material = fixtureMaterial;

        // Glass cover
        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glass.name = "Light Glass";
        glass.transform.parent = fixture.transform;
        glass.transform.localPosition = new Vector3(0, -0.3f, 0);
        glass.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);

        Material glassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glassMat.color = new Color(1f, 1f, 1f, 0.3f);
        glass.GetComponent<Renderer>().material = glassMat;
    }

    void CreateLightComponent(GameObject parent)
    {
        GameObject lightObj = new GameObject("Light Source");
        lightObj.transform.parent = parent.transform;
        lightObj.transform.localPosition = new Vector3(0, 8f, 0);

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.range = lightRange;
        light.shadows = LightShadows.Soft;

        // Add light halo effect
        CreateLightHalo(lightObj);
    }

    void CreateLightHalo(GameObject parent)
    {
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "Light Halo";
        halo.transform.parent = parent.transform;
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = new Vector3(2f, 2f, 2f);

        Material haloMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        haloMat.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.1f);
        haloMat.EnableKeyword("_EMISSION");
        haloMat.SetColor("_EmissionColor", lightColor * 0.3f);
        halo.GetComponent<Renderer>().material = haloMat;
    }

    void CreateLightBase(GameObject parent)
    {
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "Light Base";
        baseObj.transform.parent = parent.transform;
        baseObj.transform.localPosition = new Vector3(0, 0.2f, 0);
        baseObj.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);

        Material baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        baseMat.color = new Color(0.5f, 0.5f, 0.5f);
        baseObj.GetComponent<Renderer>().material = baseMat;

        // Access panel
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Access Panel";
        panel.transform.parent = parent.transform;
        panel.transform.localPosition = new Vector3(0.3f, 1f, 0);
        panel.transform.localScale = new Vector3(0.1f, 0.5f, 0.3f);

        Material panelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        panelMat.color = Color.gray;
        panel.GetComponent<Renderer>().material = panelMat;
    }
}