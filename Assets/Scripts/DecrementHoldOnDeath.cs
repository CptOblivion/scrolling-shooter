using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecrementHoldOnDeath : MonoBehaviour
{
    public void Death()
    {
        GameObject.FindObjectOfType<LevelController>().HoldDecrement();
    }
}
