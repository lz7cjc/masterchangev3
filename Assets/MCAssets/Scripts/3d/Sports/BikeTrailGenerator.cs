using UnityEngine;

public class BikeTrailGenerator : MonoBehaviour
{
    [Header("Trail Settings")]
    public Material trailMaterial;
    public Material markerMaterial;
    public int trailSegments = 10;
    public float trailWidth = 2f;

    [ContextMenu("Generate Bike Trail")]
    public void GenerateBikeTrail()
    {
        GameObject bikeTrail = new GameObject("Mountain Bike Trail");
        bikeTrail.transform.position = transform.position;

        // Trail path
        CreateTrailPath(bikeTrail);

        // Trail markers
        CreateTrailMarkers(bikeTrail);

        // Obstacles/features
        CreateTrailFeatures(bikeTrail);

        // Rest stops
        CreateRestStops(bikeTrail);

        // Trail signage
        CreateTrailSignage(bikeTrail);

        Debug.Log("Bike Trail prefab generated!");
    }

    void CreateTrailPath(GameObject parent)
    {
        GameObject trail = new GameObject("Trail Path");
        trail.transform.parent = parent.transform;

        // Create winding trail segments
        for (int i = 0; i < trailSegments; i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"Trail Segment {i + 1}";
            segment.transform.parent = trail.transform;

            // Create S-curve pattern
            float x = i * 5f;
            float z = Mathf.Sin(i * 0.5f) * 10f;
            float y = i * 0.2f; // Slight elevation changes

            segment.transform.localPosition = new Vector3(x, y, z);
            segment.transform.localScale = new Vector3(5f, 0.1f, trailWidth);

            // Rotate segment to follow curve
            if (i > 0)
            {
                Vector3 prevPos = new Vector3((i - 1) * 5f, (i - 1) * 0.2f, Mathf.Sin((i - 1) * 0.5f) * 10f);
                Vector3 currentPos = new Vector3(x, y, z);
                segment.transform.LookAt(currentPos + (currentPos - prevPos));
            }

            if (trailMaterial != null)
                segment.GetComponent<Renderer>().material = trailMaterial;
            else
            {
                Material defaultTrail = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                defaultTrail.color = new Color(0.4f, 0.3f, 0.2f); // Dirt color
                segment.GetComponent<Renderer>().material = defaultTrail;
            }
        }
    }

    void CreateTrailMarkers(GameObject parent)
    {
        GameObject markers = new GameObject("Trail Markers");
        markers.transform.parent = parent.transform;

        for (int i = 0; i < trailSegments; i += 2) // Every other segment
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Trail Marker {i / 2 + 1}";
            marker.transform.parent = markers.transform;

            float x = i * 5f;
            float z = Mathf.Sin(i * 0.5f) * 10f + 3f; // Offset from trail
            float y = i * 0.2f + 1f;

            marker.transform.localPosition = new Vector3(x, y, z);
            marker.transform.localScale = new Vector3(0.3f, 2f, 0.3f);

            if (markerMaterial != null)
                marker.GetComponent<Renderer>().material = markerMaterial;
            else
            {
                Material defaultMarker = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                defaultMarker.color = new Color(1f, 0.5f, 0f); // Orange color
                marker.GetComponent<Renderer>().material = defaultMarker;
            }

            // Add distance marker
            CreateDistanceMarker(marker, (i / 2 + 1) * 100); // 100m intervals
        }
    }

    void CreateDistanceMarker(GameObject parent, int distance)
    {
        GameObject distanceSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        distanceSign.name = $"Distance: {distance}m";
        distanceSign.transform.parent = parent.transform;
        distanceSign.transform.localPosition = new Vector3(0, 1f, 0);
        distanceSign.transform.localScale = new Vector3(0.8f, 0.3f, 0.1f);

        Material signMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signMat.color = Color.white;
        distanceSign.GetComponent<Renderer>().material = signMat;
    }

    void CreateTrailFeatures(GameObject parent)
    {
        GameObject features = new GameObject("Trail Features");
        features.transform.parent = parent.transform;

        // Add some trail obstacles/features
        CreateJump(features, new Vector3(15f, 0.5f, 5f));
        CreateJump(features, new Vector3(35f, 2f, -8f));

        CreateRockGarden(features, new Vector3(25f, 1.5f, 3f));

        CreateBridge(features, new Vector3(40f, 3f, -2f));
    }

    void CreateJump(GameObject parent, Vector3 position)
    {
        GameObject jump = new GameObject("Bike Jump");
        jump.transform.parent = parent.transform;
        jump.transform.localPosition = position;

        // Jump ramp
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Jump Ramp";
        ramp.transform.parent = jump.transform;
        ramp.transform.localPosition = Vector3.zero;
        ramp.transform.localScale = new Vector3(3f, 0.5f, 2f);
        ramp.transform.Rotate(15f, 0, 0);

        Material rampMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rampMat.color = new Color(0.6f, 0.4f, 0.2f);
        ramp.GetComponent<Renderer>().material = rampMat;
    }

    void CreateRockGarden(GameObject parent, Vector3 position)
    {
        GameObject rockGarden = new GameObject("Rock Garden");
        rockGarden.transform.parent = parent.transform;
        rockGarden.transform.localPosition = position;

        // Create several rocks
        for (int i = 0; i < 8; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock {i + 1}";
            rock.transform.parent = rockGarden.transform;
            rock.transform.localPosition = new Vector3(
                Random.Range(-2f, 2f),
                0,
                Random.Range(-1f, 1f)
            );
            rock.transform.localScale = Vector3.one * Random.Range(0.3f, 0.8f);

            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rockMat.color = Color.gray;
            rock.GetComponent<Renderer>().material = rockMat;
        }
    }

    void CreateBridge(GameObject parent, Vector3 position)
    {
        GameObject bridge = new GameObject("Trail Bridge");
        bridge.transform.parent = parent.transform;
        bridge.transform.localPosition = position;

        // Bridge deck
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Bridge Deck";
        deck.transform.parent = bridge.transform;
        deck.transform.localPosition = Vector3.zero;
        deck.transform.localScale = new Vector3(6f, 0.2f, 3f);

        Material bridgeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bridgeMat.color = new Color(0.5f, 0.3f, 0.1f);
        deck.GetComponent<Renderer>().material = bridgeMat;

        // Bridge supports
        CreateBridgeSupport(bridge, new Vector3(-2.5f, -1f, 1f));
        CreateBridgeSupport(bridge, new Vector3(2.5f, -1f, 1f));
        CreateBridgeSupport(bridge, new Vector3(-2.5f, -1f, -1f));
        CreateBridgeSupport(bridge, new Vector3(2.5f, -1f, -1f));
    }

    void CreateBridgeSupport(GameObject parent, Vector3 position)
    {
        GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        support.name = "Bridge Support";
        support.transform.parent = parent.transform;
        support.transform.localPosition = position;
        support.transform.localScale = new Vector3(0.2f, 2f, 0.2f);

        Material supportMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        supportMat.color = new Color(0.4f, 0.2f, 0.1f);
        support.GetComponent<Renderer>().material = supportMat;
    }

    void CreateRestStops(GameObject parent)
    {
        GameObject restStops = new GameObject("Rest Stops");
        restStops.transform.parent = parent.transform;

        // Create 2 rest stops along the trail
        CreateRestStop(restStops, new Vector3(20f, 1f, 8f));
        CreateRestStop(restStops, new Vector3(45f, 3.5f, -5f));
    }

    void CreateRestStop(GameObject parent, Vector3 position)
    {
        GameObject restStop = new GameObject("Rest Stop");
        restStop.transform.parent = parent.transform;
        restStop.transform.localPosition = position;

        // Bench
        GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.name = "Rest Bench";
        bench.transform.parent = restStop.transform;
        bench.transform.localPosition = Vector3.zero;
        bench.transform.localScale = new Vector3(2f, 0.5f, 0.6f);

        // Water fountain
        GameObject fountain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fountain.name = "Water Fountain";
        fountain.transform.parent = restStop.transform;
        fountain.transform.localPosition = new Vector3(3f, 1f, 0);
        fountain.transform.localScale = new Vector3(0.5f, 2f, 0.5f);

        Material fountainMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        fountainMat.color = Color.blue;
        fountain.GetComponent<Renderer>().material = fountainMat;
    }

    void CreateTrailSignage(GameObject parent)
    {
        GameObject signage = new GameObject("Trail Signage");
        signage.transform.parent = parent.transform;

        // Trail entrance sign
        CreateTrailSign(signage, "MOUNTAIN BIKE TRAIL", "DIFFICULTY: INTERMEDIATE", Vector3.zero);

        // Trail exit sign
        Vector3 exitPos = new Vector3(trailSegments * 5f, trailSegments * 0.2f,
                                     Mathf.Sin(trailSegments * 0.5f) * 10f);
        CreateTrailSign(signage, "TRAIL END", "THANK YOU FOR VISITING", exitPos);
    }

    void CreateTrailSign(GameObject parent, string title, string subtitle, Vector3 position)
    {
        GameObject sign = new GameObject($"Trail Sign - {title}");
        sign.transform.parent = parent.transform;
        sign.transform.localPosition = position;

        // Sign post
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Sign Post";
        post.transform.parent = sign.transform;
        post.transform.localPosition = new Vector3(0, 1f, 0);
        post.transform.localScale = new Vector3(0.1f, 2f, 0.1f);

        // Sign board
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Sign Board";
        board.transform.parent = sign.transform;
        board.transform.localPosition = new Vector3(0, 2f, 0);
        board.transform.localScale = new Vector3(3f, 1f, 0.1f);

        Material signMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signMat.color = Color.white;
        board.GetComponent<Renderer>().material = signMat;
    }
}