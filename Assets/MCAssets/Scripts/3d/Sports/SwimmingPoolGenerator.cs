using UnityEngine;

public class SwimmingPoolGenerator : MonoBehaviour
{
    [Header("Pool Settings")]
    public Material waterMaterial;
    public Material poolDeckMaterial;
    public Material laneMaterial;

    [ContextMenu("Generate Swimming Pool")]
    public void GenerateSwimmingPool()
    {
        GameObject pool = new GameObject("Swimming Pool Complex");
        pool.transform.position = transform.position;

        // Main pool (50m x 25m Olympic size)
        CreateOlympicPool(pool);

        // Pool deck
        CreatePoolDeck(pool);

        // Lane markers
        CreateLaneMarkers(pool);

        // Starting blocks
        CreateStartingBlocks(pool);

        // Pool facilities
        CreatePoolFacilities(pool);

        Debug.Log("Swimming Pool prefab generated!");
    }

    void CreateOlympicPool(GameObject parent)
    {
        GameObject poolWater = GameObject.CreatePrimitive(PrimitiveType.Cube);
        poolWater.name = "Pool Water";
        poolWater.transform.parent = parent.transform;
        poolWater.transform.localPosition = new Vector3(0, -1f, 0);
        poolWater.transform.localScale = new Vector3(50f, 2f, 25f);

        if (waterMaterial != null)
            poolWater.GetComponent<Renderer>().material = waterMaterial;
        else
        {
            Material defaultWater = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultWater.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            poolWater.GetComponent<Renderer>().material = defaultWater;
        }

        // Pool walls
        CreatePoolWalls(parent);
    }

    void CreatePoolWalls(GameObject parent)
    {
        GameObject walls = new GameObject("Pool Walls");
        walls.transform.parent = parent.transform;

        // Pool edge (lip)
        GameObject[] edges = {
            CreatePoolEdge(walls, "North Edge", new Vector3(0, 0.1f, 12.6f), new Vector3(50.2f, 0.2f, 0.2f)),
            CreatePoolEdge(walls, "South Edge", new Vector3(0, 0.1f, -12.6f), new Vector3(50.2f, 0.2f, 0.2f)),
            CreatePoolEdge(walls, "East Edge", new Vector3(25.1f, 0.1f, 0), new Vector3(0.2f, 0.2f, 25f)),
            CreatePoolEdge(walls, "West Edge", new Vector3(-25.1f, 0.1f, 0), new Vector3(0.2f, 0.2f, 25f))
        };
    }

    GameObject CreatePoolEdge(GameObject parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = name;
        edge.transform.parent = parent.transform;
        edge.transform.localPosition = position;
        edge.transform.localScale = scale;

        Material concreteMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        concreteMat.color = Color.gray;
        edge.GetComponent<Renderer>().material = concreteMat;

        return edge;
    }

    void CreatePoolDeck(GameObject parent)
    {
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Pool Deck";
        deck.transform.parent = parent.transform;
        deck.transform.localPosition = new Vector3(0, -0.1f, 0);
        deck.transform.localScale = new Vector3(70f, 0.2f, 45f);

        if (poolDeckMaterial != null)
            deck.GetComponent<Renderer>().material = poolDeckMaterial;
        else
        {
            Material defaultDeck = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            defaultDeck.color = new Color(0.8f, 0.8f, 0.8f);
            deck.GetComponent<Renderer>().material = defaultDeck;
        }
    }

    void CreateLaneMarkers(GameObject parent)
    {
        GameObject lanes = new GameObject("Lane Markers");
        lanes.transform.parent = parent.transform;

        // 8 lanes for Olympic pool
        for (int i = 0; i < 9; i++) // 9 lines for 8 lanes
        {
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = $"Lane Line {i}";
            lane.transform.parent = lanes.transform;
            lane.transform.localPosition = new Vector3(0, -0.8f, -12f + (i * 3f));
            lane.transform.localScale = new Vector3(50f, 0.1f, 0.1f);

            Material laneMat = laneMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));
            laneMat.color = (i == 0 || i == 8) ? Color.green : Color.blue;
            lane.GetComponent<Renderer>().material = laneMat;
        }
    }

    void CreateStartingBlocks(GameObject parent)
    {
        GameObject blocks = new GameObject("Starting Blocks");
        blocks.transform.parent = parent.transform;

        for (int i = 0; i < 8; i++)
        {
            GameObject block = new GameObject($"Starting Block {i + 1}");
            block.transform.parent = blocks.transform;

            // Block platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Block Platform";
            platform.transform.parent = block.transform;
            platform.transform.localPosition = new Vector3(-26f, 0.5f, -10.5f + (i * 3f));
            platform.transform.localScale = new Vector3(1f, 1f, 2f);

            // Starting post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Starting Post";
            post.transform.parent = block.transform;
            post.transform.localPosition = new Vector3(-26f, 0f, -10.5f + (i * 3f));
            post.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
        }
    }

    void CreatePoolFacilities(GameObject parent)
    {
        GameObject facilities = new GameObject("Pool Facilities");
        facilities.transform.parent = parent.transform;

        // Lifeguard chair
        CreateLifeguardChair(facilities, new Vector3(30, 2, 0));

        // Pool equipment room
        GameObject equipmentRoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        equipmentRoom.name = "Equipment Room";
        equipmentRoom.transform.parent = facilities.transform;
        equipmentRoom.transform.localPosition = new Vector3(35, 2, -15);
        equipmentRoom.transform.localScale = new Vector3(8f, 4f, 6f);

        // Spectator seating
        CreateSpectatorSeating(facilities);
    }

    void CreateLifeguardChair(GameObject parent, Vector3 position)
    {
        GameObject chair = new GameObject("Lifeguard Chair");
        chair.transform.parent = parent.transform;
        chair.transform.localPosition = position;

        // Chair legs
        for (int i = 0; i < 4; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"Chair Leg {i + 1}";
            leg.transform.parent = chair.transform;
            leg.transform.localScale = new Vector3(0.1f, 2f, 0.1f);

            float x = (i % 2 == 0) ? -0.5f : 0.5f;
            float z = (i < 2) ? -0.5f : 0.5f;
            leg.transform.localPosition = new Vector3(x, 0, z);
        }

        // Chair seat
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "Chair Seat";
        seat.transform.parent = chair.transform;
        seat.transform.localPosition = new Vector3(0, 2f, 0);
        seat.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
    }

    void CreateSpectatorSeating(GameObject parent)
    {
        GameObject seating = new GameObject("Spectator Seating");
        seating.transform.parent = parent.transform;

        // Bleachers
        for (int row = 0; row < 5; row++)
        {
            GameObject bleacher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bleacher.name = $"Bleacher Row {row + 1}";
            bleacher.transform.parent = seating.transform;
            bleacher.transform.localPosition = new Vector3(40 + row * 2, row * 0.5f, 0);
            bleacher.transform.localScale = new Vector3(1f, 0.5f, 30f);
        }
    }
}