using Meta.XR.BuildingBlocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;
    public const string NumUuidPlayerPref = "numUuids";

    private List<OVRSpatialAnchor> anchors = new List<OVRSpatialAnchor>();
    private AnchorLoader anchorLoader;


    private void Awake()
    {
        anchorLoader = GetComponent<AnchorLoader>();
    }

    public async Task<Guid?> CreateSpatialAnchor(Vector3 pos)
    {
        Debug.Log("Instanzio il gameObJ");
        if (anchorPrefab == null)
        {
            Debug.Log("Il prefab è null");
            return Guid.Empty; // Restituisce un Guid vuoto in caso di errore
        }

        // Crea l'anchor e ottieni il Guid
        Guid? anchorGuid = await CreateAnchorAsync(pos);

        return anchorGuid;
    }

    private async Task<Guid?> CreateAnchorAsync(Vector3 pos)
    {
        OVRSpatialAnchor newAnchor = Instantiate(anchorPrefab, pos, Quaternion.identity);
        // Attende il completamento del metodo AnchorCreated e ottiene il Guid
        Guid? anchorGuid = await AnchorCreated(newAnchor);
        if(anchorGuid != null)
        {
            newAnchor.gameObject.name = anchorGuid.ToString();
            Debug.Log("Anchor istanziato con nome : " + anchorGuid.ToString());

            return anchorGuid;
        }
        return null;
        
    }

    private async Task<Guid?> AnchorCreated(OVRSpatialAnchor anchor)
    {
        try
        {
            await Task.Run(async () =>
            {
                while (!anchor.Created && !anchor.Localized)
                {
                    await Task.Yield(); // Attendiamo il prossimo frame
                }
            });

            Guid anchorGuid = anchor.Uuid;
            anchors.Add(anchor);
            Debug.Log("Anchor creato");

            await SaveAnchor(anchor);

            return anchorGuid;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task SaveAnchor(OVRSpatialAnchor anchor)
    {
        try
        {
            // Attende il completamento dell'operazione di salvataggio
            await anchor.SaveAnchorAsync(); //Salva l'anchor nel sistema 
            Debug.Log("ANCHOR SALVATO CORRETTAMENTE");
            //SaveUuid(anchor.Uuid); //ABILITARE SE SI USA PLAYER PREFS
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save anchor: {ex.Message}");
        }
    }

    private void SaveUuid(Guid uuid)
    {
        /*  VERSIONE CON I PLAYER PREFS */
        if (!PlayerPrefs.HasKey(NumUuidPlayerPref))
        {
            PlayerPrefs.SetInt(NumUuidPlayerPref, 0);
        }
        int playerNumUuids = PlayerPrefs.GetInt(NumUuidPlayerPref);
        PlayerPrefs.SetString("uuid" + playerNumUuids, uuid.ToString());
        PlayerPrefs.SetInt(NumUuidPlayerPref, ++playerNumUuids);
        Debug.Log("anchor salvato in posizione " + playerNumUuids + " con uuid: " + uuid.ToString());
        
    }


    //VERSIONE CON JSON
    public void SaveUuids(List<Guid> uuids)
    {
        var path = Application.persistentDataPath + "/data.Json";
        string json = File.ReadAllText(path);
        RoomsDataJson roomsDataJson = JsonUtility.FromJson<RoomsDataJson>(json);
        foreach (var uuid in uuids)
        {
            roomsDataJson.anchorUuidsList.Add(uuid.ToString());
            Debug.Log("anchor salvato nel json con id: " + uuid.ToString());
        }
        json = JsonUtility.ToJson(roomsDataJson, true);
        File.WriteAllText(path, json);
    }





    public async Task EraseAllAnchors()  //DA USARE IN CASO DI CANCELLAZIONE DEI DATI
    {
        foreach (OVRSpatialAnchor anchor in anchors)
        {
            await EraseAnchor(anchor);
        }

        anchors.Clear();
        //ClearAllUuidsFromPlayerPrefs();  //Abilitare solo nel caso si usi Player Prefs
    }

    private void ClearAllUuidsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(NumUuidPlayerPref))
        {
            int plyaerNumUuids = PlayerPrefs.GetInt(NumUuidPlayerPref);
            for (int i = 0; i < plyaerNumUuids; ++i)
            {
                PlayerPrefs.DeleteKey("uuid" + i);
            }
            PlayerPrefs.DeleteKey(NumUuidPlayerPref);
            PlayerPrefs.Save();
        }
    }

    public async Task EraseAnchor(OVRSpatialAnchor anchor)
    {
        try
        {
            await anchor.EraseAnchorAsync();
            Debug.Log("Anchor erased successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to erase anchor: {ex.Message}");
        }
    }


    public async Task LoadAnchorsAsync()
    {
        Debug.Log("Avvio procedura di caricamento degli anchor");

        if (anchorLoader != null)
        {
            await anchorLoader.LoadAnchors();
        }
        else
        {
            anchorLoader = new AnchorLoader();
            anchorLoader.anchorPrefab = anchorPrefab;
            await anchorLoader.LoadAnchors();
        }
    }

}




