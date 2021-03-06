using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameHandler : StateManager
{
    public static GameHandler Global;

    List<NetworkIdentity> allPlayers;
    public GameObject extraPlayerUIPrefab;
    public PlayerUIInfo localPlayerUI;
    public UserManager singletonDataStorage;
    public WaveManager waveManager;
    public GameObject mainCamera;
    public GameObject playersUI;
    public GameObject enemiesUI;
    public GameObject towersUI;
    public GameObject mapSlots;
    public TMP_Text roundText;
    public float roundPrep;

    [SyncVar(hook = nameof(roundChanged))]
    public float round = 0.0f;

    [SyncVar(hook = nameof(roundChanged))]
    public float roundCountDown = -1f;

    float roundStart = -2f;

    public void Start()
    {
        Global = this;
    }

    // Server Init
    public override void onBegin(Dictionary<NetworkIdentity, string> players)
    {

        PathNode[] nodePaths = new PathNode[players.Count];
        NetworkPlayer[] coresPlrs = new NetworkPlayer[players.Count];
        allPlayers = new List<NetworkIdentity>();

        int c = 0;
        foreach (KeyValuePair<NetworkIdentity, string> plr in players)
        {
            NetworkPlayer netPlr = plr.Key.gameObject.GetComponent<NetworkPlayer>();
            Transform playArea = mapSlots.transform.GetChild(c);
            netPlr.username = plr.Value;
            netPlr.location = playArea.position;
            allPlayers.Add(plr.Key);

            coresPlrs[c] = netPlr;
            nodePaths[c] = playArea.Find("pathroot").Find("pathnode0").GetComponent<PathNode>();
            c++;
        }

        RpcInitializeGame(allPlayers.ToArray());

        waveManager.spawnPointList = nodePaths;
        waveManager.coresPlayers = coresPlrs;
        round = 1.0f;
        roundStart = -1f;
    }

    public override void onEnter(NetworkConnection conn, string username)
    {
        Debug.Log("Handle entering midgame");
    }

    public override void onLeave(NetworkConnection conn)
    {
        Debug.Log("Handle leaving");
    }

    // Client Init
    [ClientRpc]
    public void RpcInitializeGame(NetworkIdentity[] playerIDs)
    {
        // remove old player stuff
        foreach (Transform child in playersUI.transform)
        {
            Destroy(child.gameObject);
        }

        // link listener UIs
        CameraController camControl = mainCamera.GetComponent<CameraController>();
        int i = 0;
        foreach (NetworkIdentity id in playerIDs)
        {
            NetworkPlayer netPlr = id.gameObject.GetComponent<NetworkPlayer>();
            if (id.isLocalPlayer)
            {
                netPlr.dataSingleton = singletonDataStorage;
                netPlr.uiAdjust = localPlayerUI;
                netPlr.locationChanged(new Vector3(), netPlr.location);

            }
            else
            { 
                // make new prefab
                GameObject plrUI = Instantiate(extraPlayerUIPrefab, playersUI.transform);
                plrUI.transform.position = plrUI.transform.position + new Vector3(0, -75 * i, 0);
                netPlr.uiAdjust = plrUI.GetComponent<PlayerUIInfo>();

                netPlr.uiAdjust.changeOrigin.onClick.AddListener(() =>
                {
                    camControl.SetTarget(new Vector2(netPlr.location.x, netPlr.location.y));
                    setPlayerTarget(netPlr);
                });

                netPlr.uiAdjust.changeOrigin2.onClick.AddListener(() =>
                {
                    camControl.SetTarget(new Vector2(netPlr.location.x, netPlr.location.y));
                    setPlayerTarget(netPlr);
                });

                i++;
            }

            netPlr.setClientMoney(netPlr.money);
            netPlr.healthChanged(100, netPlr.hp);
            netPlr.uiAdjust.username.text = netPlr.username;
        }
    }

    // Client listeners
    public void roundChanged(float oldValue, float newValue)
    {
        roundText.text = (roundCountDown == -1) ? round.ToString() : roundCountDown.ToString();
        roundText.color = (roundCountDown == -1) ? new Color(0, 238 / 255f, 1) : new Color(1, 1, 1);

        if (newValue == -1f)
        {
            enemiesUI.SetActive(true);
            towersUI.SetActive(false);
        }
        else if (oldValue == -1f)
        {
            enemiesUI.SetActive(false);
            towersUI.SetActive(true);
        }
    }

    public void wantUpgrade(GameObject tower)
    {
        CmdUpgrade(tower);
    }
    [Command(requiresAuthority = false)]
    public void CmdUpgrade(GameObject tower, NetworkConnectionToClient sender = null)
    {
        if (tower != null && tower.GetComponent<Tower>().creator.Equals(sender.identity.gameObject.GetComponent<NetworkPlayer>()))
        {
            Tower towerScript = tower.GetComponent<Tower>();
            GameObject upgradeTo = towerScript.data.upgradeTo;

            if (upgradeTo != null)
            {
                int cost = upgradeTo.GetComponent<Tower>().data.cost;
                NetworkPlayer plr = sender.identity.gameObject.GetComponent<NetworkPlayer>();

                GameObject newTower = Instantiate(upgradeTo, tower.transform.position, Quaternion.identity);
                plr.incrementMoney(cost * -1);

                Tower tow = newTower.GetComponent<Tower>();
                tow.creator = plr;
                tow.placed = true;

                NetworkServer.Destroy(tower);
                NetworkServer.Spawn(newTower);
            }
            else
            {
                Debug.Log("No upgrades");
            }

        }
        else
        {
            Debug.Log("Invalid tower parameter");
        }
    }

    public void wantPurchase(string towerName, Vector3 position)
    {
        CmdPurchase(towerName, position);
    }

    // Purchase command
    [Command(requiresAuthority = false)]
    public void CmdPurchase(string towerName, Vector3 position, NetworkConnectionToClient sender = null)
    {
        NetworkIdentity id = sender.identity;
        GameObject towerpref = Resources.Load<GameObject>(towerName);

        if (towerpref == null)
        {
            Debug.Log("Rescource could not be found");
            return;
        }

        GameObject tower = Instantiate(towerpref, position, Quaternion.identity);
        int cost = tower.GetComponent<Tower>().data.cost;
        NetworkPlayer plr = id.gameObject.GetComponent<NetworkPlayer>();

        if (plr.money >= cost)
        {
            plr.incrementMoney(cost * -1);

            Tower tow = tower.GetComponent<Tower>();
            tow.creator = plr;
            tow.placed = true;

            NetworkServer.Spawn(tower);
        }
        else
        {
            Destroy(tower);
        }
    }

    Dictionary<string, int> unitPriorities = new Dictionary<string, int> {
        { "MinionLv1", 1 },
        {"Smoker", 2},
        {"HellHound", 2},
        {"HellHoundCarriage", 3},
        {"Slime", 3},
        { "MinionLv2", 3 },
        {"HellHoundConvoy", 5},
        { "MinionLv3", 8 },
        { "JoggerLv1", 3 },
        { "JoggerLv2", 8 },
        { "TankLv1", 12 },
    };

    // Enemy Spawning
    private NetworkPlayer target;

    public void setPlayerTarget(NetworkPlayer newTarget)
    {
        if (newTarget == target)
        {
            if (target != null)
            {
                target.toggleTargetted();
                target = null;
            }
        }
        else
        {
            if (target != null)
            {
                target.toggleTargetted();
            }

            target = newTarget;
            target.toggleTargetted();
        }

        Debug.Log("Told of " + newTarget.ToString() + ", now the target is " + target);
        
    }

    public void sendEnemy(string name)
    {
        if (target != null){
            Debug.Log("Sending command to make enemy");
            CmdSendUnit(name, target.gameObject);

        }
    }

    // Purchase command
    [Command(requiresAuthority = false)]
    public void CmdSendUnit(string unitName, GameObject targetObject, NetworkConnectionToClient sender = null)
    {
        GameObject unitref = Resources.Load<GameObject>(unitName);

        if (unitref == null)
        {
            Debug.Log("Rescource could not be found");
            return;
        }

        int cost = unitref.GetComponent<Enemy>().data.cost;
        NetworkPlayer target = targetObject.GetComponent<NetworkPlayer>();
        NetworkPlayer plr = sender.identity.gameObject.GetComponent<NetworkPlayer>();

        if (plr != null && target != null && plr.money >= cost && roundStart == -1)
        {
            plr.incrementMoney(cost * -1);
            waveManager.spawnUnit(unitName, 1, 0, target);
            Debug.Log("Done");
        }
        else
        {
            Debug.Log("Something off");
        }
    }

    // Game Logic
    [Server]
    private void startRound()
    {
        Debug.Log("Starting Round");
        List<string> unitChoices = new List<string> { };
        float unitCount = (round - 1) * 50;

        foreach (KeyValuePair<string, int> unit in unitPriorities)
        {
            if (unitCount >= unit.Value)
            {
                for (int i = 0; i < Mathf.Min(10, Mathf.Max(unitCount, 20 / unit.Value)); i++)
                {
                    unitChoices.Add(unit.Key);
                }
            }
        }

        while (unitCount > 0)
        {
            string wantedEnemy = unitChoices[(int)( Mathf.Min(Random.value, 0.99f) * unitChoices.Count)];
            int heaviness = unitPriorities[wantedEnemy];

            int chosenAmount = Mathf.Max((int)(Random.value * (unitCount - heaviness)), 0) + 1;
            unitCount -= chosenAmount * heaviness * 0.75f;

            waveManager.queueUnit(wantedEnemy, chosenAmount, Mathf.Max(0.05f, Random.value * heaviness / 5), Mathf.Max(0.05f, Random.value * heaviness / 5) );
        }
        
        waveManager.startWave();
        roundStart = Time.realtimeSinceStartup + roundPrep * 5;
    }

    private void Update()
    {
        // Game hasn't started yet
        if (roundStart == -2)
        {
            return;
        }

        // Start preparing for next round
        if (roundStart == -1 && !waveManager.isWaveOngoing)
        {
            roundStart = Time.realtimeSinceStartup + roundPrep;
            round += 0.1f;
        }

        // Start the round horde
        if (roundStart != -1 && Time.realtimeSinceStartup >= roundStart)
        {
            startRound();
            roundStart = -1;
        }

        // Adjust what the client will show
        if (roundStart == -1)
        {
            roundCountDown = -1;
        }
        else
        {
            roundCountDown = (int)(roundStart - Time.realtimeSinceStartup);
        }
    }
}
