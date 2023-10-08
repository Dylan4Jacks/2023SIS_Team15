using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Threading;
using System.Threading.Tasks;

public class PlacedCreature : MonoBehaviour
{
    [SerializeField] public CreatureAnimationHandler creatureAnimator;
    private BattleController battleController;
    public BaseCard baseStats;
    public int currentHealth;
    public int currentStrength;
    public int currentSpeed; 
    public bool isSlain;
    public int currentShield;
    public string[] currentAbility;

    [SerializeField] private GameObject displayHealth;
    [SerializeField] private GameObject displayStrength;
    
    private TextMeshPro healthText;
    private TextMeshPro strengthText;
    private int alignment;
    private int position;
    private Team team;
    public bool isVictorious; 
    public PlacedCreature lanePartner; 
    // Awake is called when instantiated
    void Awake()
    {
        this.battleController = BattleController.instance;
        this.healthText = displayHealth.GetComponent<TextMeshPro>();
        this.strengthText = displayStrength.GetComponent<TextMeshPro>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*************************************************
    * BASIC FUNCTIONS (setters, getters, etc.)
    *************************************************/
    public void setupCard(BaseCard baseCard, int alignment, int position) {
        this.baseStats = baseCard;
        setCurrentHealth(baseCard.health);
        setCurrentStrength(baseCard.strength);
        setCurrentSpeed(baseCard.speed);
        setCurrentShield(baseCard.shield);
        setCurrentAbility(baseCard.ability);
        this.position = position;
        this.alignment = alignment;
        this.lanePartner = battleController.teams[alignment].placedCreatures[Utils.calculateLanePartner(this.position)];
        if (lanePartner != null) {
            lanePartner.setNewLanePartner(this);
        }
        this.team = BattleController.instance.teams[alignment];
    }
    public void setCurrentStrength(int value) {
        currentStrength = value;
        strengthText.text = value.ToString();
    }
    public void setCurrentHealth(int value) {
        currentHealth = value;
        healthText.text = value.ToString();
    }
    public void setCurrentSpeed(int value) {
        //TODO add display speed
        currentSpeed = value;
    }
    public void setCurrentShield(int value)
    {
        //TODO add display sheild
        currentShield = value;
    }
    public void setCurrentAbility(string[] value)
    {
        currentAbility = value;
    }
    public bool hasLanePartner() {
        return lanePartner != null;
    }

    public void setNewLanePartner(PlacedCreature partner){
        this.lanePartner = partner;
    }

    public int getPosition() {
        return this.position;
    }

    public int getAlignment() {
        return this.alignment;
    }
    /*************************************************
    * AUXILIARY FUNCTIONS (to derive things)
    *************************************************/
    public List<PlacedCreature> getLaneOpponents() {
        int lane = Utils.calculateLane(position);
        return team.getAdversary().getLaneCreatures(lane);
    }
    public bool canAttack() {
        // cards can only attack if: 
        // strength > 0
        // They are in the front row OR the creature infront has died
        // They have an attribute which lets them ignore the card infront
        bool canAttack = true; 
        if (this.isSlain) {canAttack = false;}
        if (Utils.calculateRow(position, alignment) == Utils.BACK_ROW) {
            if (hasLanePartner() && !lanePartner.isSlain) {canAttack = false;}
        }
        return canAttack;
    }
    /*************************************************
    * BATTLE FUNCTIONS
    *************************************************/
    private PlacedCreature findTarget() {
        List<PlacedCreature> laneOpponents = this.getLaneOpponents();
        //attack directly infront 
        PlacedCreature target = laneOpponents[0];
        //attack directly infront in its own row
        if (target == null || target.isSlain) {
            target = laneOpponents[1];
            if (target == null || target.isSlain) {
                return null; 
            }
        }
        Debug.Log(Utils.roundTemplate() + this.baseStats.cardName + " is targetting " + target.baseStats.cardName);
        return target;
    }
     public IEnumerator attack() {
        if (this.isSlain || !this.canAttack()) {
            Debug.Log(Utils.roundTemplate() + this.baseStats.cardName + "cannot attack.");
            yield break;
        }
        PlacedCreature target = this.findTarget();
        if (target == null) {
            Debug.Log(Utils.roundTemplate() + this.baseStats.cardName + "is targetting null");
            yield break;
        }
        yield return StartCoroutine(attack(target));
    }

    public IEnumerator attack(PlacedCreature target) {
        Debug.Log(Utils.roundTemplate() + baseStats.cardName + " animation start");
        yield return StartCoroutine(creatureAnimator.basicAttack(alignment, target));
        Debug.Log(Utils.roundTemplate() + baseStats.cardName + " animation finish");
        yield return StartCoroutine(target.beAttacked(this));
        yield return StartCoroutine(this.checkDeath(target)); //check death after damage taken from retaliation
    }

    public IEnumerator beAttacked(PlacedCreature attacker) {
        //check abilities to see if anything activates upon being attacked
        //then take damage
        //then retaliate
        // Debug.Log(attacker.baseStats.cardName + " is attacking " + this.baseStats.cardName);
        int adjustedAttack = attacker.currentStrength - currentShield;
        if (adjustedAttack > 0)
        {
            setCurrentHealth(currentHealth - adjustedAttack);
        }
        retaliate(attacker);
        yield return StartCoroutine(this.checkDeath(attacker)); 
    }

    public void retaliate(PlacedCreature attacker) {
        // Debug.Log(this.baseStats.cardName + " is retaliating against" + attacker.baseStats.cardName);
        int adjustedAttack = this.currentStrength - attacker.currentShield;
        if (adjustedAttack > 0)
        {
            attacker.setCurrentHealth(attacker.currentHealth - adjustedAttack);
        }
    }

    public IEnumerator checkDeath(PlacedCreature killer) {
        if (this.currentHealth <= 0) {
            yield return perish();
            Debug.Log(this.baseStats.cardName + " has been slain by " + killer.baseStats.cardName);
        }
    }

    public IEnumerator perish() {
        this.isSlain = true;
        yield return StartCoroutine(creatureAnimator.perish());
    }

    /*************************************************
    * ABILTIES
    *************************************************/

    public int abilityTarget (string target)
    {
        // Determines target of ability for player's team
        if (alignment == 1)
        {
            switch (target)
            {
                case "self":
                    return position;
                case "front":
                    if (position - 3 >= 0)
                    {
                        return position - 3;
                    }
                    break;
                case "back":
                    if (position + 3 <= 5)
                    {
                        return position + 3;
                    }
                    break;
                case "left":
                    if ((position - 1 >= 0 && position <= 2) || (position - 1 >= 3 && position <= 5 && position >= 3))
                    {
                        return position - 1;
                    }
                    break;
                case "right":
                    if ((position + 1 <= 2 && position <= 2) || (position + 1 <= 5 && position <= 5 && position >= 3))
                    {
                        return position + 1;
                    }
                    break;
            }
        }
        // Determines the target of the ability for the enemy's team
        else
        {
            switch (target)
            {
                case "self":
                    return position;
                case "front":
                    if (position + 3 <= 5)
                    {
                        return position + 3;
                    }
                    break;
                case "back":
                    if (position - 3 >= 0)
                    {
                        return position - 3;
                    }
                    break;
                case "left":
                    if ((position - 1 >= 0 && position <= 2) || (position - 1 >= 3 && position <= 5 && position >= 3))
                    {
                        return position - 1;
                    }
                    break;
                case "right":
                    if ((position + 1 <= 2 && position <= 2) || (position + 1 <= 5 && position <= 5 && position >= 3))
                    {
                        return position + 1;
                    }
                    break;
            }
        }
        return -1;
    }

    public void triggerInitialAbilities () {
        int targetPosition = this.abilityTarget(currentAbility[1]);

        if (currentAbility[0] == "shield")
        {
            if (targetPosition != -1)
            {
                battleController.teams[alignment].placedCreatures[targetPosition].currentShield = int.Parse(currentAbility[2]);
            }
        } 
        else if (currentAbility[0] == "health")
        {
            if (targetPosition != -1)
            {
                battleController.teams[alignment].placedCreatures[targetPosition].currentHealth += int.Parse(currentAbility[2]);
            }
        }
        else if (currentAbility[0] == "stregnth")
        {
            if (targetPosition != -1)
            {
                battleController.teams[alignment].placedCreatures[targetPosition].currentStrength += int.Parse(currentAbility[2]);
            }
        }
        else if (currentAbility[0] == "speed")
        {
            if (targetPosition != -1)
            {
                battleController.teams[alignment].placedCreatures[targetPosition].currentSpeed += int.Parse(currentAbility[2]);
            }
        }
    }
}
