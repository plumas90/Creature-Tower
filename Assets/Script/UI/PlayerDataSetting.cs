using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum CharClass
{
    TV,
    Charlie,
    Kim,
}

public class PlayerDataSetting : MonoBehaviour
    {
        [Header("PlayerSO")]
        public PlayerSO TVSO;
        public PlayerSO CharlieSO;
        public PlayerSO KimSO;

        [Header("Player")]
        public GameObject player;


        public void SetClassType(int charType)
        {
            PlayerStatControl statSO = player.GetComponent<PlayerStatControl>();

            switch (charType)
            {
                case (int)CharClass.TV:
                    statSO.CharacterChange(TVSO);
                    DelComponent(statSO.gameObject);
                    statSO.gameObject.AddComponent<Player1Skill>();
                    break;
                case (int)CharClass.Charlie:
                    statSO.CharacterChange(CharlieSO);
                    DelComponent(statSO.gameObject);
                    statSO.gameObject.AddComponent<Player2Skill>();
                    break;
                case (int)CharClass.Kim:
                    statSO.CharacterChange(KimSO);
                    DelComponent(statSO.gameObject);
                    statSO.gameObject.AddComponent<Player3Skill>();
                    break;
            }

            //LobbyManager.Instance.audioLibrary.SetupPlayerSE();
        }

        public void DelComponent(GameObject GO)
        {

            GO.GetComponent<PlayerInputController>().SkillReset();

            if (GO.GetComponent<Player1Skill>())
            {
                Destroy(GO.GetComponent<Player1Skill>());
            }
            if (GO.GetComponent<Player2Skill>())
            {
                Destroy(GO.GetComponent<Player2Skill>());
            }
            if (GO.GetComponent<Player3Skill>())
            {
                Destroy(GO.GetComponent<Player3Skill>());
            }
        }

    }