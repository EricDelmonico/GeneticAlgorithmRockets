using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class UICollapse : MonoBehaviour
{
    private UIBehaviour[] children;
    [SerializeField]
    private UIBehaviour downArrow;

    private void Start()
    {
        children = transform.GetComponentsInChildren<UIBehaviour>().Where((s) => s != downArrow).ToArray();
    }

    public void Collapse()
    {
        for (int i = 0; i < children.Length; i++)
        {
            children[i].transform.localScale = Vector3.zero;
        }
        downArrow.transform.localScale = -Vector3.one;
    }

    public void Expand()
    {
        for (int i = 0; i < children.Length; i++)
        {
            children[i].transform.localScale = Vector3.one;
        }
        downArrow.transform.localScale = Vector3.zero;
    }
}
