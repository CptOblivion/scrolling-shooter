using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public int Health = 10;
    public bool AcceptingDamage = true;

    public bool Damage(int damage) 
    {
        if (AcceptingDamage)
        {
            Health -= damage;
            return true; //returns true if damage is dealt
        }
        else return false;

    }
}
