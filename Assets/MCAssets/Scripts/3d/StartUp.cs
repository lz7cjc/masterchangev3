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
        Debug.Log($"ResetScene");
        InitializeScene();
        HandleZoneNavigation();
    }

    private void InitializeScene()
    {
        Debug.Log($"InitializeScene");

        togglingXR = FindFirstObjectByType<togglingXR>();
        togglingXR.SwitchingVR();

        currentZone = PlayerPrefs.GetString("lastknownzone", "Home_tgt");
        Debug.Log($"lastknownzone -> currentZone: {currentZone}");
        closeAllHuds.CloseTheHuds();

        toggler = false;
    }

    private void HandleZoneNavigation()
    {
        Debug.Log($"HandleZoneNavigation");

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
        Debug.Log($"IsBCTZone");
        return zone switch
        {
            "Smoking_tgt" or "Alcohol_tgt" or "MFN_tgt" => true,
            _ => false
        };
    }

    private void SetPlayerToTarget(GameObject target)
    {
        Debug.Log($"SetPlayerToTarget");
        if (target != null && player != null)
        {
            player.transform.position = target.transform.position;
            player.transform.SetParent(target.transform);
            player.isKinematic = true;
            player.linearVelocity = Vector3.zero;
            player.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError($"Missing reference - Target: {target}, Player: {player}");
        }
    }

    private void NavigateToBCTZone()
    {

        Debug.Log($"Navigating to BCT zone: {currentZone}");

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
                Debug.LogError($"Unhandled BCT zone: {currentZone}");
                NavigateToRegularZone();
                break;
        }
    }

    private void NavigateToRegularZone()
    {
        Debug.Log($"NavigateToRegularZone");
        Debug.Log($"Navigating to regular zone: {currentZone}");

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
        Debug.Log($"HandleAlcoholTreatment");
        SetPlayerToTarget(targetAlcohol);
        stage = PlayerPrefs.GetInt("stageAlcohol");

        alcoholintro.SetActive(stage == 0);
        alcoholfollow.SetActive(stage == 1);
        alcoholsolution.SetActive(stage == 2);
        alcoholfinish.SetActive(stage == 3);
    }

    private void HandleSmokingTreatment()
    {
        Debug.Log($"HandleSmokingTreatment");
        Debug.Log("Handling smoking treatment zone");
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
