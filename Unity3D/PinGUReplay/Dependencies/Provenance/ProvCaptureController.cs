using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PinGU;

public class ProvCaptureController : MonoBehaviour
{
    public static ProvCaptureController instance;
    ProvenanceController provController;

    void Awake()
    {
        instance = this;
        provController = GetComponent<ProvenanceController>();
    }

    public void ExportProv()
    {
        Debug.Log("Exported");
        string filename = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        provController.Save("pingu-data/provenance-data-"+filename);
    } 
}
