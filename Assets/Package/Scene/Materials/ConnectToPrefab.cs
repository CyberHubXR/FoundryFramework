using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Foundry
{
    public class ConnectToPrefab : MonoBehaviour
    {
        public GameObject Prefab;

        public List<GameObject> ConnectToList;

        [ContextMenu("Connect")]
        public void Connect() {


        }
    }
}
