using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSethStart : MonoBehaviour
{
    public GameObject Fadescene;
    public GameObject ChaSeth;
    public GameObject ChKaisa;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(EvenStart());
    }

    // Update is called once per frame
    IEnumerator EvenStart()
    {
        yield return new WaitForSeconds(2f);
        Fadescene.SetActive(false);
        ChaSeth.SetActive(true);
        yield return new WaitForSeconds(2f);
        ChKaisa.SetActive(true);
        //Talk to Seth
    }
}
