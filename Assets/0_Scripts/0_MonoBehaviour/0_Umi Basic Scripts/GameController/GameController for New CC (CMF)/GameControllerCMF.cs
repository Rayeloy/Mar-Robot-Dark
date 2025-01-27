﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Crest;

#region ----[ PUBLIC ENUMS ]----
#endregion

public class GameControllerCMF : MonoBehaviour
{

    #region ----[ VARIABLES FOR DESIGNERS ]----
    public bool debugModeOn = false;
    //referencias
    [Header(" --- Referencias --- ")]
    public GameInterfaceCMF myGameInterface;
    RenController myRenCont;
    //este parámetro es para poner slowmotion al juego (como estados: 0=normal,1=slowmo,2=slowestmo),
    // solo se debe usar para testeo, hay que QUITARLO para la build "comercial".
    [Header(" --- Variables generales ---")]
    public GameMode gameMode;
    public bool slowMoButtonOn = false;
    int slowmo = 0;
    public bool recordMode = false;

    //PREFABS
    [Header(" --- Player components prefabs ---")]
    public GameObject playerPrefab;
    public GameObject playerCanvasPrefab;
    public GameObject playerCameraPrefab;
    public GameObject playerUICameraPrefab;

    [Header(" --- Player components parents ---")]
    public Transform playersParent;
    public Transform playersCanvasParent;
    public Transform playersCamerasParent;
    public Transform playersUICamerasParent;

    //public AttackData[] allAttacks; //este array seria otra manera de organizar los distintos ataques. En el caso de haber muchos en vez de 3 puede que usemos algo como esto.
    //Estos son los ataques de los jugadores, seguramente en un futuro vayan adheridos al jugador, o a su arma. Si no, será un mega array (arriba)
    //[Header("Attacks")]
    //public AttackData attackX;
    //public AttackData attackY;
    //public AttackData attackB;

    [Header(" --- 'All' lists ---")]
    public BufferedInputData[] allBufferedInputs;

    [Header(" --- Spawn positions ---")]
    //Posiciones de los spawns
    public Transform[] teamASpawns;
    public Transform[] teamBSpawns;

    //Variables de HUDS
    [Header(" --- Players HUD --- ")]

    private List<RectTransform> contador;//Array que contiene todos los contadores de tiempo, solo util en Pantalla Dividida
    private List<RectTransform> powerUpPanel;//Array que contiene los objetos del dash y el hook en el HUD, solo util en Pantalla Dividida
    [Header(" --- Players HUD scale --- ")]
    public float scaleDos = 1.25f;//escala de las camaras para 2 jugadores
    public float scaleCuatro = 1.25f;//escala para 3 jugadores y 4 jugadores
    [Header(" --- Other Stuff --- ")]
    public OceanRenderer myOceanRenderer;
    //public Weapon startingWeaponA;
    //public Weapon startingWeaponB;
    #endregion

    #region ----[ PROPERTIES ]----

    Team winner = Team.none;
    [HideInInspector] public int playerNumTeamA = 0, playerNumTeamB = 0;
    private PlayerActions playerActions;
    private PlayerSpawnInfo[] playerSpawnInfoList;

    //GAME OVER MENU
    [HideInInspector]
    public bool gameOverStarted = false;

    //Player components lists
    protected List<PlayerMovementCMF> allPlayers;//Array que contiene a los PlayerMovementCMF
    protected List<CameraControllerCMF> allCameraBases;//Array que contiene todas las cameras bases, solo util en Pantalla Dividida
    protected List<GameObject> allCanvas;//Array que contiene los objetos de los canvas de cada jugador, solo util en Pantalla Dividida
    protected List<Camera> allUICameras;//Array que contiene todas las cameras bases, solo util en Pantalla Dividida

    //Number of players in the game. In online it will start at 0 and add +1 every time a player joins. In offline it stays constant since the game scene starts
    [HideInInspector]
    public int playerNum = 1;

    //Pause Menu
    [HideInInspector]
    public bool gamePaused = false;
    //variables globales de la partida
    [HideInInspector]
    public bool playing = false;

    [HideInInspector]
    public bool HasPlayerFlatCamera(PlayerMovementCMF pM)
    {
        if (allPlayers.Count == 2)
        {
            return true;
        }
        else if (allPlayers.Count == 3)
        {
            for (int i = 0; i < allPlayers.Count; i++)
            {
                if (allPlayers[i] == pM)
                {
                    if (i == 2)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        return false;
    }
    #endregion

    #region ----[ MONOBEHAVIOUR FUNCTIONS ]----

    #region Awake
    protected virtual void Awake()
    {
        //online = PhotonNetwork.IsConnected;
        //if (online)
        //{
        //    Debug.Log("GameControllerBase: estamos conectados y la base del game controller está funcionando correctamente");
        //}
        //else
        //{
        //Esto es para no entrar en escenas cuando no tenemos los controles. Te devuelve a seleccion de equipo
        //Eloy: he cambiado esto porque me he dado cuenta de que es necesario hasta en la build final, no solo en el editor.
        if (GameInfo.instance == null || GameInfo.instance.inControlManager == null)
        {
            Debug.LogWarning("GameInfo or InControlManager was not found, so we load Team Setup Scene");
            //string escena = TeamSetupManager.siguienteEscena;
            //print(escena);
            TeamSetupManager.siguienteEscena = SceneManager.GetActiveScene().name;
            TeamSetupManager.startFromMap = true;
            //TO CHANGE TO:
            Alpha_Team_Select.startFromMapScene = SceneManager.GetActiveScene().name;
            Alpha_Team_Select.startFromMap = true;
            SceneManager.LoadScene("Demo_TeamSetUp");
            return;
        }
        //}

        //GameInfo.instance.PrintTeamList();

        myRenCont = myGameInterface.GetComponentInChildren<RenController>();
        if (myRenCont == null)
        {
            Debug.LogError("GameControllerCMF-> No RenController could be found!");
        }
        //Deactivate test player components. DO NOT MOVE!
        DeactivatePlayerComponents();


        //initialize lists
        allPlayers = new List<PlayerMovementCMF>();
        allCameraBases = new List<CameraControllerCMF>();
        allCanvas = new List<GameObject>();
        allUICameras = new List<Camera>();

        contador = new List<RectTransform>();
        powerUpPanel = new List<RectTransform>();

        //Check data
        CheckValidInputsBuffer();

        //Set stuff active
        myGameInterface.gameObject.SetActive(true);

        //if (!online)
        //{
        playerNum = GameInfo.instance.nPlayers;
        playerNum = Mathf.Clamp(playerNum, 1, 4);
        for (int i = 0; i < playerNum; i++)
        {
            CreatePlayer(i + 1);
        }

        //AUTOMATIC PLAYERS & CAMERAS/CANVAS SETUP
        PlayersSetup();// Spawn positions
        SetUpCanvases();
        AllAwakes();
        //}
        //else //ONLINE //Eloy: para Juan: aqui inicia al host! playerNum deberia estar a 0 y luego ponerse a 1 cuando se crea el jugador
        //{
        //    //Calculate spawns
        //    SetSpawnPositions();
        //    //CreatePlayer
        //    int playernumber = PhotonNetwork.CurrentRoom.PlayerCount - 1;//ELOY: Para Juan: ESTO NO ESTA PREPARADO PARA CUANDO SE SALE ALGUIEN. Si eran 10 jugadores y se sale el 3
        //    //no se rellena el 3, sino el 10. Hay que buscar con un for el numero más bajo por rellenar.
        //    print("Creating player number " + playernumber);
        //    Debug.Log("0 - AllCameraBases.Length = " + allCameraBases.Count);
        //    CreatePlayer(playernumber);
        //    //PlayerSetupOnline?
        //    //No hace falta SetUpCanvas creo
        //    //Haz los awakes, y haz el awake de cada jugador nuevo(esto ultimo hay que buscar donde ponerlo... en el CreatePlayer?

        //    Debug.Log("3 - AllCameraBases.Length = " + allCameraBases.Count);
        //    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
        //    allUICameras[0].rect = new Rect(0, 0, 1, 1);
        //    //Debug.Log("Nuestro jugador es: "+ GameInfo.instance.myControls);
        //    allPlayers[0].Actions = GameInfo.instance.myControls == null ? PlayerActions.CreateWithKeyboardBindings() : GameInfo.instance.myControls;


        //    allCanvas[0].GetComponent<PlayerHUD>().AdaptCanvasHeightScale();
        //    myGameInterface.KonoAwake(this);
        //    allPlayers[0].KonoAwake(true);
        //    allCameraBases[0].KonoAwake();

        //    gameOverStarted = false;
        //    contador[0].anchoredPosition = new Vector3(contador[0].anchoredPosition.x, 100, contador[0].anchoredPosition.y);

        //    //OnlinePlayerSetup();
        //    //OnlineCanvasSetUp();
        //    //OnlineAwakePlayer();

        //}
        SpecificAwake();
    }

    protected virtual void SpecificAwake()
    {
        //code for children's awake
    }

    #endregion

    #region Start
    protected virtual void Start()
    {
        Application.targetFrameRate = 60;

        StartPlayers();
        StartGame();

        StartOceanRendererViewpoint();

        if (debugModeOn) Debug.Log("GameController Start finished");
    }

    //Funcion que llama al Start de los jugadores. Eloy: Juan, ¿solo pantalla dividida?, JUAN: Sí Eloy, sólo pantalla dividida.
    void StartPlayers()
    {
        if (debugModeOn) Debug.Log("GameControllerCMF -> StartPlayers");
        for (int i = 0; i < playerNum; i++)
        {
            if (debugModeOn) Debug.Log("GameControllerCMF -> StartPlayer " + i);
            allPlayers[i].KonoStart();
        }
    }

    //Funcion que se llama al comenzar la partida, que inicicia las variables necesarias, y que posiciona a los jugadores y ¿bandera?
    public virtual void StartGame()
    {
        playing = true;
        gamePaused = false;
        myRenCont.disabled = true;
        for (int i = 0; i < playerNum; i++)
        {
            Debug.Log("Respawning player " + (i + 1) + "/" + (playerNum));
            RespawnPlayer(allPlayers[i]);
        }
    }
    #endregion

    #region Update
    void Update()
    {
        //if (playerNum == 1)
        //{
        //    allPlayers[0].Actions = GameInfo.instance.myControls;
        //}
        //if (scoreManager.End) return;
        SlowMotion();
        SwitchLockMouse();
        UpdateOceanRendererViewpoint();

        if (!gamePaused && playing)// PLAYING NORMALLY
        {
            UpdatePlayers();
            UpdateModeExclusiveClasses();
        }
        else
        {
            if (playing)//PLAYING BUT PAUSED
            {
                if (myRenCont.currentControls.B.WasPressed || myRenCont.currentControls.Start.WasPressed)
                {
                    UnPauseGame();
                }
            }
            else// NOT PLAYING ANYMORE, GAME OVER
            {
                if (gameOverStarted && !myGameInterface.gameOverMenuOn)
                {
                    for (int i = 0; i < playerNum; i++)
                    {
                        if (myRenCont.currentControls.Start.WasReleased)
                        {
                            SwitchGameOverMenu();
                            i = playerNum;//BREAK
                        }
                    }
                }
            }
        }
        SpecificUpdate();
    }

    private void FixedUpdate()
    {
        //Debug.Log("GAME CONTROLLER FIXED UPDATE");
        if (!gamePaused && playing)
        {
            FixedUpdatePlayers();
        }
    }

    private void LateUpdate()
    {
        if (!gamePaused)
        {
            if (playing)
            {
                LateUpdatePlayers();
            }
        }
        SpecificLateUpdate();
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < playerNum; i++)
        {
            allPlayers[i].KonoUpdate();
        }
    }

    void FixedUpdatePlayers()
    {
        for (int i = 0; i < playerNum; i++)
        {
            allPlayers[i].KonoFixedUpdate();
        }
    }

    void LateUpdatePlayers()
    {
        for (int i = 0; i < playerNum; i++)
        {
            allPlayers[i].KonoLateUpdate();
        }
    }
    /// <summary>
    /// Update done at the end for the child classes.
    /// </summary>
    protected virtual void SpecificUpdate()
    {
    }

    /// <summary>
    /// Update for Capture the Whale. It is different to SpecificUpdate about when it is done. SpecificUpdate is always done at the end. This is done earlier. Check code.
    /// </summary>
    protected virtual void UpdateModeExclusiveClasses()//no borrar, es para los hijos
    {
    }

    protected virtual void SpecificLateUpdate()
    {

    }
    #endregion

    #endregion

    #region ----[ CLASS FUNCTIONS ]----

    #region --- AWAKE / CREATE PLAYERS / SPAWN POSITIONS ---

    /// <summary>
    /// Funcion que Inicializa valores de todos los jugadores y sus cámaras.
    /// </summary>
    void PlayersSetup()
    {
        //if (!online)
        //{
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (i < playerNum)
            {
                //LE DAMOS AL JUGADOR SUS CONTROLES (Mando/teclado) y SU EQUIPO
                if (allPlayers.Count == 1)
                {
                    GameInfo.instance.myControls = PlayerActions.CreateDefaultBindings();
                    if (debugModeOn) Debug.Log("My actions are = " + GameInfo.instance.myControls);
                    allPlayers[0].actions = GameInfo.instance.myControls;
                }
                else
                {
                    allPlayers[i].actions = GameInfo.instance.playerActionsList[i];
                }

                if (GameInfo.instance.playerTeamList[i] == Team.none)
                {
                    //Debug.Log("Randomizing Team for player " + i);
                    GameInfo.instance.playerTeamList[i] = GameInfo.instance.NoneTeamSelect();
                }

                //GameInfo.instance.PrintTeamList();
                allPlayers[i].team = GameInfo.instance.playerTeamList[i];
                allPlayers[i].myPlayerBody.myPlayerSkin = GameInfo.instance.playerSkinList[i];
                Debug.Log("GameController: The player " + i + " is given the skin " + GameInfo.instance.playerSkinList[i].name);
                allPlayers[i].myPlayerWeap.myWeaponSkinData = GameInfo.instance.weaponSkinList[i];
                allPlayers[i].myPlayerWeap.myWeaponSkinRecolor = GameInfo.instance.weaponSkinRecolorList[i];
            }
        }
        //SETUP CAMERAS

        if (recordMode)
        {
            switch (playerNum)
            {
                case 1:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1f, 1f);
                    allUICameras[0].rect = new Rect(0, 0, 0, 0);
                    break;
                case 2:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0.5f, 1, 0.5f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 0.5f);
                    allUICameras[0].rect = new Rect(0, 0, 0, 0);
                    allUICameras[1].rect = new Rect(0, 0, 0, 0);
                    break;
                case 3:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allCameraBases[2].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 0.5f);
                    allUICameras[0].rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allUICameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allUICameras[2].rect = new Rect(0, 0, 1, 0.5f);
                    break;
                case 4:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0f, 0f, 0.8f, 0.8f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0.8f, 0, 0.2f, 0.2f);
                    allCameraBases[2].myCamera.GetComponent<Camera>().rect = new Rect(0.8f, 0.2f, 0.2f, 0.2f);
                    allCameraBases[3].myCamera.GetComponent<Camera>().rect = new Rect(0.8f, 0.4f, 0.2f, 0.2f);
                    allCameraBases[0].myCamera.GetComponent<Camera>().depth = 1;
                    allUICameras[0].rect = new Rect(0, 0, 0, 0);
                    allUICameras[1].rect = new Rect(0, 0, 0, 0);
                    allUICameras[2].rect = new Rect(0, 0, 0, 0);
                    allUICameras[3].rect = new Rect(0, 0, 0, 0);
                    break;
            }
        }
        else
        {
            switch (playerNum)
            {
                case 1:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
                    allUICameras[0].rect = new Rect(0, 0, 1f, 1);
                    break;
                case 2:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0.5f, 1, 0.5f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 0.5f);
                    allUICameras[0].rect = new Rect(0, 0.5f, 1, 0.5f);
                    allUICameras[1].rect = new Rect(0, 0, 1, 0.5f);
                    break;
                case 3:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allCameraBases[2].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 0.5f);
                    allUICameras[0].rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allUICameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allUICameras[2].rect = new Rect(0, 0, 1, 0.5f);
                    break;
                case 4:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allCameraBases[1].myCamera.GetComponent<Camera>().rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allCameraBases[2].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 0.5f, 0.5f);
                    allCameraBases[3].myCamera.GetComponent<Camera>().rect = new Rect(0.5f, 0, 0.5f, 0.5f);
                    allUICameras[0].rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    allUICameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    allUICameras[2].rect = new Rect(0, 0, 0.5f, 0.5f);
                    allUICameras[3].rect = new Rect(0.5f, 0, 0.5f, 0.5f);
                    break;
            }
            //}
            if (gameMode != GameMode.Housing)
                SetSpawnPositions();
        }

    }

    protected virtual void AllAwakes()
    {
        myGameInterface.KonoAwake(this);
        for (int i = 0; i < allCameraBases.Count; i++)
        {
            allPlayers[i].KonoAwake();
            allCameraBases[i].KonoAwake();
        }
    }

    public Team SetRandomOnlineTeam(int SROT_playernum)
    {
        if (SROT_playernum % 2 == 0)
        {
            return Team.A;
        }
        else
        {
            return Team.B;
        }
    }

    public virtual void CreatePlayer(int playerNumber)
    {
        PlayerMovementCMF newPlayer;
        GameObject newPlayerCanvas;
        CameraControllerCMF newPlayerCamera;
        Camera newPlayerUICamera;

        //if (online)
        //{
        //    //Debug.Log("0.1 - AllCameraBases.Length = " + allCameraBases.Count);
        //    if (playerPrefab == null)
        //    {
        //        Debug.LogError("GamerControllerBase: Color=Red><a>Missing playerPrefab Reference in GameController</a></Color>");
        //    }
        //    else
        //    {
        //        Debug.Log("0.2 - AllCameraBases.Length = " + allCameraBases.Count);

        //        Debug.Log("GameControllerBase: Instantiating player over the network");
        //        //JUAN: WARNING!!, el objeto que se instancie debe estar siempre en la carpeta de Resources de Photon, o ir al método de instantiate para cambiarlo
        //        //JUAN: Eloy, donde dice Vector3 y Quartenion debe ser para establecer la posición del spawn del jugador, para hacer las pruebas lo dejo to random pero hay que mirarlo
        //        if (PlayerMovementCMF.LocalPlayerInstance == null)//Esta linea no sirve para nada y no está bien hecha la comprobación. To be erased
        //        {
        //            Debug.Log("0.3 - AllCameraBases.Length = " + allCameraBases.Count);

        //            Debug.LogFormat("GameControllerBase: We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
        //            // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
        //            Team newPlayerTeam = SetRandomOnlineTeam(playerNumber);
        //            ////Vector3 respawn = new Vector3(-200, -200, -200);
        //            ////if (newPlayerTeam == Team.A)
        //            ////{
        //            ////    respawn = teamASpawns[0].position;
        //            ////}
        //            ////else if (newPlayerTeam == Team.B)
        //            ////{
        //            ////    respawn = teamBSpawns[0].position;
        //            ////}
        //            PlayerSpawnInfo auxSpawnInfo = GetSpawnPosition(playerNumber);
        //            newPlayer = MasterManager.NetworkInstantiate(playerPrefab, auxSpawnInfo.position, Quaternion.identity).GetComponent<PlayerMovementCMF>();// PhotonNetwork.Instantiate(this.playerPrefab.name, auxSpawnInfo.position, Quaternion.identity, 0).GetComponent<PlayerMovementCMF>();
        //            newPlayer.team = newPlayerTeam;
        //            newPlayer.mySpawnInfo = auxSpawnInfo;
        //            //newPlayer.rotateObj.transform.rotation = newPlayer.mySpawnInfo.rotation;

        //            newPlayerCanvas = Instantiate(playerCanvasPrefab, playersCanvasParent);
        //            newPlayerCamera = Instantiate(playerCameraPrefab, playersCamerasParent).GetComponent<CameraController>();
        //            newPlayerUICamera = Instantiate(playerUICameraPrefab, playersUICamerasParent).GetComponent<Camera>();

        //            //nombrado de objetos nuevos
        //            newPlayer.gameObject.name = "Player " + playerNumber;
        //            newPlayerCanvas.gameObject.name = "Canvas " + playerNumber;
        //            newPlayerCamera.gameObject.name = "CameraBase " + playerNumber;
        //            newPlayerUICamera.gameObject.name = "UICamera " + playerNumber;

        //            newPlayer.playerNumber = PhotonNetwork.CurrentRoom.PlayerCount;
        //            Debug.Log("1 - AllCameraBases.Length = " + allCameraBases.Count);

        //            InitializePlayerReferences(newPlayer, newPlayerCanvas, newPlayerCamera, newPlayerUICamera);
        //            Debug.Log("final allPlayers.Count = " + allPlayers.Count + "; playerNumber = " + playerNumber);
        //            allPlayers[0].KonoAwake();
        //        }
        //        else
        //        {
        //            Debug.Log("GameControllerBase: Ignoring CreatePlayer() call because we already exist");
        //        }
        //    }
        //}
        //else//offline
        //{
        newPlayer = Instantiate(playerPrefab, playersParent).GetComponent<PlayerMovementCMF>();
        newPlayer.mySpawnInfo = new PlayerSpawnInfo();
        newPlayer.playerNumber = playerNumber;

        newPlayerCanvas = Instantiate(playerCanvasPrefab, playersCanvasParent);
        newPlayerCamera = Instantiate(playerCameraPrefab, playersCamerasParent).GetComponent<CameraControllerCMF>();
        newPlayerUICamera = Instantiate(playerUICameraPrefab, newPlayerCamera.myCamera).GetComponent<Camera>();

        //nombrado de objetos nuevos
        newPlayer.gameObject.name = "Player " + playerNumber;
        newPlayerCanvas.gameObject.name = "Canvas " + playerNumber;
        newPlayerCamera.gameObject.name = "CameraBase " + playerNumber;
        newPlayerUICamera.gameObject.name = "UICamera " + playerNumber;

        InitializePlayerReferences(newPlayer, newPlayerCanvas, newPlayerCamera, newPlayerUICamera);
        //}
    }

    //Eloy: Juan, he creado este método porque copiar y pegar lo mismo en ambos lados del if(online/offline) era un horror para mi cerebro. cada nueva referencia sería un lío, así mejor.
    void InitializePlayerReferences(PlayerMovementCMF player, GameObject canvas, CameraControllerCMF cameraBase, Camera UICamera)
    {
        //Inicializar referencias
        PlayerHUDCMF playerHUD = canvas.GetComponent<PlayerHUDCMF>();
        //Player
        player.gC = this;
        player.myCamera = cameraBase;
        player.myPlayerHUD = playerHUD;
        player.myUICamera = UICamera;
        //player.myPlayerCombat.attackNameText = playerHUD.attackNameText;
        //Canvas
        playerHUD.gC = this;
        playerHUD.myCamera = cameraBase.myCamera.GetComponent<Camera>();//newPlayerUICamera;
        playerHUD.myUICamera = UICamera;//newPlayerUICamera;
        playerHUD.myPlayerMov = player;
        playerHUD.myPlayerCombat = player.transform.GetComponent<PlayerCombatCMF>();
        canvas.GetComponent<Canvas>().worldCamera = UICamera;
        //CameraBase
        cameraBase.myPlayerMov = player;
        cameraBase.myPlayer = player.transform;
        cameraBase.cameraFollowObj = player.cameraFollow;

        //Añadir a los arrays todos los componentes del jugador
        //guarda jugador
        allPlayers.Add(player);
        //if (online)
        //{
        //    if (jugadaGalaxia)
        //    {
        //        jugadaGalaxia = false;
        //        allCanvas.Add(canvas);
        //        Debug.Log("2 - AllCameraBases.Length = " + allCameraBases.Count);

        //        allCameraBases.Add(cameraBase);
        //        allUICameras.Add(UICamera);
        //    }
        //}
        //else
        //{
        allCanvas.Add(canvas);
        allCameraBases.Add(cameraBase);
        allUICameras.Add(UICamera);
        //}

        //ignore this
        contador.Add(playerHUD.contador);
        powerUpPanel.Add(playerHUD.powerUpPanel);
    }

    //actualmente en desuso
    public virtual void RemovePlayer(PlayerMovementCMF _pM)//solo para online
    {
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i] == _pM)
            {
                allPlayers.Remove(_pM);
                allCanvas.Remove(_pM.myPlayerHUD.gameObject);
                allCameraBases.Remove(_pM.myCamera);
                allUICameras.Remove(_pM.myUICamera);

                Destroy(allPlayers[i].gameObject);
                Destroy(allCanvas[i]);
                Destroy(allCameraBases[i].gameObject);
                Destroy(allUICameras[i].gameObject);
            }
        }
    }

    PlayerSpawnInfo GetSpawnPosition(int playerNumber)
    {
        print("Setting spawn pos for Player number " + playerNumber);
        return playerSpawnInfoList[playerNumber];
    }

    /// <summary>
    /// Calcula las posiciones de spawn dentro de cada spawn (rojo y azul), de manera equidistante y centrada y se las da a los Players.
    /// </summary>
    void SetSpawnPositions()
    {
        int maxPlayers = /*online ? PhotonNetwork.CurrentRoom.MaxPlayers :*/ allPlayers.Count;
        int playerNumACopy, playerNumBCopy;
        List<PlayerMovementCMF> teamBPlayers = new List<PlayerMovementCMF>();
        List<PlayerMovementCMF> teamAPlayers = new List<PlayerMovementCMF>();
        //if (online)
        //{
        //    if (teamASpawns.Length == 0) Debug.LogError("Error: there must be at least one spawn for team A in online mode");
        //    if (teamBSpawns.Length == 0) Debug.LogError("Error: there must be at least one spawn for team B in online mode");
        //    playerSpawnInfoList = new PlayerSpawnInfo[maxPlayers];
        //    for (int i = 0; i < playerSpawnInfoList.Length; i++)
        //    {
        //        playerSpawnInfoList[i] = new PlayerSpawnInfo();
        //    }
        //    playerNumACopy = playerNumBCopy = playerNumACopy = playerNumTeamA = playerNumTeamB = maxPlayers / 2;
        //}
        //else
        //{
        for (int i = 0; i < maxPlayers; i++)
        {
            if (allPlayers[i].team == Team.A)
            {
                teamAPlayers.Add(allPlayers[i]);
            }
            else
            {
                teamBPlayers.Add(allPlayers[i]);
            }
        }
        playerNumACopy = playerNumTeamA = teamAPlayers.Count;
        playerNumBCopy = playerNumTeamB = teamBPlayers.Count;
        //}
        if (debugModeOn) Debug.Log("Blue players: " + playerNumACopy + "; Red Players: " + playerNumBCopy);

        //Divide number of players in a team by number of spawns in that team && set spawnRotation of players (because is more efficient to do it here)
        //this is an array where each position is each spawn in the respective team, and the int inside is the number of players of that team that spawn there.
        int[] teamASpawnsNumPlayers = new int[teamASpawns.Length];
        int[] teamBSpawnsNumPlayers = new int[teamBSpawns.Length];


        int playersPerSpawn = 0;
        //BLUE TEAM PLAYERS PER SPAWN
        if (teamASpawns.Length > 0 && playerNumTeamA > 0)
        {
            if (debugModeOn) Debug.Log("Blue Team Spawns Players:");
            //pps= Players Per Spawn not rounded
            float pps = (float)playerNumTeamA / (float)teamASpawns.Length;
            //we clamp the number so that is minimum 1 and then round it
            playersPerSpawn = Mathf.CeilToInt(Mathf.Clamp(pps, 1, float.MaxValue));
            //print("playerNumBlue = "+ playerNumBlue + "; blueTeamSpawns.Length = "+ blueTeamSpawns.Length + "; playersPerSpawn = " + pps + "; rounded number = "+playersPerSpawn);
            for (int i = 0; i < teamASpawns.Length && playerNumACopy > 0; i++)
            {
                teamASpawnsNumPlayers[i] = Mathf.Clamp(playerNumACopy, 0, playersPerSpawn);
                playerNumACopy -= teamASpawnsNumPlayers[i];
                //print("Respawn "+ i + ": " + blueSpawnsNumPlayers[i] + " players");
                //if (online)
                //{
                //    int auxTeamASpawnNumPlayers = teamASpawnsNumPlayers[i];
                //    for (int j = 0; j < playerSpawnInfoList.Length && auxTeamASpawnNumPlayers > 0; j++)
                //    {
                //        if (j % 2 == 0)//Team A
                //        {
                //            auxTeamASpawnNumPlayers--;
                //            playerSpawnInfoList[j].rotation = Quaternion.Euler(0, teamASpawns[i].rotation.eulerAngles.y, 0);
                //        }
                //    }
                //}
                //else
                //{
                for (int j = 0; j < teamASpawnsNumPlayers[i]; j++)
                {
                    teamAPlayers[0].mySpawnInfo.rotation = Quaternion.Euler(0, teamASpawns[i].rotation.eulerAngles.y, 0);
                    //print("SpawnRotation " + bluePlayers[0].gameObject.name + " = " + bluePlayers[0].spawnRotation.eulerAngles);
                    teamAPlayers.RemoveAt(0);
                }
                //}
            }
        }


        //RED TEAM PLAYERS PER SPAWN
        if (teamBSpawns.Length > 0 && playerNumTeamB > 0)
        {
            print("Red Team Spawns Players:");
            //pps= Players Per Spawn not rounded
            float pps = (float)playerNumTeamB / (float)teamBSpawns.Length;
            //we clamp the number so that is minimum 1 and then round it
            playersPerSpawn = Mathf.CeilToInt(Mathf.Clamp(pps, 1, float.MaxValue));
            for (int i = 0; i < teamBSpawns.Length && playerNumBCopy > 0; i++)
            {
                teamBSpawnsNumPlayers[i] = Mathf.Clamp(playerNumBCopy, 0, playersPerSpawn);
                playerNumBCopy -= teamBSpawnsNumPlayers[i];
                //print(i + ": " + redSpawnsNumPlayers[i] + " players");
                //if (online)
                //{
                //    int auxTeamBSpawnNumPlayers = teamBSpawnsNumPlayers[i];
                //    for (int j = 0; j < playerSpawnInfoList.Length && auxTeamBSpawnNumPlayers > 0; j++)
                //    {
                //        if (j % 2 != 0)//Team B
                //        {
                //            auxTeamBSpawnNumPlayers--;
                //            playerSpawnInfoList[j].rotation = Quaternion.Euler(0, teamBSpawns[i].rotation.eulerAngles.y, 0);
                //        }
                //    }
                //}
                //else
                //{
                for (int j = 0; j < teamBSpawnsNumPlayers[i]; j++)
                {
                    teamBPlayers[0].mySpawnInfo.rotation = Quaternion.Euler(0, teamBSpawns[i].rotation.eulerAngles.y, 0);
                    teamBPlayers.RemoveAt(0);
                }
                //}
            }
        }

        //ALL SPAWN POSITIONS CONCATENATED
        List<Vector3> spawnPosTeamA = new List<Vector3>();
        List<Vector3> spawnPosTeamB = new List<Vector3>();
        for (int i = 0; i < teamASpawns.Length && playerNumTeamA > 0; i++)
        {
            List<Vector3> auxPositions = teamASpawns[i].GetComponent<Respawn>().SetSpawnPositions(teamASpawnsNumPlayers[i]);
            spawnPosTeamA.AddRange(auxPositions);
        }

        for (int i = 0; i < teamBSpawns.Length && playerNumTeamB > 0; i++)
        {
            List<Vector3> auxPositions = teamBSpawns[i].GetComponent<Respawn>().SetSpawnPositions(teamBSpawnsNumPlayers[i]);
            spawnPosTeamB.AddRange(auxPositions);
        }
        if (spawnPosTeamA.Count != playerNumTeamA) Debug.LogError("Error: Spawn positions (" + spawnPosTeamA.Count + ") for blue team are not equal to number of blue team players(" + playerNumTeamA + ").");
        if (spawnPosTeamB.Count != playerNumTeamB) Debug.LogError("Error: Spawn positions (" + spawnPosTeamB.Count + ") for red team are not equal to number of red team players(" + playerNumTeamB + ").");
        //if (online)
        //{
        //    for (int i = 0; i < playerSpawnInfoList.Length; i++)
        //    {
        //        Vector3 pos;
        //        if (i % 2 == 0)
        //        {
        //            pos = spawnPosTeamA[0];
        //            spawnPosTeamA.RemoveAt(0);
        //        }
        //        else
        //        {
        //            pos = spawnPosTeamB[0];
        //            spawnPosTeamB.RemoveAt(0);
        //        }
        //        playerSpawnInfoList[i].position = pos;
        //    }
        //}
        //else
        //{
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].team == Team.A)
            {
                allPlayers[i].mySpawnInfo.position = spawnPosTeamA[0];
                spawnPosTeamA.RemoveAt(0);
            }
            else
            {
                allPlayers[i].mySpawnInfo.position = spawnPosTeamB[0];
                spawnPosTeamB.RemoveAt(0);
            }
        }
        //}
    }

    /// <summary>
    /// Inicia los canvas con las variables y tamaños necesarios para el numero de jugadores. 
    /// </summary>
    private void SetUpCanvases()//Para PantallaDividida
    {
        for (int i = 0; i < allCanvas.Count; i++)
        {
            allCanvas[i].GetComponent<PlayerHUDCMF>().AdaptCanvasHeightScale();
        }

        //for(int i = 0; playerNum >= 2 && i < contador.Count;i++)
        //{
        //    contador[i].anchoredPosition = new Vector3(contador[i].anchoredPosition.x, 100, contador[i].anchoredPosition.y);
        //}

        //if (playerNum == 2)
        //{
        //    contador[0].localScale /= scaleDos;
        //    contador[1].localScale /= scaleDos;

        //    powerUpPanel[0].localScale /= scaleDos;
        //    powerUpPanel[1].localScale /= scaleDos;
        //}
        //else if (playerNum == 3)
        //{
        //    contador[0].localScale /= scaleCuatro;
        //    contador[1].localScale /= scaleCuatro;
        //    contador[2].localScale /= scaleDos;

        //    powerUpPanel[0].localScale /= scaleCuatro;
        //    powerUpPanel[1].localScale /= scaleCuatro;
        //    powerUpPanel[2].localScale /= scaleDos;
        //}
        //else if (playerNum == 4)
        //{
        //    contador[0].localScale /= scaleCuatro;
        //    contador[1].localScale /= scaleCuatro;
        //    contador[2].localScale /= scaleCuatro;
        //    contador[3].localScale /= scaleCuatro;

        //    powerUpPanel[0].localScale /= scaleCuatro;
        //    powerUpPanel[1].localScale /= scaleCuatro;
        //    powerUpPanel[2].localScale /= scaleCuatro;
        //    powerUpPanel[3].localScale /= scaleCuatro;
        //}
    }

    //Eloy: this is for checking if the inputsBuffer array is well set up
    void CheckValidInputsBuffer()
    {
        List<PlayerInput> knownInputs = new List<PlayerInput>();
        for (int i = 0; i < allBufferedInputs.Length; i++)
        {
            PlayerInput auxInput = allBufferedInputs[i].inputType;
            bool found = false;
            for (int j = 0; j < knownInputs.Count && !found; j++)
            {
                //Debug.Log("j = "+j+"; knownInputs.Count = " + knownInputs.Count);
                //Debug.LogWarning("Checking if the input "+ auxInput + " is equal to the known input " + knownInputs[j]);
                if (auxInput == knownInputs[j])
                {
                    found = true;
                }
            }
            if (!found)
            {
                knownInputs.Add(auxInput);
            }
            else
            {
                Debug.LogError("Error: There is more than one BufferedInput of the type " + auxInput.ToString() + "in the inputsBuffer.");
            }
        }
    }
    #endregion

    #region --- MENU --- 
    private void SwitchGameOverMenu()
    {
        if (gameOverStarted)
        {
            myGameInterface.StartPressStartToContinue(winner != Team.none);
        }
    }

    public void PauseGame(PlayerActions p)
    {
        //if (!online)
        //{
        Time.timeScale = 0;
        myGameInterface.PauseGame();
        playerActions = p;
        gamePaused = true;
        GameInfo.instance.SetRenController(myRenCont);
        //}
    }

    public void UnPauseGame()
    {
        switch (slowmo)
        {
            case 0:
                Time.timeScale = 1;
                break;
            case 1:
                Time.timeScale = 0.25f;
                break;
            case 2:
                Time.timeScale = 0.075f;
                break;
            case 3:
                Time.timeScale = 0.025f;
                break;
        }
        myGameInterface.UnPauseGame();
        gamePaused = false;
        GameInfo.instance.ReturnToLastRenController();
    }

    #endregion

    #region --- GAME FLOW FUNCTIONS ---
    public virtual void StartGameOver(Team _winnerTeam = Team.none)
    {
        //print("GAME OVER");
        if (!gameOverStarted /*&& !online*/)
        {
            for (int i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].DoGameOver();
            }

            playing = false;
            gamePaused = true;
            gameOverStarted = true;
            winner = _winnerTeam;
            GameInfo.instance.SetRenController(myRenCont);
            myGameInterface.StartGameOver(winner);
        }
    }

    public void RespawnPlayer(PlayerMovementCMF player)
    {
        //print("RESPAWN PLAYER");
        player.SetVelocity(Vector3.zero);
        player.transform.position = player.mySpawnInfo.position;
        player.rotateObj.transform.rotation = player.mySpawnInfo.rotation;
        player.targetRotAngle = player.mySpawnInfo.rotation.eulerAngles.y;
        Debug.LogWarning("Respawn Player:  player.mySpawnInfo.position = " + player.mySpawnInfo.position + "; player.mySpawnInfo.rotation.y = " + player.mySpawnInfo.rotation.eulerAngles.y);
        //print("Player " + player.gameObject.name + " respawn rotation = " + player.spawnRotation.eulerAngles);

        //player.myCamera.KonoAwake();
        //player.myCamera.SwitchCamera(player.myCamera.camMode);
        //player.myCamera.LateUpdate();
        player.myCamera.InstantPositioning();
        player.myCamera.InstantRotation();
        //player.myCamera.transform.localRotation = player.rotateObj.transform.localRotation;
        //player.myCamera.SwitchCamera(player.myCamera.camMode);
        player.ResetPlayer();
        player.myPlayerAnimation.RestartAnimation();
    }

    public virtual void ResetGame()//Eloy: habrá que resetear muchas más cosas
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        /*playing = true;
        SwitchGameOverMenu();
        foreach (PlayerMovementCMF pM in allPlayers)
        {
            RespawnPlayer(pM);
            pM.ResetPlayer();
        }
        if (gamePaused)
        {
            UnPauseGame();
        }*/
    }

    void DeactivatePlayerComponents()
    {
        int count = playersParent.childCount;
        for (int i = 0; i < count; i++)
        {
            playersParent.GetChild(i).gameObject.SetActive(false);
        }
        count = playersCamerasParent.childCount;
        for (int i = 0; i < count; i++)
        {
            playersCamerasParent.GetChild(i).gameObject.SetActive(false);
        }
        //count = playersUICamerasParent.childCount;
        //for (int i = 0; i < count; i++)
        //{
        //    playersUICamerasParent.GetChild(i).gameObject.SetActive(false);
        //}
        count = playersCanvasParent.childCount;
        for (int i = 0; i < count; i++)
        {
            playersCanvasParent.GetChild(i).gameObject.SetActive(false);
        }
    }

    #endregion

    #region --- Ocean Renderer ---

    void StartOceanRendererViewpoint()
    {
        // Check for Crest Ocean
        if (myOceanRenderer != null && myOceanRenderer.isActiveAndEnabled)
        {
            if (MasterManager.GameSettings.online || (!MasterManager.GameSettings.online && playerNum == 1))
            {
                //Set Crest Ocean viewpoint for Ocean LOD
                myOceanRenderer.Viewpoint = allPlayers[0].transform;
            }
            else//offline and more than 1 player
            {
                if (myOceanRenderer.Viewpoint == null) Debug.LogError("GameControllerCMF -> There is no defaultViewpoint to move around");
                else
                {
                    Vector3[] points = new Vector3[playerNum];
                    for (int i = 0; i < playerNum; i++)
                    {
                        allCameraBases[i].myCamera.GetComponent<OceanPlanarReflection>().enabled = false;
                        points[i] = allPlayers[i].transform.position;
                    }
                    myOceanRenderer.Viewpoint.position = VectorMath.MiddlePoint(points);
                }
            }
        }
    }

    void UpdateOceanRendererViewpoint()
    {
        if (myOceanRenderer != null && myOceanRenderer.isActiveAndEnabled && myOceanRenderer.Viewpoint != null && !MasterManager.GameSettings.online && playerNum > 1)
        {
            Vector3[] points = new Vector3[playerNum];
            for (int i = 0; i < playerNum; i++)
            {
                points[i] = allPlayers[i].transform.position;
            }
            myOceanRenderer.Viewpoint.position = VectorMath.MiddlePoint(points);
        }
    }

    #endregion

    #region --- AUXILIAR ---
    void SlowMotion()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0) && slowMoButtonOn)
        {
            switch (slowmo)
            {
                case 0:
                    Time.timeScale = 0.25f;
                    slowmo = 1;
                    break;
                case 1:
                    Time.timeScale = 0.075f;
                    slowmo = 2;
                    break;
                case 2:
                    Time.timeScale = 0.025f;
                    slowmo = 3;
                    break;
                case 3:
                    Time.timeScale = 1;
                    slowmo = 0;
                    break;
            }
        }
    }

    private void SwitchLockMouse()
    {
        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    #endregion

    #endregion

    #region ----[ BOLT CALLBACKS ]----
    #endregion

    #region ----[ NETWORK FUNCTIONS ]----
    #endregion
}