using UnityEngine;
using System;

public class StartUp : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Rigidbody player;

    [Header("BCT Zone Targets")]
    [SerializeField] private GameObject targetAlcohol;
    [SerializeField] private GameObject targetSmoking;
    [SerializeField] private GameObject targetMfn;

    [Header("Regular World Targets")]
    [SerializeField] private GameObject targetTravel;
    [SerializeField] private GameObject targetBeaches;
    [SerializeField] private GameObject targetHome;

    [Header("UI References")]
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private SetVrState setVrState;
    [SerializeField] private togglingXR togglingXR;

    [Header("Stage Management")]
    [SerializeField] private int stage;
    [SerializeField] private string currentZone;
    [SerializeField] private bool toggler;

    [Header("Alcohol Treatment")]
    [SerializeField] private GameObject alcoholintro;
    [SerializeField] private GameObject alcoholfollow;
    [SerializeField] private GameObject alcoholsolution;
    [SerializeField] private GameObject alcoholfinish;

    [Header("Smoking Treatment")]
    [SerializeField] private GameObject welcomeSmoking;
    [SerializeField] private GameObject xrayResults;
    [SerializeField] private GameObject stopGoCtscan;
    [SerializeField] private GameObject CTresults;
    [SerializeField] private GameObject smokingDone;
    [SerializeField] private setCTdate setCTdate;
    [SerializeField] private int stopFilm;
    [SerializeField] private GameObject hud;

    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<Rigidbody>();
        ResetScene();
    }

    public void ResetScene()
    {
        Debug.Log("Marker: ResetScene");
        InitializeScene();
        HandleZoneNavigation();
    }

    private void InitializeScene()
    {
        Debug.Log("Marker: InitializeScene");

        togglingXR = FindFirstObjectByType<togglingXR>();
        togglingXR.SwitchingVR();

        currentZone = PlayerPrefs.GetString("lastknownzone", "Home_tgt");
        Debug.Log($"Marker: lastknownzone -> currentZone: {currentZone}");

        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }
        else
        {
            Debug.LogError("Marker: closeAllHuds is not assigned!");
        }

        toggler = false;
    }



    private void HandleZoneNavigation()
    {
        Debug.Log("Marker: HandleZoneNavigation");

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
        Debug.Log("Marker: IsBCTZone");
        return zone switch
        {
            "Smoking_tgt" or "Alcohol_tgt" or "MFN_tgt" => true,
            _ => false
        };
    }

    private void SetPlayerToTarget(GameObject target)
    {
        Debug.Log("Marker: SetPlayerToTarget");
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
            Debug.LogError($"Marker: Missing reference - Target: {target}, Player: {player}");
        }
    }

    private void NavigateToBCTZone()
    {
        Debug.Log($"Marker: Navigating to BCT zone: {currentZone}");

        switch (currentZone)
        {
            case "Alcohol_tgt":
                HandleAlcoholTreatment();
                break;

            case "Smoking_tgt":
                HandleSmokingTreatment();
                break;

            case "MFN_tgt":
                SetPlayerToTarget(targetMfn);
                break;

            default:
                Debug.LogError($"Marker: Unhandled BCT zone: {currentZone}");
                NavigateToRegularZone();
                break;
        }
    }

    private void NavigateToRegularZone()
    {
        Debug.Log("Marker: NavigateToRegularZone");
        Debug.Log($"Marker: Navigating to regular zone: {currentZone}");

        switch (currentZone)
        {
            case "Travel_tgt":
                SetPlayerToTarget(targetTravel);
                break;

            case "Beaches_tgt":
                SetPlayerToTarget(targetBeaches);
                break;

            default:
                SetPlayerToTarget(targetHome);
                break;
        }
    }

    private void HandleAlcoholTreatment()
    {
        Debug.Log("Marker: HandleAlcoholTreatment");
        SetPlayerToTarget(targetAlcohol);
        stage = PlayerPrefs.GetInt("stageAlcohol");

        alcoholintro.SetActive(stage == 0);
        alcoholfollow.SetActive(stage == 1);
        alcoholsolution.SetActive(stage == 2);
        alcoholfinish.SetActive(stage == 3);
    }

    private void HandleSmokingTreatment()
    {
        Debug.Log("Marker: HandleSmokingTreatment");
        Debug.Log("Marker: Handling smoking treatment zone");
        SetPlayerToTarget(targetSmoking);
        stage = PlayerPrefs.GetInt("stageSmoking");

        // Reset all smoking treatment objects
        welcomeSmoking.SetActive(false);
        xrayResults.SetActive(false);
        stopGoCtscan.SetActive(false);
        CTresults.SetActive(false);
        smokingDone.SetActive(false);

        switch (stage)
        {
            case 0:
                welcomeSmoking.SetActive(true);
                break;
            case 1:
                xrayResults.SetActive(true);
                break;
            case 2:
                stopGoCtscan.SetActive(true);
                setCTdate = FindFirstObjectByType<setCTdate>();
                setCTdate.setReferenceDate();
                break;
            case 3:
                PlayerPrefs.DeleteKey("CTstartpoint");
                PlayerPrefs.DeleteKey("delaynotification");
                CTresults.SetActive(true);
                break;
            case 4:
                smokingDone.SetActive(true);
                break;
        }

        player.transform.localPosition = Vector3.zero;
    }
}
