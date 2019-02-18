﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Photon.Pun;

#region ----[ PUBLIC ENUMS ]----
public enum GameMode
{
    CaptureTheFlag,
    AirPump,
    Tutorial
}
#endregion

public class GameControllerBase : MonoBehaviourPunCallbacks
{

    #region ----[ VARIABLES FOR DESIGNERS ]----

    //referencias
    [Header(" --- Referencias --- ")]
    public GameInterface myGameInterface;
    //este parámetro es para poner slowmotion al juego (como estados: 0=normal,1=slowmo,2=slowestmo),
    // solo se debe usar para testeo, hay que QUITARLO para la build "comercial".
    [Header(" --- Variables generales ---")]
    public GameMode gameMode;
    int slowmo = 0;

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
    public List<WeaponData> allWeapons;//Array que contendrá las armas utilizadas, solo util en Pantalla Dividida, SIN USAR
    public BufferedInputData[] allBufferedInputs;

    [Header(" --- Spawn positions ---")]
    //Posiciones de los spawns
    public Transform blueTeamSpawn;
    public Transform redTeamSpawn;

    //Variables de HUDS
    [Header(" --- Players HUD --- ")]

    private List<RectTransform> contador;//Array que contiene todos los contadores de tiempo, solo util en Pantalla Dividida
    private List<RectTransform> powerUpPanel;//Array que contiene los objetos del dash y el hook en el HUD, solo util en Pantalla Dividida
    [Header(" --- Players HUD scale --- ")]
    public float scaleDos = 1.25f;//escala de las camaras para 2 jugadores
    public float scaleCuatro = 1.25f;//escala para 3 jugadores y 4 jugadores
    #endregion

    #region ----[ PROPERTIES ]----

    private PlayerActions playerActions;

    //GAME OVER MENU
    [HideInInspector]
    public bool gameOverStarted = false;

    //Player components lists
    protected List<PlayerMovement> allPlayers;//Array que contiene a los PlayerMovement
    protected List<CameraController> allCameraBases;//Array que contiene todas las cameras bases, solo util en Pantalla Dividida
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
    public bool online; //= PhotonNetwork.IsConnected; JUAN: No se puede inicializar el valor porque tira un error chungo, THX UNITY, está inicializado en el Awake

    // variables para nuestro jugador online
    [HideInInspector]
    PlayerMovement onlinePlayer;
    [HideInInspector]
    CameraController onlineCamera;
    [HideInInspector]
    GameObject onlineCanvas;
    [HideInInspector]
    Camera onlineUICamera;
    [HideInInspector]
    public PlayerActions BaseGameActions { get; set; }
    #endregion

    #region ----[ VARIABLES ]----



    #endregion

    #region ----[ MONOBEHAVIOUR FUNCTIONS ]----

    #region Awake
    protected virtual void Awake()
    {
        online = PhotonNetwork.IsConnected;
        if (online)
        {
            Debug.Log("GameControllerBase: estamos conectados y la base del game controller está funcionando correctamente");
        }
        else
        {
            //Esto es para no entrar en escenas cuando no tenemos los controles. Te devuelve a seleccion de equipo
            //Eloy: he cambiado esto porque me he dado cuenta de que es necesario hasta en la build final, no solo en el editor.
            if (GameInfo.instance == null || GameInfo.instance.inControlManager == null)
            {
                string escena = TeamSetupManager.siguienteEscena;
                //print(escena);
                TeamSetupManager.siguienteEscena = SceneManager.GetActiveScene().name;
                TeamSetupManager.startFromMap = true;
                SceneManager.LoadScene("TeamSetup");
                return;
            }
        }


        //initialize lists
        allPlayers = new List<PlayerMovement>();
        allCameraBases = new List<CameraController>();
        allCanvas = new List<GameObject>();
        allUICameras = new List<Camera>();

        contador = new List<RectTransform>();
        powerUpPanel = new List<RectTransform>();

        CheckValidInputsBuffer();

        if (!online)
        {
            playerNum = GameInfo.instance.nPlayers;
            playerNum = Mathf.Clamp(playerNum, 1, 4);
            for (int i = 0; i < playerNum; i++)
            {
                CreatePlayer(""+(i+1));
            }

            //AUTOMATIC PLAYERS & CAMERAS/CANVAS SETUP
            PlayersSetup();
            SetUpCanvases();
            AllAwakes();
        }
        else //Eloy: para Juan: aqui inicia al host! playerNum deberia estar a 0 y luego ponerse a 1 cuando se crea el jugador
        {
            //CreatePlayer
            //PlayerSetup
            //PlayerSetupOnline?
            //No hace falta SetUpCanvas creo
            //Haz los awakes, y haz el awake de cada jugador nuevo(esto ultimo hay que buscar donde ponerlo... en el CreatePlayer?
            int playernumber = PhotonNetwork.CurrentRoom.PlayerCount;
            CreatePlayer(playernumber.ToString());
            OnlineAwake();
            PlayersSetup();
            //OnlinePlayerSetup();
            //OnlineCanvasSetUp();
            //OnlineAwakePlayer();
        }
    }
 
    #endregion

    #region Start
    protected virtual void Start()
    {
        if (!online)
        {
            StartPlayers();
            StartGame();
            Debug.Log("GameController Start terminado");
        }
        else
        {
            onlinePlayer.KonoStart();
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                StartGame();
                Debug.Log("GameControllerBase: Empezamos el juego pues se han unido todos los jugadores");
            }
        }
    }

    //Funcion que llama al Start de los jugadores. Eloy: Juan, ¿solo pantalla dividida?, JUAN: Sí Eloy, sólo pantalla dividida.
    void StartPlayers()
    {
        for (int i = 0; i < playerNum; i++)
        {
            allPlayers[i].KonoStart();
        }
    }

    //Funcion que se llama al comenzar la partida, que inicicia las variables necesarias, y que posiciona a los jugadores y ¿bandera?
    public virtual void StartGame()
    {
        playing = true;
        gamePaused = false;
        for (int i = 0; i < playerNum; i++)
        {
            RespawnPlayer(allPlayers[i]);
        }
    }
    #endregion

    #region Update
    void Update()
    {
        //if (scoreManager.End) return;
        SlowMotion();

        if (!gamePaused)
        {

            if (playing)
            {
                UpdatePlayers();
                UpdateModeExclusiveClasses();
            }
        }
        else
        {
            if (playing)
            {
                if (playerActions.Attack3.WasPressed || playerActions.Options.WasPressed)
                {
                    UnPauseGame();
                }
            }
            else
            {
                if (gameOverStarted && !myGameInterface.gameOverMenuOn)
                {
                    for (int i = 0; i < playerNum; i++)
                    {
                        if (allPlayers[i].Actions.Options.WasPressed)
                        {
                            SwitchGameOverMenu();
                            i = playerNum;//BREAK
                        }
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < playerNum; i++)
        {
            allPlayers[i].KonoUpdate();
        }
    }

    protected virtual void UpdateModeExclusiveClasses()//no borrar, es para los hijos
    {
    }
    #endregion

    #endregion

    #region ----[ CLASS FUNCTIONS ]----

    #region AWAKE AND CREATE PLAYERS
    /// <summary>
    /// Funcion que Inicializa valores de todos los jugadores y sus cámaras.
    /// </summary>
    void PlayersSetup()
    {
        if (online)
        {
            onlinePlayer.Actions = this.BaseGameActions; //Juan: hay que hacer la toma de valores de TeamSetupManager aquí pero bueh...
            onlineCamera.myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
            onlineUICamera.rect = new Rect(0, 0, 1, 1);
        }
        else
        {
            for (int i = 0; i < allPlayers.Count; i++)
            {
                if (i < playerNum)
                {
                    //LE DAMOS AL JUGADOR SUS CONTROLES (Mando/teclado) y SU EQUIPO
                    allPlayers[i].Actions = GameInfo.instance.playerActionsList[i];

                    if (GameInfo.instance.playerTeamList[i] == Team.none)
                    {
                        GameInfo.instance.playerTeamList[i] = GameInfo.instance.NoneTeamSelect();
                    }

                    allPlayers[i].team = GameInfo.instance.playerTeamList[i];
                }
            }
            //SETUP CAMERAS
            switch (playerNum)
            {
                case 1:
                    allCameraBases[0].myCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
                    allUICameras[0].rect = new Rect(0, 0, 1, 1);
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

            SetSpawnPositions();//Eloy: Para Juan: Esto tendrás que usarlo en online también supongo... JUAN: nup, con el photonNetwork.Instantiate ya les coloco en los spawns de entrada
        }
    }

    protected virtual void AllAwakes()
    {
        for (int i = 0; i < allCameraBases.Count; i++)
        {
            myGameInterface.KonoAwake(this);
            allPlayers[i].KonoAwake();
            allCameraBases[i].KonoAwake();
        }
    }

    protected virtual void OnlineAwake()
    {
        myGameInterface.KonoAwake(this);
        onlinePlayer.KonoAwake();
        onlineCamera.KonoAwake();
    }

    //Juan: esta función se ejecuta antes de instanciar al jugador, PhotonNetwrok.Instantiate  así spawneará al jugador en su respectivo lugar desde el principio haya o no comenzado el juego
    public Team SetOnlineTeam()
    {
        Team myTeam = Team.blue;
        int playercount = (int)PhotonNetwork.CurrentRoom.PlayerCount;
        if (playercount % 2 != 0) //Juan: Pares al azul impares al rojo
        {
            myTeam = Team.red;
        }
        return myTeam;
    }

    public virtual void CreatePlayer(string playerNumber)
    {
        PlayerMovement newPlayer;
        GameObject newPlayerCanvas;
        CameraController newPlayerCamera;
        Camera newPlayerUICamera;

        if (online)
        {
            if (playerPrefab == null)
            {
                Debug.Log("GamerControllerBase: Color=Red><a>Missing playerPrefab Reference in GameController</a></Color>");
            }
            else
            {
                Debug.Log("GameControllerBase: Instantiating player over the network");
                //JUAN: WARNING!!, el objeto que se instancie debe estar siempre en la carpeta de Resources de Photon, o ir al método de instantiate para cambiarlo
                //JUAN: Eloy, donde dice Vector3 y Quartenion debe ser para establecer la posición del spawn del jugador, para hacer las pruebas lo dejo to random pero hay que mirarlo
                if (PlayerMovement.LocalPlayerInstance == null)
                {
                    if (online)
                    {
                        Debug.LogFormat("GameControllerBase: We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                        // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                        Team newPlayerTeam = SetOnlineTeam();
                        Vector3 respawn = new Vector3(-200, -200, -200);
                        if (newPlayerTeam == Team.blue)
                        {
                            respawn = blueTeamSpawn.position;
                        }
                        else if (newPlayerTeam == Team.red)
                        {
                            respawn = redTeamSpawn.position;
                        }
                        newPlayer = PhotonNetwork.Instantiate(this.playerPrefab.name, respawn, Quaternion.identity, 0).GetComponent<PlayerMovement>();

                        newPlayerCanvas = Instantiate(playerCanvasPrefab, playersCanvasParent);
                        newPlayerCamera = Instantiate(playerCameraPrefab, playersCamerasParent).GetComponent<CameraController>();
                        newPlayerUICamera = Instantiate(playerUICameraPrefab, playersUICamerasParent).GetComponent<Camera>();

                        //nombrado de objetos nuevos
                        newPlayer.gameObject.name = "Player " + playerNumber;
                        newPlayerCanvas.gameObject.name = "Canvas " + playerNumber;
                        newPlayerCamera.gameObject.name = "CameraBase " + playerNumber;
                        newPlayerUICamera.gameObject.name = "UICamera " + playerNumber;

                        //Inicializar referencias
                        //Player
                        newPlayer.gC = this;
                        newPlayer.myCamera = newPlayerCamera;
                        newPlayer.myPlayerHUD = newPlayerCanvas.GetComponent<PlayerHUD>();
                        newPlayer.myUICamera = newPlayerUICamera;
                        newPlayer.myPlayerCombat.attackNameText = newPlayerCanvas.GetComponent<PlayerHUD>().attackNameText;
                        //Canvas
                        newPlayerCanvas.GetComponent<PlayerHUD>().gC = this;
                        newPlayerCanvas.GetComponent<Canvas>().worldCamera = newPlayerUICamera;
                        //CameraBase
                        newPlayerCamera.myPlayerMov = newPlayer;
                        newPlayerCamera.myPlayer = newPlayer.transform;
                        newPlayerCamera.cameraFollowObj = newPlayer.cameraFollow;

                        //Añadir a los arrays todos los componentes del jugador
                        //guarda jugador
                        allPlayers.Add(newPlayer);
                        allCanvas.Add(newPlayerCanvas);
                        allCameraBases.Add(newPlayerCamera);
                        allUICameras.Add(newPlayerUICamera);
                        contador.Add(newPlayerCanvas.GetComponent<PlayerHUD>().contador);
                        powerUpPanel.Add(newPlayerCanvas.GetComponent<PlayerHUD>().powerUpPanel);

                        onlinePlayer = newPlayer;
                        onlineCamera = newPlayerCamera;
                        onlineCanvas = newPlayerCanvas;
                        onlineUICamera = newPlayerUICamera;
                    }
                }
                else
                {
                    Debug.Log("GameControllerBase: Ignoring CreatePlayer() call because we already exist");
                }
            }
        }
        else
        {
            newPlayer = Instantiate(playerPrefab, playersParent).GetComponent<PlayerMovement>();
            newPlayerCanvas = Instantiate(playerCanvasPrefab, playersCanvasParent);
            newPlayerCamera = Instantiate(playerCameraPrefab, playersCamerasParent).GetComponent<CameraController>();
            newPlayerUICamera = Instantiate(playerUICameraPrefab, playersUICamerasParent).GetComponent<Camera>();

            //nombrado de objetos nuevos
            newPlayer.gameObject.name = "Player " + playerNumber;
            newPlayerCanvas.gameObject.name = "Canvas " + playerNumber;
            newPlayerCamera.gameObject.name = "CameraBase " + playerNumber;
            newPlayerUICamera.gameObject.name = "UICamera " + playerNumber;

            //Inicializar referencias
            //Player
            newPlayer.gC = this;
            newPlayer.myCamera = newPlayerCamera;
            newPlayer.myPlayerHUD = newPlayerCanvas.GetComponent<PlayerHUD>();
            newPlayer.myUICamera = newPlayerUICamera;
            newPlayer.myPlayerCombat.attackNameText = newPlayerCanvas.GetComponent<PlayerHUD>().attackNameText;
            //Canvas
            newPlayerCanvas.GetComponent<PlayerHUD>().gC = this;
            newPlayerCanvas.GetComponent<Canvas>().worldCamera = newPlayerUICamera;
            //CameraBase
            newPlayerCamera.myPlayerMov = newPlayer;
            newPlayerCamera.myPlayer = newPlayer.transform;
            newPlayerCamera.cameraFollowObj = newPlayer.cameraFollow;
                
            //Añadir a los arrays todos los componentes del jugador
            //guarda jugador
            allPlayers.Add(newPlayer);
            allCanvas.Add(newPlayerCanvas);
            allCameraBases.Add(newPlayerCamera);
            allUICameras.Add(newPlayerUICamera);
            contador.Add(newPlayerCanvas.GetComponent<PlayerHUD>().contador);
            powerUpPanel.Add(newPlayerCanvas.GetComponent<PlayerHUD>().powerUpPanel);
        }
    }

    //actualmente en desuso
    public virtual void RemovePlayer(PlayerMovement _pM)//solo para online
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

    /// <summary>
    /// Calcula las posiciones de spawn dentro de cada spawn (rojo y azul), de manera equidistante y centrada y se las da a los Players.
    /// </summary>
    void SetSpawnPositions()
    {
        int playerNumBlue = 0, playerNumRed = 0;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].team == Team.blue)
            {
                playerNumBlue++;
            }
            else
            {
                playerNumRed++;
            }
        }
        List<Vector3> spawnPosBlue = blueTeamSpawn.GetComponent<Respawn>().SetSpawnPositions(playerNumBlue);
        List<Vector3> spawnPosRed = redTeamSpawn.GetComponent<Respawn>().SetSpawnPositions(playerNumRed);
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].team == Team.blue)
            {
                allPlayers[i].spawnPosition = spawnPosBlue[0];
                spawnPosBlue.RemoveAt(0);
            }
            else
            {
                allPlayers[i].spawnPosition = spawnPosRed[0];
                spawnPosRed.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Inicia los canvas con las variables y tamaños necesarios para el numero de jugadores. 
    /// </summary>
    private void SetUpCanvases()//Para PantallaDividida
    {
        if (!online)
        {
            for(int i = 0; playerNum >= 2 && i < contador.Count;i++)
            {
                contador[i].anchoredPosition = new Vector3(contador[i].anchoredPosition.x, 100, contador[i].anchoredPosition.y);
            }

            if (playerNum == 2)
            {
                contador[0].localScale /= scaleDos;
                contador[1].localScale /= scaleDos;

                powerUpPanel[0].localScale /= scaleDos;
                powerUpPanel[1].localScale /= scaleDos;
            }
            else if (playerNum == 3)
            {
                contador[0].localScale /= scaleCuatro;
                contador[1].localScale /= scaleCuatro;
                contador[2].localScale /= scaleDos;

                powerUpPanel[0].localScale /= scaleCuatro;
                powerUpPanel[1].localScale /= scaleCuatro;
                powerUpPanel[2].localScale /= scaleDos;
            }
            else if (playerNum == 4)
            {
                contador[0].localScale /= scaleCuatro;
                contador[1].localScale /= scaleCuatro;
                contador[2].localScale /= scaleCuatro;
                contador[3].localScale /= scaleCuatro;

                powerUpPanel[0].localScale /= scaleCuatro;
                powerUpPanel[1].localScale /= scaleCuatro;
                powerUpPanel[2].localScale /= scaleCuatro;
                powerUpPanel[3].localScale /= scaleCuatro;
            }
        }
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
                if (auxInput == knownInputs[i])
                {
                    found = true;
                }
            }
            if (found)
            {
                Debug.LogError("Error: There is more than one BufferedInput of the type " + auxInput.ToString() + "in the inputsBuffer.");
            }
            else
            {
                knownInputs.Add(auxInput);
            }
        }
    }

    #endregion

    void SlowMotion()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            if (slowmo == 0)
            {
                Time.timeScale = 0.25f;
                slowmo = 1;
            }
            else if (slowmo == 1)
            {
                Time.timeScale = 0.075f;
                slowmo = 2;
            }
            else if (slowmo == 2)
            {
                Time.timeScale = 1;
                slowmo = 0;
            }
        }
    }

    public virtual void StartGameOver(Team _winnerTeam)
    {
        //print("GAME OVER");
        if (!gameOverStarted)
        {
            playing = false;
            gamePaused = true;
            gameOverStarted = true;
            myGameInterface.StartGameOver(_winnerTeam);
        }
    }

    public void RespawnPlayer(PlayerMovement player)
    {
        //print("RESPAWN PLAYER");
        player.SetVelocity(Vector3.zero);
        player.transform.position = player.spawnPosition;
        switch (player.team)
        {
            case Team.blue:              
                player.rotateObj.transform.localRotation = Quaternion.Euler(0, blueTeamSpawn.rotation.eulerAngles.y, 0);
                break;
            case Team.red:
                player.rotateObj.transform.localRotation = Quaternion.Euler(0, redTeamSpawn.rotation.eulerAngles.y, 0);
                break;
        }
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

    private void SwitchGameOverMenu()
    {
        if (gameOverStarted)
        {
            myGameInterface.SwitchGameOverMenu();
        }
    }

    public void PauseGame(PlayerActions p)
    {
        print("PARO EL TIEMPO AQUÍ");
        Time.timeScale = 0;
        myGameInterface.PauseGame();
        playerActions = p;
        gamePaused = true;

    }

    public void UnPauseGame()
    {
        Time.timeScale = 1;
        myGameInterface.UnPauseGame();
        gamePaused = false;
    }

    public virtual void ResetGame()//Eloy: habrá que resetear muchas más cosas
    {
        playing = true;
        SwitchGameOverMenu();
        foreach (PlayerMovement pM in allPlayers)
        {
            pM.Die();
        }
        if (gamePaused)
        {
            UnPauseGame();
        }
    }

    #endregion

    #region ----[ PUN CALLBACKS ]----
    #endregion

    #region ----[ RPC ]----
    #endregion

    #region ----[ NETWORK FUNCTIONS ]----
    #endregion

    #region ----[ IPUNOBSERVABLE ]----
    #endregion
}

#region Struct
#endregion