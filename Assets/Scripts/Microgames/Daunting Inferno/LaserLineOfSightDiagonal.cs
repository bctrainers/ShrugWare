using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace ShrugWare
{
    public class LaserLineOfSightDiagonal : Microgame
    {
        [SerializeField]
        PlayerMover playerMover = null;

        [SerializeField]
        GameObject wallParent;

        [SerializeField]
        List<GameObject> laserList = new List<GameObject>();

        [SerializeField]
        List<GameObject> hitVFXList;

        [SerializeField]
        GameObject playerObj;

        private static bool hasBeenHit = false;
        private float timeRunning = 0.0f;

        private Vector3 targetPos = new Vector3();
        private Vector3 TOP_TARGET_POS = new Vector3(25, 25, 0);
        private Vector3 BOTTOM_TARGET_POS = new Vector3(-50, -25, 0);

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayerCollider.OnBadCollision += LaserHit;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PlayerCollider.OnBadCollision -= LaserHit;
        }

        protected override void OnMyGameStart()
        {
            base.OnMyGameStart();

            // 50/50 chance to go up or down
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                targetPos = BOTTOM_TARGET_POS;
            }
            else
            {
                targetPos = TOP_TARGET_POS;
            }

            wallParent.SetActive(true);
        }

        protected override void OnMyGameTick(float timePercentLeft)
        {
            base.OnMyGameTick(timePercentLeft);

            // don't enable the lasers until the end of the microgame
            timeRunning += Time.deltaTime;
            if (timeRunning >= microGameTime)
            {
                EnableLasers();
            }
            else if (hasBeenHit || timeRunning > microGameTime)
            {
                // stop moving if we hit a laser
                playerMover.DisableMovement();
            }
            else if (timeRunning < microGameTime)
            {
                MoveWalls();
            }
        }

        protected override bool VictoryCheck()
        {
            return !hasBeenHit;
        }

        private void EnableLasers()
        {
            playerMover.DisableMovement();
            foreach (GameObject laser in laserList)
            {
                laser.SetActive(true);
            }
        }
        private void LaserHit(GameObject gameObj)
        {
            if (!hasBeenHit)
            {
                int index = UnityEngine.Random.Range(0, hitVFXList.Count);
                Instantiate(hitVFXList[index], playerObj.transform.position, Quaternion.identity);
                Destroy(playerObj);
            }

            hasBeenHit = true;
            SetMicrogameEndText(false);
        }

        private void MoveWalls()
        {
            wallParent.transform.position = Vector3.MoveTowards(wallParent.transform.position, targetPos, Time.deltaTime * 20);
            if(wallParent.transform.position == targetPos)
            {
                // swap
                if(targetPos == TOP_TARGET_POS)
                {
                    targetPos = BOTTOM_TARGET_POS;
                }
                else
                {
                    targetPos = TOP_TARGET_POS;
                }
            }
        }
    }
}