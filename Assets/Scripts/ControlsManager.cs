using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class ControlsManager : MonoBehaviour
{
    private Main main;
    private ExitSpawner exitSpawner;

    private AnchorManager anchorManager;
    private OVRVirtualKeyboard keyboard;

    // Start is called before the first frame update

    private void Awake()
    {
        main = GetComponent<Main>();
        exitSpawner = GetComponent<ExitSpawner>();

        anchorManager = GetComponent<AnchorManager>();
        keyboard = FindObjectOfType<OVRVirtualKeyboard>();
    }


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                StartCoroutine(main.LoadSceneFromDevice());
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                StartCoroutine(DeleteData());
            }
             
            //  Cambiare tasto altrimenti spawna segnaletica durante il salvataggio del piano
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                exitSpawner.spawnExitMarker(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch));
            }
            
        }


    }

    

    private IEnumerator DeleteData()
    {
        var path = main.path;
        if (File.Exists(path))
        {
            Debug.Log("cancellato il path");
            File.Delete(path);
            var deleteTask = Task.Run(async () => await anchorManager.EraseAllAnchors());
            yield return deleteTask;
        }
        else
        {
            Debug.Log("non ci sono dati da cancellare");
        }
    }

    
}
