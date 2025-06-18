using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartUp : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Rigidbody player;

    [Header("BCT Zone Targets")]
    [SerializeField] private GameObject targetAlcohol;
    [SerializeField] private GameObject targetSmoking;
    [SerializeField] private GameObject targetMfn;
    [SerializeField] private GameObject targetHeights;

    [Header("Regular World Targets")]
    [SerializeField] private GameObject targetTravel;
    [SerializeField] private GameObject targetBeaches;
    [SerializeField] private GameObject targetSport;
    [SerializeField] private GameObject targetHome;


    [Header("UI References")]
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private SetVrState setVrState;
    [SerializeField] private togglingXR togglingXR;

    [Header("Stage Management")]
    private int stage;
    private string currentZone;
    private string lastKnownZone;
    [SerializeField] private bool toggler;

    [Header("Alcohol Treatment")]
    [SerializeField] private GameObject alcoholintro;
    [SerializeField] private GameObject alcoholfollow;
    [SerializeField] private GameObject alcoholsolution;
    [SerializeField] private GameObject alcoholfinish;

    [Header("Smoking Treatment")]
    [SerializeField] private GameObject welcomeSmoking;
    [SerializeField] private GameObject InitialConsultation;
    [SerializeField] private GameObject CTScanDelay;
    [SerializeField] private GameObject CTresults;
    [SerializeField] private GameObject smokingDone;
    [SerializeField] private int stopFilm;
    [SerializeField] private GameObject hud;

    private void Start()
    {
        StartCoroutine(WaitForAddressablesAndSetupScene());
    }

    private IEnumerator WaitForAddressablesAndSetupScene()
    {
        // Check if the BackgroundPreloader exists and wait for it to complete
        BackgroundPreloader preloader = BackgroundPreloader.Instance;

        if (preloader != null && !preloader.PreloadingComplete)
        {
            Debug.Log("[StartUp] Waiting for addressables to finish loading...");

            // Wait until preloading is complete or a timeout occurs
            float timeoutSeconds = 10f;
            float elapsedTime = 0f;

            while (!preloader.PreloadingComplete && elapsedTime < timeoutSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log("[StartUp] Addressables finished loading");
        }

        // Now proceed with scene setup
        player = GameObject.Find("Player").GetComponent<Rigidbody>();
        ResetScene();
    }

    public void ResetScene()
    {
        Debug.Log("[StartUp] Marker: ResetScene");

        // Retrieve values from PlayerPrefs
        stage = PlayerPrefs.GetInt("stage", 0);
        lastKnownZone = PlayerPrefs.GetString("lastknownzone", "Home");
        currentZone = lastKnownZone; // Set currentZone to the value of lastKnownZone

        Debug.Log($"[StartUp] Marker: PlayerPrefs - bct: {PlayerPrefs.GetString("bct")}, lastknownzone: {lastKnownZone}, stagesmoking: {PlayerPrefs.GetInt("stagesmoking")}");

        InitializeScene();
        HandleZoneNavigation();
    }

    private void InitializeScene()
    {
        Debug.Log("[StartUp] Marker: InitializeScene");

        togglingXR = FindFirstObjectByType<togglingXR>();
        togglingXR.SwitchingVR();

        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }
        else
        {
            Debug.LogError("[StartUp] Marker: closeAllHuds is not assigned!");
        }

        toggler = false;

        // Move the player to the correct zone when the scene starts
        MovePlayerToCurrentZone();
    }

    private void HandleZoneNavigation()
    {
        Debug.Log("[StartUp] Marker: HandleZoneNavigation");

        if (IsBCTZone(currentZone))
        {
            NavigateToBCTZone();
        }
        else
        {
            NavigateToRegularZone();
        }
    }

    private bool IsBCTZone(string zone)
    {
        Debug.Log("[StartUp] Marker: IsBCTZone");
        return zone switch
        {
            "Smoking" or "Alcohol" or "Mindfulness" => true,
            _ => false
        };
    }

    private void SetPlayerToTarget(GameObject target)
    {
        Debug.Log("[StartUp] Marker: SetPlayerToTarget");
        if (target != null && player != null)
        {
            // Move the player to the target location and set it as a child of the target
            player.transform.SetParent(target.transform);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;
            player.isKinematic = true;
            player.linearVelocity = Vector3.zero;
            player.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError($"[StartUp] Marker: Missing reference - Target: {target}, Player: {player}");
        }
    }

    private void MovePlayerToCurrentZone()
    {
        Debug.Log($"[StartUp] Marker: MovePlayerToCurrentZone -> currentZone: {currentZone}");

        // Try to use ZoneManager if available
        ZoneManager zoneManager = FindFirstObjectByType<ZoneManager>();
        if (zoneManager != null)
        {
            zoneManager.MoveToZoneByName(currentZone);
        }
        else
        {
            // Fallback to original code
            switch (currentZone)
            {
                case "Alcohol":
                    SetPlayerToTarget(targetAlcohol);
                    break;

                case "Smoking":
                    SetPlayerToTarget(targetSmoking);
                    break;

                case "Mindfulness":
                    SetPlayerToTarget(targetMfn);
                    break;

                case "Travel":
                    SetPlayerToTarget(targetTravel);
                    break;

                case "Beaches":
                    SetPlayerToTarget(targetBeaches);
                    break;

                case "Sport":
                    SetPlayerToTarget(targetSport);
                    break;

                case "Heights":
                    SetPlayerToTarget(targetHeights);
                    break;

                default:
                    SetPlayerToTarget(targetHome);
                    break;
            }
        }
    }

    private void NavigateToBCTZone()
    {
        Debug.Log($"[StartUp] Marker: Navigating to BCT zone: {currentZone}");

        switch (currentZone)
        {
            case "Alcohol":
                HandleAlcoholTreatment();
                break;

            case "Smoking":
                HandleSmokingTreatment();
                break;

            case "Mindfulness":
                SetPlayerToTarget(targetMfn);
                break;

            default:
                Debug.LogError($"[StartUp] Marker: Unhandled BCT zone: {currentZone}");
                NavigateToRegularZone();
                break;
        }
    }

    private void NavigateToRegularZone()
    {
        Debug.Log("[StartUp] Marker: NavigateToRegularZone");
        Debug.Log($"[StartUp] Marker: Navigating to regular zone: {currentZone}");

        switch (currentZone)
        {
            case "Travel":
                SetPlayerToTarget(targetTravel);
                break;

            case "Beaches":
                SetPlayerToTarget(targetBeaches);
                break;

            case "Sport":
                SetPlayerToTarget(targetSport);
                break;

            case "Heights":
                SetPlayerToTarget(targetHeights);
                break;

            default:
                SetPlayerToTarget(targetHome);
                break;
        }
    }

    private void HandleAlcoholTreatment()
    {
        Debug.Log("[StartUp] Marker: HandleAlcoholTreatment");
        SetPlayerToTarget(targetAlcohol);
        stage = PlayerPrefs.GetInt("stageAlcohol");

        alcoholintro.SetActive(stage == 0);
        alcoholfollow.SetActive(stage == 1);
        alcoholsolution.SetActive(stage == 2);
        alcoholfinish.SetActive(stage == 3);
    }

    private void HandleSmokingTreatment()
    {
        Debug.Log("[StartUp] Marker: HandleSmokingTreatment");
        Debug.Log("[StartUp] Marker: Handling smoking treatment zone");
        SetPlayerToTarget(targetSmoking);
        stage = PlayerPrefs.GetInt("stageSmoking");

        // Reset all smoking treatment objects
        welcomeSmoking.SetActive(false);
        InitialConsultation.SetActive(false);
        CTScanDelay.SetActive(false);
        CTresults.SetActive(false);
        smokingDone.SetActive(false);

        switch (stage)
        {
            case 1:
                welcomeSmoking.SetActive(true);
                break;

            case 2:
                InitialConsultation.SetActive(true);
                //    setCTdate = FindFirstObjectByType<setCTdate>();
                //CTScanDelay.setReferenceDate();
                break;
            case 3:
                PlayerPrefs.DeleteKey("CTstartpoint");
                PlayerPrefs.DeleteKey("delaynotification");
                //    CTScanDelay.SetActive(true);
                break;
            case 4:
                CTresults.SetActive(true);
                break;
            case 5:
                InitialConsultation.SetActive(true);
                break;

            default:
                SetPlayerToTarget(targetHome);
                break;
        }

        player.transform.localPosition = Vector3.zero;
    }
}