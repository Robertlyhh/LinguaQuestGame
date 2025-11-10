using System;
using UnityEngine;

[Serializable]
public class Character
{
    public string name;
    public GameObject characterPrefab;
    
    //Stats
    public int attack;
    public int defense;
    public int speed;
    public int health;
}
