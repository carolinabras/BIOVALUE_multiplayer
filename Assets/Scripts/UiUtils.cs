using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class UiUtils : MonoBehaviour
{
    /// <summary>
    /// Removes all child GameObjects from the specified parent GameObject.
    /// This is useful for clearing out UI elements or other child objects without destroying the parent.
    /// </summary>
    /// <param name="parent">Parent which children will be removed.</param>
    public static void ClearChildren(GameObject parent)
    {
        if (!parent)
        {
            Debug.Log("Parent GameObject is null, cannot clear children.");
            return;
        }

        //Array to hold all child obj
        List<GameObject> allChildren = new List<GameObject>(parent.transform.childCount);

        //Find all child obj and store to that array
        foreach (Transform child in parent.transform)
        {
            allChildren.Add(child.gameObject);
        }

        //Now destroy them
        foreach (GameObject child in allChildren)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                // Execute in Edit mode
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }


    /// <summary>
    /// This method is used to force the layout to rebuild.
    /// 
    /// Unfortunately, certain changes within vertical/horizontal layout groups do get rendered correctly.
    /// This is a workaround to force the layout to rebuild.
    /// We need to do a first force rebuild, that places all the elements in their correct positions,
    /// and then a new force rebuild to allow the layout to position itself in the right location.
    /// </summary>
    public static void RebuildLayout(GameObject go, MonoBehaviour contextObject)
    {
        if (go == null)
        {
            Debug.LogWarning("GameObject is null, cannot rebuild layout.");
            return;
        }

        if (contextObject == null)
        {
            Debug.LogWarning("Context object is null, cannot start coroutine for layout rebuild.");
            return;
        }

        RectTransform layoutRoot = go.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        contextObject.StartCoroutine(RebuildLayoutOnNextFrame(layoutRoot));
    }

    private static IEnumerator RebuildLayoutOnNextFrame(RectTransform layoutRoot)
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    public delegate bool ContainerFillDelegate<T>(T hook, int index) where T : MonoBehaviour;

    public static List<T> FillContainerWithPrefab<T>(GameObject container, GameObject prefab, int amount,
        ContainerFillDelegate<T> fillDelegate = null, bool cleanContainer = true, bool usePhotonInstancing = false)
        where T : MonoBehaviour
    {
        if (!container || !prefab)
        {
            Debug.LogWarning("Container or prefab is null, cannot fill container.");
            return new List<T>();
        }

        if (amount <= 0)
        {
            Debug.LogWarning("Amount must be greater than zero, cannot fill container.");
            return new List<T>();
        }

        if (cleanContainer)
        {
            ClearChildren(container);
        }

        List<T> instances = new List<T>(amount);
        for (int i = 0; i < amount; i++)
        {
            GameObject entryObject;
            if (!usePhotonInstancing)
            {
                entryObject = Instantiate(prefab, container.transform);
            }
            else
            {
                PhotonView containerPhotonView = container.GetComponent<PhotonView>();
                if (!containerPhotonView)
                {
                    Debug.LogError("Container does not have a PhotonView component.");
                    break;
                }
                
                int parentViewID = containerPhotonView.ViewID;
                object[] initData = new object[1];
                initData[0] = parentViewID;

                entryObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", prefab.name),
                    container.transform.position, Quaternion.identity, 0, initData);
                if (!entryObject.GetComponent<SetPhotonParentOnInstantiation>())
                {
                    Debug.LogWarning("Prefab does not have SetPhotonParentOnInstantiation component. Setting the parent is not possible.");
                }
            }

            if (!entryObject)
            {
                Debug.LogError("Failed to instantiate protocol entry prefab.");
                continue;
            }

            T hook = entryObject.GetComponent<T>();
            if (!hook)
            {
                Debug.LogError("ProtocolEntryHook component not found on the entry prefab.");
                continue;
            }

            instances.Add(hook);

            if (fillDelegate != null)
            {
                if (!fillDelegate(hook, i))
                {
                    break;
                }
            }
        }

        return instances;
    }
}