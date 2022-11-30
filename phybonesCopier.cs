using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using PhysboneCollider = VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider;
using Physbone = VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone;

public class phybonesCopier : EditorWindow
{
    GameObject source;
    GameObject target;

    [MenuItem("Yelby/Phybone Copier")]
    public static void ShowWindow()
    {
        GetWindow<phybonesCopier>("Physbone Copier 1.0.1");
    }

    private void OnGUI()
    {
        source = EditorGUILayout.ObjectField("Source", source, typeof(GameObject), true) as GameObject;
        target = EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true) as GameObject;
        if (target != null)
        {
            if (GUILayout.Button("Clear Target of all Physbones(Colliders)")) clearAllPhybonesAndColliders();
        }
        if (source == null) return;

        if (GUILayout.Button("Copy/Update"))
        {
            string targetRootName = target.name;
            target.name = source.name;

            var sourceBones = getBones(source, "source");
            var targetBones = getBones(target, "target");
            if (targetBones.ContainsKey("CatIsEmpty") || sourceBones.ContainsKey("CatIsEmpty"))
            {
                target.name = targetRootName;
                return;
            }

            // Colliders
            foreach (string sourceBoneName in sourceBones.Keys)
            {
                if (!targetBones.ContainsKey(sourceBoneName)) continue;
                // Clear Previous Colliders
                var targetBoneColliderList = targetBones[sourceBoneName].GetComponents<PhysboneCollider>();
                for (int i = 0; i < targetBoneColliderList.Length; i++)
                {
                    DestroyImmediate(targetBoneColliderList[i]);
                }

                // Add New Colliders
                var sourceBoneColliderList = sourceBones[sourceBoneName].GetComponents<PhysboneCollider>();
                foreach (var sourceBoneCollider in sourceBoneColliderList)
                {
                    var newTargetBone = targetBones[sourceBoneName].AddComponent<PhysboneCollider>();
                    Type boneColliderType = typeof(PhysboneCollider);
                    FieldInfo[] boneColliderFields = boneColliderType.GetFields();
                    foreach (var field in boneColliderFields)
                    {
                        field.SetValue(newTargetBone, field.GetValue(sourceBoneCollider));
                    }
                }
            }

            // Physbones
            foreach (string sourceBoneName in sourceBones.Keys)
            {
                if (!targetBones.ContainsKey(sourceBoneName)) continue;
                // Clear previous Colliders
                var targetPhysboneList = targetBones[sourceBoneName].GetComponents<Physbone>();
                for (int i = 0; i < targetPhysboneList.Length; i++)
                {
                    DestroyImmediate(targetPhysboneList[i]);
                }

                // Add New Physbone
                var sourcePhysboneList = sourceBones[sourceBoneName].GetComponents<Physbone>();
                foreach (var sourcePhysbone in sourcePhysboneList)
                {
                    var newTargetBone = targetBones[sourceBoneName].AddComponent<Physbone>();
                    Type physboneType = typeof(Physbone);
                    FieldInfo[] physboneFields = physboneType.GetFields();
                    foreach (var field in physboneFields)
                    {
                        field.SetValue(newTargetBone, field.GetValue(sourcePhysbone));
                    }

                    // Colliders
                    newTargetBone.colliders = new List<VRC.Dynamics.VRCPhysBoneColliderBase>();
                    foreach (var sourceCollider in sourcePhysbone.colliders)
                    {
                        if (!sourceCollider) continue;
                        string nameToFind = sourceCollider.gameObject.name;
                        if (newTargetBone.colliders.Contains(targetBones[nameToFind].GetComponent<PhysboneCollider>())) continue;
                        if (targetBones.ContainsKey(nameToFind)) newTargetBone.colliders.Add(targetBones[nameToFind].GetComponent<PhysboneCollider>());
                    }

                    // Ignore
                    newTargetBone.ignoreTransforms = new List<Transform>();
                    foreach (var sourceTransform in sourcePhysbone.ignoreTransforms)
                    {
                        if (!sourceTransform) continue;
                        string nameToFind = sourceTransform.gameObject.name;
                        if (newTargetBone.ignoreTransforms.Contains(targetBones[nameToFind].transform)) continue;
                        if (targetBones.ContainsKey(nameToFind)) newTargetBone.ignoreTransforms.Add(targetBones[nameToFind].transform);
                    }
                }
            }
            target.name = targetRootName;
        }

    }

    private void clearAllPhybonesAndColliders ()
    {
        while (true)
        {
            Transform[] allChildren = target.GetComponentsInChildren<Transform>();
            int total = 0;
            foreach (Transform child in allChildren)
            {
                if (child.gameObject.GetComponent<Physbone>())
                {
                    DestroyImmediate(child.gameObject.GetComponent<Physbone>(), true);
                    total++;
                }

                if (child.gameObject.GetComponent<PhysboneCollider>())
                {
                    DestroyImmediate(child.gameObject.GetComponent<PhysboneCollider>(), true);
                    total++;
                }
            }
            AssetDatabase.Refresh();
            if (total == 0)
                return;
        }
    }

    private Dictionary<string, GameObject> getBones (GameObject obj, string sorget)
    {
        Dictionary<string, GameObject> targetBones = new Dictionary<string, GameObject>();
        targetBones.Add(obj.name, obj);
        targetBones = UnpackBones(obj, targetBones, sorget);
        return targetBones;
    }

    private Dictionary<string, GameObject> UnpackBones(GameObject bone, Dictionary<string, GameObject> targetBones, string name)
    {
        foreach (Transform child in bone.transform)
        {
            if (targetBones.ContainsKey(child.name))
            {
                EditorUtility.DisplayDialog("Dynamic Bones Copier", "Duplicate Name: " + child.name + " [" + name + "] " + child.GetHierarchyPath(), "Ok");
                Debug.LogWarning("Dynamic Bones Copier: [Duplicate Name: " + child.name + "]" + "{" + name + "}");
                targetBones.Clear();
                targetBones.Add("CatIsEmpty", null);
                return targetBones;
            }
            targetBones.Add(child.name, child.gameObject);
            targetBones = UnpackBones(child.gameObject, targetBones, name); //Recursion
        }
        return targetBones;
    }
}
