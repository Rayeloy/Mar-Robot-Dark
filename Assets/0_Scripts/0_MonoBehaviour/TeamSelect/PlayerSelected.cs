﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UnityEngine.UI;
using TMPro;

public class PlayerSelected : MonoBehaviour
{
    //public int
    public PlayerActions Actions { get; set; }
    //public int actionNum;
    //public PlayerMovement.Team team;

    public Team team = Team.none;

    public SkinnedMeshRenderer Body;
    public Material teamNeutralMat;
    public Material teamBlueMat;
    public Material teamRedMat;

    //public Renderer cachedRenderer;

    private bool _ready = false;
    /// <value>The Name property gets/sets the value of the string field, _name.</value>
    public bool Ready
    {
        get { return _ready; }
        set
        {
            animator.SetBool("Ready", value);
            _ready = value;
        }
    }

    [Header("Referencias")]
    [HideInInspector]
    public PlayerSelecionUI playerSelecionUI;
    public Sprite PlayerSelectRandom;
    public Sprite PlayerSelectBlue;
    public Sprite PlayerSelectRed;
    public Sprite PlayerSelectedRandom;
    public Sprite PlayerSelectedBlue;
    public Sprite PlayerSelectedRed;

    //public bool Ready = false;
    [Header("Referencias")]
    public Animator animator;
    [HideInInspector]
    public bool isAReleased = true;

    void Update()
    {
        if(!isAReleased && Actions.A.WasReleased)
        {
            isAReleased = true;
        }
        if (Actions.A.WasPressed && isAReleased)
            SetReady ();

        if (!Ready)
        {
            if (Actions.LeftJoystick.X < -0.5f && joystickNeutral)
            {
                joystickNeutral = false;
                switch (team)
                {
                    case Team.none:
                        changeTeam(Team.A);
                        break;
                    case Team.B:
                        changeTeam(Team.none);
                        break;
                }
            }
            else if (Actions.LeftJoystick.X > 0.5f && joystickNeutral)
            {
                joystickNeutral = false;
                switch (team)
                {
                    case Team.none:
                        changeTeam(Team.B);
                        break;
                    case Team.A:
                        changeTeam(Team.none);
                        break;
                }
            }else if (Actions.LeftJoystick.X >= -0.5f && Actions.LeftJoystick.X <= 0.5f)
            {
                joystickNeutral = true;
            }
        }
    }
    bool joystickNeutral = true;

    private void changeTeam(Team t)
    {
        team = t;
        switch (t)
        {
            case Team.A:
                Body.material = teamBlueMat;
                playerSelecionUI.TeamSelect.sprite = PlayerSelectBlue;
                break;
            case Team.B:
                Body.material = teamRedMat;
                playerSelecionUI.TeamSelect.sprite = PlayerSelectRed;
                break;
            case Team.none:
                Body.material = teamNeutralMat;
                playerSelecionUI.TeamSelect.sprite = PlayerSelectRandom;
                break;
        }
    }

    private void SetReady ()
    {
        Ready = !Ready;

        playerSelecionUI.FlechaIzquierda.enabled = !playerSelecionUI.FlechaIzquierda.enabled;
        playerSelecionUI.FlechaDerecha.enabled = !playerSelecionUI.FlechaDerecha.enabled;

        if (Ready)
        {
            playerSelecionUI.AcctionsText.text = "B to back";
            switch (team)
            {
                case Team.A:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectedBlue;
                    break;
                case Team.B:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectedRed;
                    break;
                case Team.none:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectedRandom;
                    break;
            }
        }
        else
        {
            playerSelecionUI.AcctionsText.text = "Press to choose";
            switch (team)
            {
                case Team.A:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectBlue;
                    break;
                case Team.B:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectRed;
                    break;
                case Team.none:
                    playerSelecionUI.TeamSelect.sprite = PlayerSelectRandom;
                    break;
            }
        }
    }
}
