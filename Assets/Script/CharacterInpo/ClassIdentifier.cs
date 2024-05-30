using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassIdentifier : MonoBehaviour
{
    public PlayerDataSetting playerData;

    public void ClassChangeApply(int classNum)
    {
        playerData.SetClassType(classNum, this.gameObject);
    }

}