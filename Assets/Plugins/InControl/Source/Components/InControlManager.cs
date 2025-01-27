namespace InControl
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
#if NETFX_CORE
	using System.Reflection;
#endif
#if UNITY_5_4_OR_NEWER
	using UnityEngine.SceneManagement;


#endif


	public class InControlManager : SingletonMonoBehavior<InControlManager>
	{
		public bool logDebugInfo = true;
		public bool invertYAxis = false;
		public bool useFixedUpdate = false;
		public bool dontDestroyOnLoad = true;
		public bool suspendInBackground = false;

		public bool enableICade = false;

		public bool enableXInput = false;
		public bool xInputOverrideUpdateRate = false;
		public int xInputUpdateRate = 0;
		public bool xInputOverrideBufferSize = false;
		public int xInputBufferSize = 0;

		public bool enableNativeInput = true;
		public bool nativeInputEnableXInput = true;
		public bool nativeInputPreventSleep = false;
		public bool nativeInputOverrideUpdateRate = false;
		public int nativeInputUpdateRate = 0;

		public List<string> customProfiles = new List<string>();


		public void OnEnable()
		{
			if (EnforceSingleton)
			{
				return;
			}

			InputManager.InvertYAxis = invertYAxis;
			InputManager.SuspendInBackground = suspendInBackground;
			InputManager.EnableICade = enableICade;

			InputManager.EnableXInput = enableXInput;
			InputManager.XInputUpdateRate = (uint) Mathf.Max( xInputUpdateRate, 0 );
			InputManager.XInputBufferSize = (uint) Mathf.Max( xInputBufferSize, 0 );

			InputManager.EnableNativeInput = enableNativeInput;
			InputManager.NativeInputEnableXInput = nativeInputEnableXInput;
			InputManager.NativeInputUpdateRate = (uint) Mathf.Max( nativeInputUpdateRate, 0 );
			InputManager.NativeInputPreventSleep = nativeInputPreventSleep;

			if (InputManager.SetupInternal())
			{
				if (logDebugInfo)
				{
					Debug.Log( "InControl (version " + InputManager.Version + ")" );
					Logger.OnLogMessage -= LogMessage;
					Logger.OnLogMessage += LogMessage;
				}

				foreach (var className in customProfiles)
				{
					var classType = Type.GetType( className );
					if (classType == null)
					{
						Debug.LogError( "Cannot find class for custom profile: " + className );
					}
					else
					{
						var customProfileInstance = Activator.CreateInstance( classType ) as UnityInputDeviceProfileBase;
						if (customProfileInstance != null)
						{
							InputManager.AttachDevice( new UnityInputDevice( customProfileInstance ) );
						}
					}
				}
			}

#if UNITY_5_4_OR_NEWER
			SceneManager.sceneLoaded -= OnSceneWasLoaded;
			SceneManager.sceneLoaded += OnSceneWasLoaded;
#endif

			if (dontDestroyOnLoad)
			{
				DontDestroyOnLoad( this );
			}
		}


		void OnDisable()
		{
			if (IsNotTheSingleton) return;
#if UNITY_5_4_OR_NEWER
			SceneManager.sceneLoaded -= OnSceneWasLoaded;
#endif
			InputManager.ResetInternal();
		}


#if UNITY_ANDROID && INCONTROL_OUYA && !UNITY_EDITOR
		void Start()
		{
			if (IsNotTheSingleton) return;
			StartCoroutine( CheckForOuyaEverywhereSupport() );
		}


		IEnumerator CheckForOuyaEverywhereSupport()
		{
			Debug.Log( "[InControl] Checking for OUYA Everywhere support..." );

			while (!OuyaSDK.isIAPInitComplete())
			{
				yield return null;
			}

			Debug.Log( "[InControl] OUYA SDK IAP initialization has completed." );

			OuyaEverywhereDeviceManager.Enable();
		}
#endif


		void Update()
		{
			if (IsNotTheSingleton) return;
			if (!useFixedUpdate || Utility.IsZero( Time.timeScale ))
			{
				InputManager.UpdateInternal();
			}
		}


		void FixedUpdate()
		{
			if (IsNotTheSingleton) return;
			if (useFixedUpdate)
			{
				InputManager.UpdateInternal();
			}
		}


		void OnApplicationFocus( bool focusState )
		{
			if (IsNotTheSingleton) return;
			InputManager.OnApplicationFocus( focusState );
		}


		void OnApplicationPause( bool pauseState )
		{
			if (IsNotTheSingleton) return;
			InputManager.OnApplicationPause( pauseState );
		}


		void OnApplicationQuit()
		{
			if (IsNotTheSingleton) return;
			InputManager.OnApplicationQuit();
		}


#if UNITY_5_4_OR_NEWER
		void OnSceneWasLoaded( Scene scene, LoadSceneMode loadSceneMode )
		{
			if (IsNotTheSingleton) return;
			InputManager.OnLevelWasLoaded();
		}
#else
		void OnLevelWasLoaded( int level )
		{
			if (IsNotTheSingleton) return;
			InputManager.OnLevelWasLoaded();
		}
#endif


		static void LogMessage( LogMessage logMessage )
		{
			switch (logMessage.type)
			{
				case LogMessageType.Info:
					Debug.Log( logMessage.text );
					break;
				case LogMessageType.Warning:
					Debug.LogWarning( logMessage.text );
					break;
				case LogMessageType.Error:
					Debug.LogError( logMessage.text );
					break;
				default:
					break;
			}
		}
	}
}
