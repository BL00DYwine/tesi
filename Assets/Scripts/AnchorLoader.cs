using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class AnchorLoader : MonoBehaviour
{

    public OVRSpatialAnchor anchorPrefab;
    private AnchorManager anchorManager;

    private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors;


    private void Awake()
    {
        anchorManager = GetComponent<AnchorManager>();
        anchorPrefab = anchorManager.anchorPrefab;
        _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
    }

    /* VERSIONE CON PLAYER PREFS
    public async Task LoadAnchors()
    {
        if (!PlayerPrefs.HasKey(AnchorManager.NumUuidPlayerPref))
        {
            PlayerPrefs.SetInt(AnchorManager.NumUuidPlayerPref, 0);
        }

        int playerUuidCount = PlayerPrefs.GetInt(AnchorManager.NumUuidPlayerPref);
        if (playerUuidCount == 0)
        {
            return;
        }

        var uuids = new List<Guid>();
        for (int i = 0; i < playerUuidCount; i++)
        {
            string uuidKey = "uuid" + i;
            string currentUuid = PlayerPrefs.GetString(uuidKey);

            try
            {
                if (!string.IsNullOrEmpty(currentUuid))
                {
                    uuids.Add(new Guid(currentUuid));
                }
                else
                {
                    Debug.LogWarning($"UUID at index {i} is null or empty.");
                }
            }
            catch (FormatException e)
            {
                Debug.LogError($"Invalid GUID format at index {i}: {currentUuid}. Error: {e.Message}");
            }
        }

        // Convert the list to an array
        Guid[] uuidArray = uuids.ToArray();

        // Create a list to hold the unbound anchors
        List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

        // Load the unbound anchors
        await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuidArray, _unboundAnchors);

        Debug.Log("il contenuto dei unboundAncorList è : "+ _unboundAnchors.Count);

        foreach (OVRSpatialAnchor.UnboundAnchor unboundAnchor in _unboundAnchors)
        {
            Debug.Log("adesso localizzo l'anchor");
            await unboundAnchor.LocalizeAsync();
            // Wait until the anchor is localized
            while (!unboundAnchor.Localized)
            {
                await Task.Yield(); // Yield execution until the next frame
            }

            // Now it's safe to use the pose
            Pose pose;
            unboundAnchor.TryGetPose(out pose);
            Debug.Log("ANCHOR LOCALIZZATO");
            Debug.Log("Posizione : "+ pose.position);
            var spatialAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);
            spatialAnchor.name = unboundAnchor.Uuid.ToString();
            Debug.Log("anchor caricato con nome : "+ spatialAnchor.name);
            unboundAnchor.BindTo(spatialAnchor);
        }

        Debug.Log("All anchors loaded and localized.");
    }

    */

    public async Task LoadAnchors()
    {
        Debug.Log("avvio caricamento degli anchor da json");
        string json = File.ReadAllText(Application.persistentDataPath + "/data.Json");
        RoomsDataJson roomdataJson = JsonUtility.FromJson<RoomsDataJson>(json);
        Debug.Log("la lunghezza è: "+ roomdataJson.anchorUuidsList.Count);
        var uuids = new List<Guid>();
        foreach ( var uuid in roomdataJson.anchorUuidsList )
        {
            Debug.Log("creo nuovo GUID con " + uuid);
            uuids.Add(new Guid(uuid));
        }
        
        Guid[] uuidArray = uuids.ToArray();
        List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
        await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuidArray, _unboundAnchors);
        Debug.Log("il contenuto dei unboundAncorList è : " + _unboundAnchors.Count);
        foreach (OVRSpatialAnchor.UnboundAnchor unboundAnchor in _unboundAnchors)
        {
            await unboundAnchor.LocalizeAsync();
            while (!unboundAnchor.Localized)
            {
                await Task.Yield(); 
            }
            Pose pose;
            unboundAnchor.TryGetPose(out pose);
            Debug.Log("ANCHOR LOCALIZZATO");
            Debug.Log("Posizione : " + pose.position);

            //Setto la label per capire i nomi delle stanze
            var spatialAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);
            spatialAnchor.name = unboundAnchor.Uuid.ToString();
            // spatialAnchor.GetComponent<AnchorLabelManager>().setLabel(spatialAnchor.name);
            unboundAnchor.BindTo(spatialAnchor);
        }
        Debug.Log("All anchors loaded and localized.");
    }

}
