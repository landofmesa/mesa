using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class TowerManager: MonoBehaviour
{
    GameObject holdingTower;
    GameObject selectedTower;
    public static TowerManager Global;
    string prefabName;

    void Start() {
        Global = this;
        
    }

    void Update() {

        if (holdingTower != null) placeTower();

        if (holdingTower == null) selectTower();
    }

    void placeTower() {

        holdingTower.transform.position = ScreenToWorld(Input.mousePosition);
        if (Input.GetMouseButtonDown(0)&&!EventSystem.current.IsPointerOverGameObject()) {
            PlaceableIndicator indicator = holdingTower.GetComponentInChildren<PlaceableIndicator>();

            // don't place if obstructed
            if (indicator.isObstructed) return;

            GameHandler.Global.wantPurchase(prefabName, holdingTower.transform.position);

            Destroy(indicator.gameObject);
            Destroy(holdingTower);
            prefabName = "";

            holdingTower = null;
        }
        if (Input.GetMouseButtonDown(1) || GameHandler.Global.roundCountDown==-1) {
            Destroy(holdingTower);
            holdingTower = null;
        }

    }

    void selectTower() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit.collider != null && hit.collider.GetComponent<TowerHoverHighlight>() != null) {
                hit.collider.GetComponent<TowerHoverHighlight>().setSelected();
                selectedTower = hit.collider.gameObject;

                createPlaceIndicator(
                    selectedTower.transform,
                    selectedTower.GetComponent<Tower>().data.range,
                    selectedTower.GetComponent<Tower>().data.placeRadius
                );

            } else if (selectedTower != null && !EventSystem.current.IsPointerOverGameObject()) {
                // if there is a selected tower, unselected it
                selectedTower.GetComponent<TowerHoverHighlight>().setUnselected();
                Destroy(selectedTower.GetComponentInChildren<PlaceableIndicator>().gameObject);
                selectedTower = null;
            }
        }
    }

    public void OnCreate(GameObject prefab) {
        TowerData towerData = prefab.GetComponent<Tower>().data;
        if (UserManager.Global.money < towerData.cost)
        {
            return;
        }

        holdingTower = Instantiate(prefab, ScreenToWorld(Input.mousePosition), Quaternion.identity);
        prefabName = prefab.name;

        createPlaceIndicator(
            holdingTower.transform,
            holdingTower.GetComponent<Tower>().data.range,
            holdingTower.GetComponent<Tower>().data.placeRadius
        );
        
    }

    void createPlaceIndicator(Transform parent, float attackRange, float placeRange) {
        GameObject placeIndicator = (GameObject)Instantiate(Resources.Load("Effects/BuildIndicator"));
        placeIndicator.transform.SetParent(parent, false);

        PlaceableIndicator indicator = placeIndicator.GetComponent<PlaceableIndicator>(); 
        indicator.setRadius(attackRange, placeRange);
    }

    public Vector3 ScreenToWorld(Vector3 mousePos) {
        Vector3 stw = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        return new Vector3(stw.x, stw.y, 0);

    }
    public void requestUpgrade()
    {
        if (selectedTower == null)
        {
            Debug.LogWarning("there is no tower selected");
            return;
        }

        Debug.Log("clicked upgrade");

        GameHandler.Global.wantUpgrade(selectedTower);
    }

    public bool isSelectingTower()
    {
        return (selectedTower != null);
    }

    public GameObject getTowerUpgrade()
    {
        if (selectedTower == null) return null;

        return selectedTower.GetComponent<Tower>().data.upgradeTo;
    }

    
}
