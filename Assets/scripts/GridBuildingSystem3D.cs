﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class GridBuildingSystem3D : MonoBehaviour {
    
    public static GridBuildingSystem3D Instance { get; private set; }

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;

    

    private GridXZ<GridObject> grid;
    //private GridXZ<GridObject> gridService;
    //private GridXZ<GridObject> gridroad;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList = null; // сериалайз филд
    private PlacedObjectTypeSO placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir dir;

    private void Awake() {
        Instance = this;

        int gridWidth = 10;
        int gridHeight = 10;
        float cellSize = 10f;
        grid = new GridXZ<GridObject>(gridWidth, gridHeight, cellSize, new Vector3(0, 0, 0), (GridXZ<GridObject> g, int x, int y) => new GridObject(g, x, y));

        placedObjectTypeSO = placedObjectTypeSOList[0];
    }
    public void SetPO(int code){
        if (code==0) { placedObjectTypeSO = placedObjectTypeSOList[0]; RefreshSelectedObjectType(); }
       if (code==1) { placedObjectTypeSO = placedObjectTypeSOList[1]; RefreshSelectedObjectType(); }
       if (code==2) { placedObjectTypeSO = placedObjectTypeSOList[2]; RefreshSelectedObjectType(); }
       if (code==3) { placedObjectTypeSO = placedObjectTypeSOList[3]; RefreshSelectedObjectType(); }
       if (code==4) { placedObjectTypeSO = placedObjectTypeSOList[4]; RefreshSelectedObjectType(); }
       if (code==5) { placedObjectTypeSO = placedObjectTypeSOList[5]; RefreshSelectedObjectType(); }
    }
    public class GridObject {

        private GridXZ<GridObject> grid;
        private int x;
        private int y;
        public PlacedObject_Done placedObject;

        // одна ячейка с хранением, есть ли здание и координаты
        public GridObject(GridXZ<GridObject> grid, int x, int y) {
            this.grid = grid;
            this.x = x;
            this.y = y;
            placedObject = null;
        }

        public override string ToString() {
            return x + ", " + y + "\n" + placedObject;
        }

        public void SetPlacedObject(PlacedObject_Done placedObject) {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, y);
        }

        public void ClearPlacedObject() {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y);
        }

        public PlacedObject_Done GetPlacedObject() {
            return placedObject;
        }

        public bool CanBuild() {
            return placedObject == null;
        }
        
    }

    // проверяет ли кнопка или нет, проверяет координаты нажатой ячейки, делает их читаемыми, ставит объект на ячейку
    private void Update() {
        // добавить if на положение мышки вне сетки
        if (Input.GetMouseButtonDown(0) && placedObjectTypeSO != null && !IsMouseOverUI()) {
            Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
            grid.GetXZ(mousePosition, out int x, out int z);

            Vector2Int placedObjectOrigin = new Vector2Int(x, z);
            placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);
   
            // Test Can Build
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(placedObjectOrigin, dir);
            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList) {
                if (!grid.GetGridObject(gridPosition.x, gridPosition.y).CanBuild()) {
                    canBuild = false;
                    break;
                }
            }
   
            if (canBuild) {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y);
   
                PlacedObject_Done placedObject = PlacedObject_Done.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO);
   
                foreach (Vector2Int gridPosition in gridPositionList) {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }
   
                OnObjectPlaced?.Invoke(this, EventArgs.Empty);
   
                //DeselectObjectType();
            } else {
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y);
                UtilsClass.CreateWorldTextPopup("Здесь нельзя строить", placedObjectWorldPosition);
                
            }
        }
   
       if (Input.GetKeyDown(KeyCode.R)) {
           dir = PlacedObjectTypeSO.GetNextDir(dir);
       }
   
       if (Input.GetKeyDown(KeyCode.Alpha1)) { placedObjectTypeSO = placedObjectTypeSOList[0]; RefreshSelectedObjectType(); }
       if (Input.GetKeyDown(KeyCode.Alpha2)) { placedObjectTypeSO = placedObjectTypeSOList[1]; RefreshSelectedObjectType(); }
       if (Input.GetKeyDown(KeyCode.Alpha3)) { placedObjectTypeSO = placedObjectTypeSOList[2]; RefreshSelectedObjectType(); }
       if (Input.GetKeyDown(KeyCode.Alpha4)) { placedObjectTypeSO = placedObjectTypeSOList[3]; RefreshSelectedObjectType(); }
       if (Input.GetKeyDown(KeyCode.Alpha5)) { placedObjectTypeSO = placedObjectTypeSOList[4]; RefreshSelectedObjectType(); }
       if (Input.GetKeyDown(KeyCode.Alpha6)) { placedObjectTypeSO = placedObjectTypeSOList[5]; RefreshSelectedObjectType(); }
   
       if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectObjectType(); }
   
        // удаление объекта правой кнопкой мыши
       if (Input.GetMouseButtonDown(1)) {
           Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
           if (grid.GetGridObject(mousePosition) != null) {
               // Valid Grid Position
               PlacedObject_Done placedObject = grid.GetGridObject(mousePosition).GetPlacedObject();
               if (placedObject != null) {
                   // Demolish
                   placedObject.DestroySelf();
   
                   List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
                   foreach (Vector2Int gridPosition in gridPositionList) {
                       grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                   }
               }
           }
       }
   }

    private void DeselectObjectType() {
        placedObjectTypeSO = null; RefreshSelectedObjectType();
    }

    private void RefreshSelectedObjectType() {
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }
    private bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition) {
        grid.GetXZ(worldPosition, out int x, out int z);
        return new Vector2Int(x, z);
    }

    public Vector3 GetMouseWorldSnappedPosition() {
        Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
        grid.GetXZ(mousePosition, out int x, out int z);

        if (placedObjectTypeSO != null) {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z);
            return placedObjectWorldPosition;
        } else {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation() {
        if (placedObjectTypeSO != null) {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        } else {
            return Quaternion.identity;
        }
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO() {
        return placedObjectTypeSO;
    }

}
