using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShrugWare
{
    public class CastFrostboltMicrogame : Microgame
    {
        [SerializeField]
        Button fireballButton;

        [SerializeField]
        Button frostboltButton;

        [SerializeField]
        Button healButton;

        [SerializeField]
        GameObject leftButtonObj;

        [SerializeField]
        GameObject middleButtonObj;

        [SerializeField]
        GameObject rightButtonObj;

        [SerializeField]
        GameObject bossObj;

        [SerializeField]
        AudioClipData frostboltAudio;

        [SerializeField]
        AudioClipData buttonClickAudio;

        private bool castedFrostbolt = false;

        protected override void Start()
        {
            base.Start();
            fireballButton.gameObject.SetActive(false);
            frostboltButton.gameObject.SetActive(false);
            frostboltButton.onClick.AddListener(CastFrostboltButtonPressed);
            healButton.gameObject.SetActive(false);
            RandomizeButtons();
        }

        protected override void OnMyGameStart()
        {
            base.OnMyGameStart();
            fireballButton.gameObject.SetActive(true);
            frostboltButton.gameObject.SetActive(true);
            healButton.gameObject.SetActive(true);
            bossObj.SetActive(true);
        }

        protected override bool VictoryCheck()
        {
            fireballButton.gameObject.SetActive(false);
            frostboltButton.gameObject.SetActive(false);
            healButton.gameObject.SetActive(false);
            return castedFrostbolt;
        }

        public void RandomizeButtons()
        {
            List<Button> buttonList = new List<Button>();
            buttonList.Add(fireballButton);
            buttonList.Add(frostboltButton);
            buttonList.Add(healButton);

            int randIndex = Random.Range(0, 3);
            Button button = buttonList[randIndex];
            button.gameObject.transform.position = leftButtonObj.transform.position;
            buttonList.RemoveAt(randIndex);

            int randIndex2 = Random.Range(0, 2);
            Button button2 = buttonList[randIndex2];
            button2.gameObject.transform.position = middleButtonObj.transform.position;
            buttonList.RemoveAt(randIndex2);

            Button button3 = buttonList[0];
            button3.gameObject.transform.position = rightButtonObj.transform.position;
            buttonList.RemoveAt(0);
        }
        private void CastFrostboltButtonPressed()
        {
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAudioClip(frostboltAudio);
            }

            castedFrostbolt = true;

            fireballButton.gameObject.SetActive(false);
            frostboltButton.gameObject.SetActive(false);
            healButton.gameObject.SetActive(false);
            SetMicrogameEndText(true);
            bossObj.GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.cyan;
        }

        public void OtherButtonPressed()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAudioClip(buttonClickAudio);
            }

            fireballButton.gameObject.SetActive(false);
            frostboltButton.gameObject.SetActive(false);
            healButton.gameObject.SetActive(false);
            SetMicrogameEndText(false);
        }
    }
}