using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreCounter : MonoBehaviour
{
    TMP_Text scoreCount;
    public static int scoreNumber;
    void Start()
    {
        scoreCount = GetComponent<TMP_Text>();
        scoreNumber = 0;
    }
    void Update()
    {
        scoreCount.text = "Score: " + scoreNumber; 
    }
}
