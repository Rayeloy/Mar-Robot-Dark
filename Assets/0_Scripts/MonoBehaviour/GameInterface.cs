﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

//SE DEDICA EXCLUSIVAMENTE A CONTROLAR LA INTERFAZ DENTRO DE LA PARTIDA, es decir, todo lo que hay dentro del Canvas general principalmente.
public class GameInterface : MonoBehaviour
{

    [Header("References")]

    private GameControllerBase gC;

    //GAME OVER MENU
    [Header(" --- Game Over Menu --- ")]
    public GameObject gameOverMenu;
    [HideInInspector]
    public bool gameOverMenuOn = false;
    public GameObject veil;
    public Image victoryRed;
    public Image victoryBlue;
    public UIAnimation victoryImageReduceAnimation;
    public UIAnimation victoryImageMoveUpAnimation;
    public Text gameOverPressStart;
    public GameObject gameOverFirstButton;//intento fallido de controlar qué boton se selecciona automáticamente al iniciar el menu de Game Over
    private string sceneLoadedOnReset;

    [Header(" --- Pause --- ")]
    public string menuScene;
    public string teamSelectScene;
    public GameObject pauseRestartButton;
    public GameObject pauseMenuButton;


    public void KonoAwake(GameControllerBase _gC)
    {
        gC = _gC;

        sceneLoadedOnReset = SceneManager.GetActiveScene().name;

        gameOverMenu.SetActive(false);
        gameOverPressStart.enabled = false;
        veil.SetActive(false);
        pauseRestartButton.SetActive(false);
        pauseMenuButton.SetActive(false);

        victoryRed.gameObject.SetActive(false);
        victoryBlue.gameObject.SetActive(false);

        gameOverMenuOn = false;

    }
    private void Update()
    {
        ProcessPressStartToContinue();
    }

    bool pressStartToContinueStarted = false;
    public void StartPressStartToContinue()
    {
        if (!pressStartToContinueStarted)
        {
            pressStartToContinueStarted = true;
            GameInfo.instance.StartAnimation(victoryImageReduceAnimation, null);
            victoryImageReduceAnimation.StartAnimation();
        }
    }

    bool moveUpAnimStarted = false;
    public void ProcessPressStartToContinue()
    {
        if (pressStartToContinueStarted)
        {
            if(!victoryImageReduceAnimation.playing & !moveUpAnimStarted)
            {
                Debug.Log("VICTORY ANIMATION MOVEUP ANIMATION STARTED");
                moveUpAnimStarted = true;
                GameInfo.instance.StartAnimation(victoryImageMoveUpAnimation, null);
            }
            else if (moveUpAnimStarted && !victoryImageMoveUpAnimation.playing)
            {
                StopPressStartToContinue();
            }
        }
    }

    public void StopPressStartToContinue()
    {
        if (pressStartToContinueStarted)
        {
            pressStartToContinueStarted = false;
            SwitchGameOverMenu();
        }
    }

    public void SwitchGameOverMenu()
    {
        //print("GAME OVER MENU");
        //print("gameOverMenuOn= " + gameOverMenuOn);

        if (gameOverMenuOn)
        {
            //GameObject incontrol = GameObject.Find("InControl manager");
            //Destroy(incontrol);
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
            gameOverMenuOn = false;
            gameOverMenu.SetActive(false);
            veil.SetActive(false);
            Debug.Log("VICTORY IMAGE SET TO FALSE");
            victoryRed.gameObject.SetActive(false);
            victoryBlue.gameObject.SetActive(false);
            gameOverPressStart.enabled = false;
            gC.gameOverStarted = false;
        }
        else
        {
            //print("ACTIVATE GAME OVER MENU");
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;
            //victoryRed.gameObject.SetActive(false);
            //victoryBlue.gameObject.SetActive(false);
            gameOverMenuOn = true;
            gameOverMenu.SetActive(true);
            gameOverPressStart.enabled = false;
            RenController.instance.SetSelectedButton(gameOverFirstButton.GetComponent<RenButton>());
        }
    }

    public void StartGameOver(Team _winnerTeam)
    {
        veil.SetActive(true);
        if (_winnerTeam == Team.A)
        {
            victoryBlue.gameObject.SetActive(true);
        }
        else if (_winnerTeam == Team.B)
        {
            victoryRed.gameObject.SetActive(true);
        }
        gameOverPressStart.enabled = true;
    }

    #region Buttons

    public virtual void ResetGame()
    {
        gC.ResetGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void BackToTeamSelect()
    {
        gC.UnPauseGame();
        if (GameInfo.instance.inControlManager != null)
        {
            //Eloy: hay que encontrar una mejor manera de resetear/borrar los controles...
            Destroy(GameInfo.instance.inControlManager);
        }
        print("LOAD MAIN TEAM SELECT");
        SceneManager.LoadScene(teamSelectScene);
    }

    public void GoBackToMenu()
    {
        gC.UnPauseGame();
        if (GameInfo.instance.inControlManager != null)
        {
            //Eloy: hay que encontrar una mejor manera de resetear/borrar los controles...
            Destroy(GameInfo.instance.inControlManager);
        }
        print("LOAD MAIN MENU");
        SceneManager.LoadScene(menuScene);
    }

    #endregion

    #region ---------------------------------------------- Pause ----------------------------------------------
    public void PauseGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        veil.SetActive(true);
        pauseRestartButton.SetActive(true);
        pauseMenuButton.SetActive(true);

        //EventSystem.current.SetSelectedGameObject(pauseRestartButton);
        RenController.instance.SetSelectedButton(pauseRestartButton.GetComponent<RenButton>());
    }

    public void UnPauseGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        veil.SetActive(false);
        pauseRestartButton.SetActive(false);
        pauseMenuButton.SetActive(false);
    }
    #endregion
}
