﻿using System.Collections.Generic;
using UnityEngine;
using InControl;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

///Juan:
///PhotonAnimatorView and Triggers
///If you use Trigger parameters in animator controllers and want to synchronize them using "PhotonAnimatorView", it's important to consider how this is handled to avoid issues.
///
///·Due to the nature of the Trigger(s), it is only enabled when the animation event starts and disabled immediately before the next Update()
///·Components on a GameObject are executed in the same order as they are declared
///·Editing the Order of Execution Settings will affect execution order on the GameObject's components
///
///It essential that the "PhotonAnimatorView" component is executed after the code that raises the Trigger(s). So it's safer to put it at the bottom of the stack, or at least
///below the component(s) that will be responsible for raising the Trigger(s) using Animator.SetTrigger(...).
///
///The "PhotonAnimatorView" Inspector shows the various paramaters current values.A good way to check even before publishing is that the Trigger is properly raised to true
///when it should.If you don't see it happening, chances are this particular Trigger won't be synchronized over the network.

public class TeamSetupManager : MonoBehaviour
{
    //public static List<PlayerActions> playerActionsList = new List<PlayerActions>();
    public static TeamSetupManager instance;
    public GameObject stockGameInfo;
    public GameObject stockInControlManager;

    [Range(1,6)]
    public int minNumPlayers;
	public GameObject playerPrefab;
    public GameObject CameraGhostBuster;

	public static string siguienteEscena;
    public static bool startFromMap = false;
    public string nextScene;
	private bool ready = false;

	const int maxPlayers = 4;

	public List<Transform> playerPositions = new List<Transform>();

	List<PlayerSelected> players = new List<PlayerSelected>( maxPlayers);

	public PlayerSelecionUI[] pUI;

	PlayerActions keyboardListener;
	PlayerActions joystickListener;

	public float tiempoParaReady = 10.0f;

	[Header("Referencias")]
	public GameObject readyButton;

    public Camera myCamera;
    public VideoPlayer video;
    bool loadingScreenstarted = false;
    float loadingScreenTime;

    private void Awake()
    {
        if(GameInfo.instance == null)
        {
            stockInControlManager.SetActive(true);
            stockInControlManager.GetComponent<InControlManager>().OnEnable();
            stockGameInfo.SetActive(true);
            stockGameInfo.GetComponent<GameInfo>().Awake();

        }
        instance = this;
        minNumPlayers = Mathf.Clamp(minNumPlayers, 1, 6);
        //We use the GameInfo.instance.inControlManager value to know if we have controls or not set, and therefore we will allow to play the game directly or we will load the TeamSetup scene.
        if (GameInfo.instance != null)
        {
            GameInfo.instance.inControlManager = GameObject.Find("InControl manager");
        }
        else
        {
            Debug.LogError("Error: GameInfo is null or has not done it's awake yet. It should be awaken and ready.");
        }

    }

    void OnEnable()
	{
		InputManager.OnDeviceDetached += OnDeviceDetached;
		keyboardListener = PlayerActions.CreateWithKeyboardBindings();
		joystickListener = PlayerActions.CreateWithJoystickBindings();
	}


	void OnDisable()
	{
		InputManager.OnDeviceDetached -= OnDeviceDetached;
		joystickListener.Destroy();
		keyboardListener.Destroy();
	}

	//private float contador = 0;
	void Update()
	{
        ProgressLoadingScreen();
        ProgressReady();    

		if (JoinButtonWasPressedOnListener( joystickListener ))
		{
			InputDevice inputDevice = InputManager.ActiveDevice;

			if (ThereIsNoPlayerUsingJoystick( inputDevice ))
			{
				CreatePlayer( inputDevice );
			}
		}

		if (JoinButtonWasPressedOnListener( keyboardListener ))
		{
			if (ThereIsNoPlayerUsingKeyboard())
			{
				CreatePlayer( null );
			}
		}

		if (players.Count >= minNumPlayers){
			int contador = 0;
			foreach(PlayerSelected ps in players){
				if (ps.Ready){
					contador++;
				}
			}
			if (contador == players.Count)
            {
				//GameInfo.playerActionsList = new PlayerActions[players.Count];
				//GameInfo.playerActionsList = new List<PlayerActions>();
				//for(int i = 0; i < players.Count; i++){
				foreach(PlayerSelected ps in players)
                {
					//GameInfo.playerActionsList[i] = players[i].Actions;
					GameInfo.instance.playerActionsList.Add(ps.Actions);
					GameInfo.instance.playerTeamList.Add(ps.team);
					//Debug.Log(ps.Actions);
				}
                GameInfo.instance.nPlayers = players.Count;
				if (siguienteEscena != "Tutorial")
                {
                    //animator.SetBool("Ready", true);
                    if (!loadingScreenstarted)
                    {
                        StartLoadingScreen();
                    }
                }
				else
                {
					if (startFromMap)
					{
						SceneManager.LoadScene(siguienteEscena);
					}
					else
					{
						SceneManager.LoadScene(nextScene);
					}
				}
			}
		}


    }

    void StartLoadingScreen()
    {
        loadingScreenstarted = true;
        loadingScreenTime = 0;
        video.enabled = true;
        myCamera.cullingMask = LayerMask.GetMask("UI");
        HideTeamSelectUI();
        
    }

    void ProgressLoadingScreen()
    {
        if (loadingScreenstarted)
        {
            loadingScreenTime += Time.deltaTime;
            if (loadingScreenTime >= tiempoParaReady)
            {
                StopLoadingScreen();
            }
        }
    }

    void StopLoadingScreen()
    {
        loadingScreenstarted = false;
        //appear button
        readyButton.SetActive(true);
        ready = true;
    }

    void HideTeamSelectUI()
    {
        for(int i = 0; i < pUI.Length; i++)
        {
            pUI[i].panel.SetActive(false);
        }

    }

    void ProgressReady()
    {
        if (ready)
        {
            foreach (PlayerSelected ps in players)
            {
                if (ps.Actions.A.WasPressed)
                {
                    Debug.Log("LOAD GAME ");
                    if (startFromMap)
                    {
                        Debug.Log("Start from map: " + siguienteEscena);
                        SceneManager.LoadScene(siguienteEscena);
                    }
                    else
                    {
                        Debug.Log("Start from menu");
                        SceneManager.LoadScene(nextScene);
                    }
                }
            }
            return;
        }
    }

    bool JoinButtonWasPressedOnListener( PlayerActions actions )
	{
        return  actions.Y.WasPressed || actions.B.WasPressed || (actions != keyboardListener && (actions.A.WasPressed || actions.X.WasPressed)) ||
            (actions == keyboardListener && (Input.GetKeyDown(KeyCode.Alpha1)));
	}

	PlayerSelected FindPlayerUsingJoystick( InputDevice inputDevice )
	{
		int playerCount = players.Count;
		for (int i = 0; i < playerCount; i++)
		{
			PlayerSelected player = players[i];
			if (player.Actions.Device == inputDevice)
			{
				return player;
			}
		}

		return null;
	}

	bool ThereIsNoPlayerUsingJoystick( InputDevice inputDevice )
	{
		return FindPlayerUsingJoystick( inputDevice ) == null;
	}

	PlayerSelected FindPlayerUsingKeyboard()
	{
		int playerCount = players.Count;
		for (int i = 0; i < playerCount; i++)
		{
			PlayerSelected player = players[i];
			if (player.Actions == keyboardListener)
			{
				return player;
			}
		}

		return null;
	}

	bool ThereIsNoPlayerUsingKeyboard()
	{
		return FindPlayerUsingKeyboard() == null;
	}
 
	void OnDeviceDetached( InputDevice inputDevice )
	{
		PlayerSelected player = FindPlayerUsingJoystick( inputDevice );
		if (player != null)
		{
			RemovePlayer( player );
		}
	}

	PlayerSelected CreatePlayer( InputDevice inputDevice )
	{
		if (players.Count < maxPlayers)
		{
			// Pop a position off the list. We'll add it back if the player is removed.
			Vector3 playerPosition = playerPositions[0].position;
			playerPositions.RemoveAt( 0 );

			GameObject gameObject = Instantiate( playerPrefab, playerPosition, Quaternion.identity );
			PlayerSelected player = gameObject.GetComponent<PlayerSelected>();

			if (inputDevice == null)
			{
                // We could create a new instance, but might as well reuse the one we have
                // and it lets us easily find the keyboard player.

                //GameInfo.instance.playerActionsList.Add(keyboardListener);
                //GameInfo.instance.playerActionUno = keyboardListener;
                player.Actions = PlayerActions.CreateWithKeyboardBindings();
			}
			else
			{
				// Create a new instance and specifically set it to listen to the
				// given input device (joystick).
				PlayerActions actions = PlayerActions.CreateWithJoystickBindings();
				actions.Device = inputDevice;

                //GameInfo.instance.playerActionsList.Add(actions);
                player.Actions = actions;
                player.isAReleased = false;
            }

			players.Add( player );
			player.playerSelecionUI = pUI[players.Count - 1];
			pUI[players.Count - 1].panel.SetActive(true);

			return player;
		}

		return null;
	}

	void RemovePlayer( PlayerSelected player )
	{
		playerPositions.Insert( 0, player.transform );
		players.Remove( player );
		player.Actions = null;
		Destroy( player.gameObject );
	}


	//void OnGUI()
	//{
	//	const float h = 22.0f;
	//	float y = 10.0f;
//
	//	GUI.Label( new Rect( 10, y, 300, y + h ), "Active players: " + players.Count + "/" + maxPlayers );
	//	y += h;
//
	//	if (players.Count < maxPlayers)
	//	{
	//		GUI.Label( new Rect( 10, y, 300, y + h ), "Press a button or a/s/d/f key to join!" );
	//		y += h;
	//	}
	//}
}
[System.Serializable]
public struct PlayerSelecionUI
{
	public GameObject panel;
	public Image TeamSelect;
	public Image FlechaDerecha;
	public Image FlechaIzquierda;
	public TextMeshProUGUI AcctionsText;
}
