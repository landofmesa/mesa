using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Tower", menuName = "Tower")]
public class TowerData : ScriptableObject
{
    public new string name;
    public string description;
    public int cost;
    public float range;
    public float placeRadius;
    public float atkSpeed;
    public GameObject projectile;
    public GameObject upgradeTo;
    public bool homing;
    
}
