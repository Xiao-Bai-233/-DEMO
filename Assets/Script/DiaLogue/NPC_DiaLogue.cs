using System;
using Fungus;
using UnityEngine;
using UnityEngine.UI;
public class NPC_DiaLogue : MonoBehaviour
{
    public GameObject F_DiaLogue;
    public String npcName;
    
    private Flowchart flowchart;
    private bool cansay;
    void Start()
    {
        flowchart = GameObject.Find("Flowchart").GetComponent<Flowchart>();
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Say();
        }
    }

    void Say()
    {
        if (cansay)
        {
            if (flowchart.HasBlock(npcName))
            {
                flowchart.ExecuteBlock(npcName);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        F_DiaLogue.SetActive(true);
        if (other.tag.Equals("Player"))
        {
            cansay = true;
        }
        Debug.Log("1111111111111111");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        F_DiaLogue.SetActive(false);
        if (other.tag.Equals("Player"))
        {
            cansay = false;
        }
    }
}
