using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR;

public class Main : MonoBehaviour
{
    [HideInInspector]
    public string path;
    public Transform player;
    public MRUKRoom currentRoom;
    public GameObject GuiFloorNameObject;
    public RoomsDataJson roomsDataJson = new RoomsDataJson();
    public List<MRUKRoom> targetRooms = new List<MRUKRoom>();   //Sfrutto variabile booleana per il calcolo percorso minimo

    private MRUK mruk;
    private EffectMesh effectMesh;
    private bool savedData = false;
    private AnchorManager anchorManager;
    private ExitSpawner exitSpawner;
    private PathFinder pathFinder;

    public string currentRoomName = "";
    private string prevRoomName = "";  //setto le adiacenze 

    private List<Floor> floorList = new List<Floor>();
    private Floor currentFloor = null;

    private float tollerance = 1f;

    private void Awake()
    {
        mruk = FindObjectOfType<MRUK>();
        pathFinder = FindObjectOfType<PathFinder>();
        effectMesh = FindObjectOfType<EffectMesh>();

        anchorManager = GetComponent<AnchorManager>();
        exitSpawner = GetComponent<ExitSpawner>();
        path = Application.persistentDataPath + "/data.Json";
        if (File.Exists(path))
        {
            //verifica su quale modalità di avvio deve seguire l'applicazione
            Debug.Log("il path esiste");
            savedData = true;
        }
        else
        {
            //se non ci sono dati allora crea un nuovo json
            DateTime today = DateTime.Now;
            roomsDataJson = new RoomsDataJson(); //creo un nuovo RoomsDataJson contenente la data e un json nuovo 
            roomsDataJson.jsonDataName = "Dati del " + today;
        }


        if (!Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
        {
            Permission.RequestUserPermission(OVRPermissionsRequester.ScenePermission);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        if (savedData == true)
        {
            //se il json era già presente allora avvia il caricamento delle stanze e anchor
            try
            {
                StartCoroutine(LoadSceneAndSetFloor());
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(player.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            string roomName = hit.collider.gameObject.transform.parent.parent.name;
            currentRoomName = roomName;
            currentRoom = hit.collider.gameObject.transform.parent.parent.GetComponent<MRUKRoom>();
            //Debug.Log("nome della stanza corrente è: "+ currentRoomName);

            if (currentRoomName != prevRoomName)
            {
                SetStanzeAdiacenti(prevRoomName, currentRoomName);
                Debug.Log("Entrato nella stanza: " + currentRoomName);
                Debug.Log("Hai lasciato la stanza: " + prevRoomName);
                prevRoomName = currentRoomName;
            }
        }

        if (currentFloor != null)
        {
            /*
            Debug.Log("nome del piano : " + currentFloor.floorName);
            Debug.Log("UpperFloor: " + (currentFloor.upperFloor != null ? currentFloor.upperFloor.floorName : "null"));
            Debug.Log("LowerFloor: " + (currentFloor.lowerFloor != null ? currentFloor.lowerFloor.floorName : "null"));
            */
            if (currentFloor.roomAnchor == null)
            {
                Debug.Log("stanza nulla");
                return;
            }

            var localPos = currentFloor.roomAnchor.transform.InverseTransformPoint(player.position);
            // Debug.Log("la pos globale del player : " + player.position.y);
            //Debug.Log("player pos: " + localPos.y + ".  MIN è : "+ currentFloor.bounds.min.y + ". MAX : "+ currentFloor.bounds.max.y);
            if (!(localPos.y >= currentFloor.bounds.min.y - tollerance && localPos.y <= currentFloor.bounds.max.y + tollerance))
            {
                Debug.Log("HAI CAMBIATO PIANO");
                if (localPos.y < currentFloor.bounds.min.y)
                {
                    Debug.Log("sei passato al piano inferiore");
                    if (currentFloor.lowerFloor != null)
                    {
                        currentFloor = currentFloor.lowerFloor;
                    }
                }
                else
                {
                    Debug.Log("sei passato al piano superiore");
                    if (currentFloor.upperFloor != null)
                    {
                        currentFloor = currentFloor.upperFloor;
                    }
                    else
                    {
                        Debug.Log("piano inesistente");
                    }
                }
            }
        }
    }



    IEnumerator LoadSceneAndSetFloor()
    {
        yield return StartCoroutine(LoadSceneFromJson());
        MRUKRoom room = null;
        foreach (MRUKRoom mrukRoom in mruk.Rooms)
        {
            if (mrukRoom.IsPositionInRoom(player.transform.position, true))
            {
                Debug.Log("il player si trova nella stanza " + mrukRoom.name);
                room = mrukRoom;
                break;
            }
        }
        if (room != null)
        {
            Debug.Log("la stanza iniziale è : " + room.name);
            GetFloorByRoom(room);  //otteniamo il riferimento al piano corrente 

        }
        else
        {
            Debug.Log("la stanza iniziale è null");
        }
        pathFinder.GetComponent<ArrowHandler>().calculatePath = true;
    }

    private void GetFloorByRoom(MRUKRoom room)
    {
        foreach (Floor floor in floorList)
        {
            if (floor.rooms.Contains(room))
            {
                currentFloor = floor;
                Debug.Log("il current floor é :" + currentFloor.floorName);
                if (currentFloor.upperFloor != null)
                {
                    Debug.Log("il piano superiorie é : " + currentFloor.upperFloor.floorName);
                }

                if (currentFloor.lowerFloor != null)
                {
                    Debug.Log("il piano inferiore é : " + currentFloor.lowerFloor.floorName);
                }
            }
        }
    }

    public IEnumerator LoadSceneFromDevice()
    {
        Debug.Log("AVVIO CARICAMENTO DELLA SCENAAAAAAAAA");
        var mrukTask = mruk.LoadSceneFromDevice(true, false);
        yield return new WaitUntil(() => mrukTask.IsCompleted);

        Debug.Log("Terminata la scansione");

        if (mruk != null)
        {
            foreach (MRUKRoom room in mruk.Rooms)
            {
                Debug.Log(("prova stanza"));
                if (room.loadedFromJson == true)
                {
                    continue;
                }
                Debug.Log(room.name);
                effectMesh.CreateMesh(room);

                Debug.Log("Ancoraggi");
                var anchorTask = Task.Run(async () => await anchorManager.CreateSpatialAnchor(room.FloorAnchor.GetAnchorCenter()));
                yield return new WaitUntil(() => anchorTask.IsCompleted);
                room.AnchorUUid = anchorTask.Result.ToString(); //ho aggiunto la proprietà AnchorUUid alla classe MRUKRoom per associarla all'anchor
                

                var anchor = GameObject.Find(room.AnchorUUid);

                if (anchor != null)
                {
                    // Imposta room come figlio dell'ancoraggio
                    room.transform.SetParent(anchor.transform);

                    room.localPosAnchor = room.transform.localPosition;
                    room.localRotAnchor = room.transform.localRotation;
                }
            }
            StartCoroutine(SaveRooms());
        }
    }


    private IEnumerator SaveRooms()
    {
        string floorName = null;
        yield return StartCoroutine(UserPickingFloorName((name) => floorName = name));

        if (floorName != null)
        {
            string roomsJson = mruk.SaveSceneToJsonString(SerializationHelpers.CoordinateSystem.Unity); //contine tutti i dati delle MURKroom scansionate nelle varie sessioni 
            roomsDataJson.roomsJson = roomsJson;

            Floor floor = new Floor(floorName);
            //non solo di quelle scansionate in questa 
            //per ogni stanza salvo nella lista del piano i riferimenti tra Anchor e stanza con le posizioni e rotazioni locali;
            foreach (var room in mruk.Rooms)
            {
                if (room.loadedFromJson == true)
                {
                    continue; //impedisce che i dati vengano duplicati nelle varie esecuzioni
                }
                //semplicemente, se non è gia presente nel json allora crea il roomAnchorDatas riguardante  
                //significa che sono stanze nuove quindi le aggiungo al nuovo piano
                floor.rooms.Add(room);
                //roomsDataJson.anchorUuidsList.Add(room.AnchorUUid); // e ne salvo l'uuid dell'anchor associato

                Debug.Log("i dati sono: " + room.name + " , " + room.AnchorUUid + " , " + room.localPosAnchor.ToString() + " , " + room.localRotAnchor.ToString());
                var data = new RoomData(room.name, room.AnchorUUid, floorName, room.localPosAnchor, room.localRotAnchor);
                roomsDataJson.roomAnchorDatas.Add(data);
            }

            floorList.Add(floor);
            string json = JsonUtility.ToJson(roomsDataJson, true);
            File.WriteAllText(path, json);
            Debug.Log("Salvato il piano con nome: " + floorName);
        }
    }


    private IEnumerator UserPickingFloorName(Action<string> onNameSubmitted)
    {
        GuiFloorNameObject.SetActive(true);

        CustomTextHandler textHandler = GuiFloorNameObject.GetComponentInChildren<CustomTextHandler>();
        if (textHandler == null)
        {
            Debug.LogError("CustomTextHandler not found on GuiFloorNameObject.");
            yield break;
        }

        bool isSubmitted = false;
        string floorName = null;

        textHandler.OnSubmitValidNumber += (submittedText) =>
        {
            if (!string.IsNullOrEmpty(submittedText))
            {
                floorName = submittedText;
                isSubmitted = true;
            }
        };

        yield return new WaitUntil(() => isSubmitted);
        GuiFloorNameObject.SetActive(false);
        onNameSubmitted?.Invoke(floorName);
    }


    private IEnumerator LoadSceneFromJson()
    {
        string json = File.ReadAllText(path);
        roomsDataJson = JsonUtility.FromJson<RoomsDataJson>(json);

        Debug.Log("Avvio il caricamento degli anchor");

        Task anchorLoad = Task.Run(async () => await anchorManager.LoadAnchorsAsync());
        yield return new WaitUntil(() => anchorLoad.IsCompleted);

        
        Debug.Log("terminato il caricamento degli ANCHOR");

        mruk.LoadSceneFromJsonString(roomsDataJson.roomsJson, false); //ricarica tutte le stanze dal json
        foreach (var data in roomsDataJson.roomAnchorDatas)
        {
            GameObject anchor = GameObject.Find(data.anchorName);
            GameObject room = GameObject.Find(data.mrukroomName);
            var mrukroom = room.GetComponent<MRUKRoom>();
            mrukroom.loadedFromJson = true; //setta che la stanza è caricata dal json così da impedirne di crearne altre 
            mrukroom.AnchorUUid = data.anchorName; //setto di nuovo il nome dell'anchor così da poter reimpostare gli anchor trigger tra i piani
            room.transform.SetParent(anchor.transform);
            room.transform.localPosition = data.localPosAnchor;
            room.transform.localRotation = data.localRotationAnchor;
            anchor.GetComponent<AnchorLabelManager>().setLabel(room.name);

            //Ricostruisco le informazioni sulle uscite di sicurezza e le adiacenze delle stanze

            if (data.exitMarkerPosition.Count > 0)
            {
                mrukroom.hasExitMarker = true; //se ha un segnale di uscita allora imposto la variabile booleana su true
                targetRooms.Add(mrukroom); //aggiungo alla lista di stanze target così da calcolare i percorsi 

                foreach (Vector3 pos in data.exitMarkerPosition)
                {
                    var gameObj = Instantiate(exitSpawner.exitPrefabObject, pos, Quaternion.identity, anchor.transform);
                    gameObj.transform.parent  = anchor.transform;
                    gameObj.transform.localPosition = new Vector3(0, 1f, 0); // posizione da aggiustare
                }

            }

            foreach(string nome in data.nomiStanzeAdiacenti)
            {
                var adjRoom = mruk.Rooms.Find(r => r.name == nome);
                if(adjRoom != null)
                {
                    mrukroom.adjRooms.Add(adjRoom);
                }
            }

        }


        // dopo aver ricreato le stanze allora costruisco i piani
        floorList = BuildFloorsFromJson(json);
        SetFloorAdj();  //setto i trigger per il passaggio da un piano all'altro
        //pathFinder.CostruisciGrafoPorte();
        
    }


    public List<Floor> BuildFloorsFromJson(string json)  //metodo per ricostruire i piani a partire dai nomi
    {
        Debug.Log("avvio creazione piani");
        RoomsDataJson roomsDataJson = JsonUtility.FromJson<RoomsDataJson>(json);  //dal json prende solo la lista dei RoomAnchorData
        Dictionary<string, Floor> floorsDict = new Dictionary<string, Floor>();  //crea un dizionario per costruire facilmente la lista

        foreach (var roomData in roomsDataJson.roomAnchorDatas)
        {
            if (!floorsDict.ContainsKey(roomData.floorName))
            {
                Debug.Log("nuovo piano con nome: " + roomData.floorName);
                floorsDict[roomData.floorName] = new Floor(roomData.floorName);
            }

            var room = mruk.Rooms.Find(r => r.name == roomData.mrukroomName);
            floorsDict[roomData.floorName].AddRoom(room);
            Debug.Log("aggiunta la stanza " + room.name + " al piano " + roomData.floorName);
        }
        // Ordina la lista di piani in base al valore numerico del nome del piano
        List<Floor> floorList = floorsDict.Values
            .OrderBy(floor => int.Parse(floor.floorName))
            .ToList();

        foreach (var floor in floorList)
        {
            floor.roomAnchor = GameObject.Find(floor.rooms[0].AnchorUUid);
            floor.bounds = floor.rooms[0].GetRoomBounds();
        }

        return floorList;
    }

    private void SetFloorAdj() //crea spatialAnchor trigger tra i vari piani successivi
    {
        for (int i = 0; i < floorList.Count - 1; i++)
        {
            Floor prevFloor = floorList[i];
            Floor nextFloor = floorList[i + 1];

            prevFloor.upperFloor = nextFloor;
            Debug.Log("il piano superiore al piano: " + prevFloor.floorName + " è il piano: " + prevFloor.upperFloor.floorName);
            nextFloor.lowerFloor = prevFloor;  //in questo modo setto la successione dei piani
            Debug.Log("il piano inferiore a " + nextFloor.floorName + " è " + nextFloor.lowerFloor.floorName);
        }
        Debug.Log("terminato il settaggio dei piani");
    }


    public void StampaPiani()
    {
        //metodo di debug per verificare che i piani vengano costruiti e gestiti in modo corretto 
        if (floorList.Count > 0)
        {

            foreach (var floor in floorList)
            {
                Debug.Log("nome piano:" + floor.floorName);
                foreach (var room in floor.rooms)
                {
                    Debug.Log("stanza :" + room.name);
                }
                Debug.Log("");
            }
        }
        else
        {
            Debug.Log("Non ci sono piani");
        }
    }
    private void SetStanzeAdiacenti(string prevRoomName, string currentRoomName)
    {

        var prevRoomData = roomsDataJson.roomAnchorDatas.Find(t => t.mrukroomName == prevRoomName);
        var currentRoomData = roomsDataJson.roomAnchorDatas.Find(t => t.mrukroomName == currentRoomName);

        if (prevRoomData == null)
        {
            Debug.Log("prevRoomData è NULL");
            return;
        }

        if (currentRoomData == null)
        {
            Debug.Log("currentRoomData è NULL");
            return;
        }

        if (prevRoomData.floorName != currentRoomData.floorName && currentFloor.floorName == currentRoomData.floorName)
        {
            //ha effettuato il ray su una stanza del piano inferiore, a meno che l'utente non abbia cambiato piano non dovrebbe succedere 
            //altrimenti verrebbe messa l'adiacenza tra due stanze appartenenti a piani diversi 
            Debug.Log("adiacenza con una stanza di piani diversi ma il piano non è cambiato");
            return;
        }

        if (!currentRoomData.nomiStanzeAdiacenti.Contains(prevRoomName) && !prevRoomData.nomiStanzeAdiacenti.Contains(currentRoomName))
        {
            currentRoomData.nomiStanzeAdiacenti.Add(prevRoomName);
            Debug.Log("Aggiunta la stanza " + prevRoomName + " tra quelle adiacenti di " + currentRoomName);

            prevRoomData.nomiStanzeAdiacenti.Add(currentRoomName);
            Debug.Log("Aggiunta la stanza " + currentRoomName + " tra quelle adiacenti di " + prevRoomName);

            string json = JsonUtility.ToJson(roomsDataJson, true);
            Debug.Log("salvo le informazioni");
            File.WriteAllText(path, json);
        }
        else
        {
            Debug.Log("le stanze sono già adiacenti ");
        }


    }

    internal Dictionary<GameObject,MRUKRoom> GetDoors()
    {
        Dictionary<GameObject,MRUKRoom> risultato = new Dictionary<GameObject, MRUKRoom> ();
        foreach(MRUKRoom room in mruk.Rooms)
        {
            foreach(GameObject obj in room.GetDoorObject())
            {
               risultato.Add(obj, room); 
            }
        }
        return risultato;
    }
}




















[Serializable]
public class RoomsDataJson
{
    public string jsonDataName;
    public string roomsJson;
    public List<RoomData> roomAnchorDatas;
    public List<string> anchorUuidsList;

    public RoomsDataJson()
    {
        roomAnchorDatas = new List<RoomData>();
        anchorUuidsList = new List<string>();
    }
}


[Serializable]
public class RoomData
{
    //Salva le info per riassociare anchor e stanza in esecuzioni successive
    public string mrukroomName;
    public string anchorName;
    public Vector3 localPosAnchor;
    public Quaternion localRotationAnchor;
    public string floorName; //info necessaria per poter ricostruire i piani tra le varie esecuzioni
    public List<string> nomiStanzeAdiacenti;
    public List<Vector3> exitMarkerPosition;

    public RoomData(string mrukroomName, string anchorName, string floorName, Vector3 localPosAnchor, Quaternion localRotationAnchor)
    {
        this.mrukroomName = mrukroomName;
        this.floorName = floorName;
        this.anchorName = anchorName;
        this.localPosAnchor = localPosAnchor;
        this.localRotationAnchor = localRotationAnchor;
        nomiStanzeAdiacenti = new List<string>();
        exitMarkerPosition = new List<Vector3>();
    }
}

public class Floor
{
    public string floorName;
    public List<MRUKRoom> rooms;

    public Vector3 floorPos;
    public GameObject roomAnchor;
    public Bounds bounds;

    public Floor upperFloor = null;   //riferimenti tra i vari piani
    public Floor lowerFloor = null;

    public Floor(string floorName)
    {
        this.floorName = floorName;
        this.rooms = new List<MRUKRoom>();
    }

    public void AddRoom(MRUKRoom room)
    {
        rooms.Add(room);
    }

    public MRUKRoom GetRoom(string roomName)
    {
        return rooms.Find(r => r.name == roomName);
    }

    public void SetRoomsVisible()
    {
        foreach (MRUKRoom room in rooms)
        {
            room.gameObject.SetActive(true);
        }
    }

    public void SetRoomsInvisibile()
    {
        foreach (MRUKRoom room in rooms)
        {
            room.gameObject.SetActive(false);
        }
    }
}

