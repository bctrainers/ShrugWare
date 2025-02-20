using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace ShrugWare
{
    public class MatchPolarity : Microgame
    {
        [SerializeField]
        GameObject playerObject = null;

        [SerializeField]
        GameObject eletricityObj = null;

        [SerializeField]
        GameObject negativeGroupObj;

        [SerializeField]
        GameObject positiveGroupObj;

        [SerializeField]
        GameObject playerNegativeObj;

        [SerializeField]
        GameObject playerPositiveObj;

        [SerializeField]
        List<GameObject> hitVFXList;

        private bool polarityMatched = false;

        private bool playerPositive = false;
        private float projectileSpeed = 0.75f;

        private const float PLAYER_X_MIN = -90;
        private const float PLAYER_X_MAX = 90;

        protected override void Start()
        {
            base.Start();
            playerNegativeObj.SetActive(false);
            playerPositiveObj.SetActive(false);
            SetupGroupMembers();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayerCollider.OnGoodCollision += EnterField;
            PlayerCollider.OnBadCollision += ElectricHit;
            PlayerCollider.OnGoodExit += ExitField;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PlayerCollider.OnGoodCollision -= EnterField;
            PlayerCollider.OnBadCollision -= ElectricHit;
            PlayerCollider.OnGoodExit -= ExitField;
        }

        protected override void OnMyGameStart()
        {
            base.OnMyGameStart();
            SetupPlayer();
        }

        protected override void OnMyGameTick(float timePercentLeft)
        {
            base.OnMyGameTick(timePercentLeft);

            float time = Vector3.Distance(eletricityObj.transform.position, playerObject.transform.position) / (microGameTime - timeElapsed) * Time.deltaTime;
            eletricityObj.transform.position = Vector3.MoveTowards(eletricityObj.transform.position, playerObject.transform.position, time);
        }

        protected override bool VictoryCheck()
        {
            if (eletricityObj.activeInHierarchy)
            {
                ElectricHit(eletricityObj);
            }

            return polarityMatched;
        }

        private void SetupPlayer()
        {
            float xPos = Random.Range(PLAYER_X_MIN, PLAYER_X_MAX);
            playerObject.transform.position = new Vector2(xPos, 0);
            playerPositive = Random.Range(0, 2) == 0;
            if (playerPositive)
            {
                playerPositiveObj.SetActive(true);
            }
            else
            {
                playerNegativeObj.SetActive(true);
            }
        }

        // 50/50 chance for polarity to be on one side
        private void SetupGroupMembers()
        {
            if (Random.Range(0, 2) == 0)
            {
                Vector3 tempObjPos = negativeGroupObj.transform.position;
                negativeGroupObj.transform.position = positiveGroupObj.transform.position;
                positiveGroupObj.transform.position = tempObjPos;
            }
        }

        private void EnterField(GameObject electricField)
        {
            bool isNegative = electricField != positiveGroupObj;

            if (isNegative ^ playerPositive)
            {
                polarityMatched = true;
            }
        }

        private void ExitField(GameObject electricField)
        {
            polarityMatched = false;
        }

        private void ElectricHit(GameObject electric)
        {
            if (!polarityMatched)
            {
                playerObject.SetActive(false);
            }

            int index = UnityEngine.Random.Range(0, hitVFXList.Count);
            Instantiate(hitVFXList[index], playerObject.transform.position, Quaternion.identity);

            SetMicrogameEndText(polarityMatched);
            eletricityObj.SetActive(false);
        }
    }
}