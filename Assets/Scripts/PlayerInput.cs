using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerInput
{
    public float velocity;
    public float acceleration;
    public float brake;
    public float turn;
    public bool jump;
    public bool crouch;

    public Vector3 direction;

    public float angularVelocity;

    public float m_TurnAmount;
    public float m_ForwardAmount;
}
