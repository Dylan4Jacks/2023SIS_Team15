using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BaseCard {
    GameObject  creatureDisplay; // this is a GameObject incase we want to add accessories to the wug
    public string cardName;
    public string description;
    public int strength;
    public int speed;
    public int health;
    public int shield;
    public string[] ability;

    public BaseCard(string name, string description, int strength, int speed, int health) {
        this.cardName = name;
        this.cardName = description;
        this.strength = strength;
        this.speed = speed;
        this.health = health;
        this.shield = 0;
        this.ability = new string[] {"shield", "right", "1"};
    }
}
