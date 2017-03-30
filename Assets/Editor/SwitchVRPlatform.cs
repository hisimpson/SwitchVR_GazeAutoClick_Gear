using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SwitchVRPlatform : Editor
{
    [MenuItem("VRPlayer/Export Gear Scene")]
    static public void ExportGearScene()
    {
        // 지금의 장면이 편집중인 경우 다른 장면을 열 때 변경이 파기되어 버리기 때문에, 세이브 해 두는 
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Process cancelled.");
            return;
        }

        Debug.Log("Export Gear Scene");
        GameObject[] rootObj = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        string[] prefabName = {
            "Assets/Prefab/EventSystem_gear.prefab",
            "Assets/Prefab/CameraManager_gear.prefab",
            "Assets/OVRGazeUI/Prefab/GazePointerRing.prefab",
            };

        for (int n = 0; n < prefabName.Length; ++n)
        {
            if (File.Exists(prefabName[n]) == false)
            {
                Debug.LogErrorFormat("File not Exist {0}", prefabName[n]);
                return;
            }
        }

        for (int n = 0; n < prefabName.Length; ++n)
        {
            if (AddRootPrefab(rootObj, prefabName[n]) == false)
                return;
        }

        string[] removeObjName = {
            "CameraManager", "EventSystem_gvr", "GvrControllerMain", "GvrViewerMain"
            };

        List<GameObject> list = new List<GameObject>(rootObj);
        for (int n = 0; n < removeObjName.Length; ++n)
        {
            RemoveGameObject(list, removeObjName[n]);
        }

        rootObj = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        GameObject GazePointerRing = Array.Find(rootObj, p => p.name == "GazePointerRing");
        GameObject CameraManager_gear = Array.Find(rootObj, p => p.name == "CameraManager_gear");
        GameObject EventSystem_gear = Array.Find(rootObj, p => p.name == "EventSystem_gear");

        if (GazePointerRing == null || CameraManager_gear == null || EventSystem_gear == null)
        {
            Debug.LogError("GazePointerRing or CameraManager_gear or EventSystem_gear is null");
            return;
        }

        GameObject OVRCameraRigObj = UtilObject.FindChild(CameraManager_gear, "OVRCameraRig", true);
        if (OVRCameraRigObj == null)
            return;

        GameObject CenterEyeAnchor = UtilObject.FindGameObject(OVRCameraRigObj, "TrackingSpace/CenterEyeAnchor");
        if (CenterEyeAnchor == null)
        {
            Debug.LogError("TrackingSpace/CenterEyeAnchor GameObject not found.");
            return;
        }

        OVRCameraRig ovrCameraRig = OVRCameraRigObj.GetComponent<OVRCameraRig>();
        ovrCameraRig.usePerEyeCameras = false;

        OVRInputModule ovrInputModule = EventSystem_gear.GetComponent<OVRInputModule>();
        ovrInputModule.rayTransform = CenterEyeAnchor.transform;

        OVRGazePointer ovrGazePointer = GazePointerRing.GetComponent<OVRGazePointer>();
        ovrGazePointer.cameraRig = ovrCameraRig;

        ChangeGearCanvas(rootObj, CenterEyeAnchor);

        AssetDatabase.RemoveUnusedAssetBundleNames();

        Save();
    }

    static void Save()
    {
        var currentScenePath = EditorSceneManager.GetActiveScene().path;
        var directory = Path.GetDirectoryName(currentScenePath);
        var name = Path.GetFileNameWithoutExtension(currentScenePath);
        var ext = Path.GetExtension(currentScenePath);
        var fullpath = string.Format("{0}/{1}_gear{2}", directory, name, ext);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), fullpath);

        EditorSceneManager.OpenScene(currentScenePath);
    }

    static bool AddRootPrefab(GameObject[] root, string prefabFullPathName)
    {
        string fileName = Path.GetFileNameWithoutExtension(prefabFullPathName);
        GameObject obj = Array.Find(root, p => p.name == fileName);
        if (obj != null)
            return true;

        GameObject go = AssetDatabase.LoadAssetAtPath(prefabFullPathName, typeof(GameObject)) as GameObject;
        if (go == null)
        {
            Debug.LogError("Export Gear Scene : AddRootPrefab LoadAssetAtPath " + prefabFullPathName);
            return false;
        }

        GameObject sceneObj = PrefabUtility.InstantiatePrefab(go) as GameObject;
        if (sceneObj == null)
        {
            Debug.LogError("Export Gear Scene : AddPrefab InstantiatePrefab " + prefabFullPathName);
            AssetDatabase.RemoveUnusedAssetBundleNames();
            return false;
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        return true;
    }

    static bool RemoveGameObject(List<GameObject> root, string prefabName)
    {
        Debug.LogFormat("RemoveGameObject {0}", prefabName);

        GameObject obj = root.Find(p => p.name == prefabName);
        if (obj == null)
            return false;

        DestroyImmediate(obj);
        root.Remove(obj);
        obj = null;
        Resources.UnloadUnusedAssets();
        return true;
    }

    static bool ChangeGearCanvas(GameObject[] root, GameObject cameraObj)
    {
        GameObject[] obj = Array.FindAll(root,  o => o.GetComponent<Canvas>() != null);

        string prefabFullPathName = "Assets/OVRGazeUI/Prefab/CanvasPointer.prefab";

        if (File.Exists(prefabFullPathName) == false)
        {
            Debug.LogError("Can't not found CanvasPointer.prefab");
            return false;
        }

        GameObject go = AssetDatabase.LoadAssetAtPath(prefabFullPathName, typeof(GameObject)) as GameObject;
        if (go == null)
        {
            Debug.LogErrorFormat("Error AssetDatabase.LoadAssetAtPath CanvasPointer.prefab");
            return false;
        }

        Camera camera = cameraObj.GetComponent<Camera>();

        for (int n = 0; n < obj.Length; ++n)
        {
            GameObject pointerObj = null;
            if (obj[n].transform.FindChild("CanvasPointer") == true)
            {
                pointerObj = obj[n].transform.FindChild("CanvasPointer").gameObject;
            }
            else
            { 
                pointerObj = PrefabUtility.InstantiatePrefab(go) as GameObject;
                if (pointerObj == null)
                {
                    Debug.LogErrorFormat("Error PrefabUtility.InstantiatePrefab CanvasPointer.prefab");
                    return false;
                }
            }
           
            pointerObj.transform.SetParent(obj[n].transform);
            RectTransform rectTrans = pointerObj.GetComponent<RectTransform>();
            rectTrans.localRotation = Quaternion.identity;
            rectTrans.localScale = new Vector3(1, 1, 1);
            rectTrans.localPosition = Vector3.zero;

            Canvas canvas = obj[n].GetComponent<Canvas>();
            canvas.worldCamera = camera;

            OVRRaycaster ovrRaycaster = obj[n].GetComponent<OVRRaycaster>();
            if (ovrRaycaster == null)
                ovrRaycaster = obj[n].AddComponent<OVRRaycaster>();

            ovrRaycaster.blockingObjects = OVRRaycaster.BlockingObjects.All;
            ovrRaycaster.pointer = pointerObj;

            OVRMousePointer ovrMousePointer = obj[n].GetComponent<OVRMousePointer>();
            if (ovrMousePointer == null)
                ovrMousePointer = obj[n].AddComponent<OVRMousePointer>();

            ovrMousePointer.mouseShowPolicy = OVRMousePointer.MouseShowPolicy.withGaze;
            ovrMousePointer.hideGazePointerWhenActive = true;
            ovrMousePointer.mouseMoveSpeed = 11;

            if(obj[n].GetComponent<GvrPointerGraphicRaycaster>() != null)
                DestroyImmediate( obj[n].GetComponent<GvrPointerGraphicRaycaster>());
            //GraphicRaycaster 컴포넌트가 한번 제거된 상태에서 다시 제거하면
            //원인 모를 에러가 발생해서 디즈블만 시킨다.
            obj[n].GetComponent<GraphicRaycaster>().enabled = false;
        }

        return true;
    }
}