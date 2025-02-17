using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class tutorialPages : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject page1;
    public GameObject page2;
    public GameObject page3;
    public GameObject page4;
    public GameObject page5;
    public GameObject page6;
    public GameObject page7;
    public GameObject page8;
    public GameObject page9;
    public GameObject page10;
    public GameObject page11;
    public TMP_Text currentPage;
    public TMP_Text totalPages;
    public bool forward;
    private int ppPageNumber;
    private int ppPageNumberNew;
    private string pages = "11";
    public GameObject btnprevious;
    public GameObject btnnext;
    public GameObject btnMasterChange;
    public void Start()
    {
        if (PlayerPrefs.HasKey("tutorialPage"))
        {
            ppPageNumber = PlayerPrefs.GetInt("tutorialPage");
           
        }
        else
        {
            ppPageNumber = 1;
        }
        ppPageNumberNew = ppPageNumber;
        setPage();
    }
    public void updateScreen(bool increment)

    {
        forward = increment;


        ppPageNumber = PlayerPrefs.GetInt("tutorialPage");
        if (forward)
        {
            
            ppPageNumberNew = ppPageNumber + 1;
        }
        else
        { 
            ppPageNumberNew = ppPageNumber - 1;
        }
       
        PlayerPrefs.SetInt("tutorialPage", ppPageNumberNew);
        setPage();
    }

    public void setPage()
    {
        switch (ppPageNumberNew)
        {
            case 1:
           
                page1.SetActive(true);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(false);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;

            case 2:
                page1.SetActive(false);
                page2.SetActive(true);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 3:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(true);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 4:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(true);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 5:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(true);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 6:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(true);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 7:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(true);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 8:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(true);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;

            case 9:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(true);
                page10.SetActive(false);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;
            case 10:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(true);
                page11.SetActive(false);

                btnprevious.SetActive(true);
                btnnext.SetActive(true);
                btnMasterChange.SetActive(false);
                break;

            case 11:
                page1.SetActive(false);
                page2.SetActive(false);
                page3.SetActive(false);
                page4.SetActive(false);
                page5.SetActive(false);
                page6.SetActive(false);
                page7.SetActive(false);
                page8.SetActive(false);
                page9.SetActive(false);
                page10.SetActive(false);
                page11.SetActive(true);

                btnprevious.SetActive(true);
                btnnext.SetActive(false);
                btnMasterChange.SetActive(true);
                break;
        }
        totalPages.text = pages;
        currentPage.text = ppPageNumberNew.ToString();
    }
}
