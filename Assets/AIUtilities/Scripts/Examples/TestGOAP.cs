using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System;
using GOAP = fzmnm.GOAP;
using State = fzmnm.GOAP.State;
using static fzmnm.TestGOAP.Condition;
namespace fzmnm
{

    public class TestGOAP : MonoBehaviour
    {
        public enum Condition { Health, Place, Equipped, HasAmmo, KillEnemy }
        public enum Equipment { None = 0, Sword, Bow }
        public enum Places { Nowhere = 0, NearEnemy, ShootSpot, Sword, Bow, Ammo, Heal }

        public float myFullHP = 150;
        public float myCurrentHP = 50;
        public float mySpeed = 5;
        public float enemySpeed = 2;
        public float enemyHP = 50;
        public float enemyMeleeDPS = 20;
        public float enemyRangedDPS = 10;
        public float rangedRange = 10;
        public float myMeleeDPS = 30;
        public float myRangedDPS = 15;
        public bool nearEnemy = true;
        [Button]
        void DoTest()
        {
            GOAP.Planner planner = new fzmnm.GOAP.Planner();
            planner.goal = new GOAP.Goal("KillEnemy").Condition(KillEnemy, true);

            planner += new GOAP.Action("AttackEnemy")
                .Cost(KillCost(Equipment.Sword))
                .Effect(KillEnemy, true).CustomEffect(GetReduceHPEffect(KillDamage(Equipment.Sword)))
                .Condition(Place, Places.NearEnemy).Condition(Equipped, Equipment.Sword);
            planner += new GOAP.Action("ShootEnemy")
                .Cost(KillCost(Equipment.Bow))
                .Effect(KillEnemy, true).CustomEffect(GetReduceHPEffect(KillDamage(Equipment.Bow)))
                .Condition(Place, Places.ShootSpot).Condition(Equipped, Equipment.Bow).Condition(HasAmmo, true);
            planner += new GOAP.Action("FleeFromEnemy")
                .Cost(FleeDamage)
                .Effect(Place, Places.Nowhere).CustomEffect(GetReduceHPEffect(FleeDamage()))
                .Condition(Place, Places.NearEnemy);
            planner += new GOAP.Action("GotoEnemy")
                .Cost(MoveCost)
                .Effect(Place, Places.NearEnemy);
            planner += new GOAP.Action("FindWeapon")
                .Cost(MoveCost)
                .Effect(Equipped, Equipment.Sword).Effect(Place, Places.Sword)
                .NCondition(Place, Places.NearEnemy);
            planner += new GOAP.Action("FindBow")
                .Cost(MoveCost)
                .Effect(Equipped, Equipment.Bow).Effect(Place, Places.Bow)
                .NCondition(Place, Places.NearEnemy);
            planner += new GOAP.Action("FindAmmo")
                .Cost(MoveCost)
                .Effect(HasAmmo, true).Effect(Place, Places.Ammo)
                .NCondition(Place, Places.NearEnemy);
            planner += new GOAP.Action("FindHeal")
                .Cost(MoveCost)
                .Effect(Place, Places.Heal)
                .NCondition(Place, Places.NearEnemy);
            planner += new GOAP.Action("FindShotSpot")
                .Cost(MoveCost)
                .Effect(Place, Places.ShootSpot)
                .NCondition(Place, Places.NearEnemy);
            planner += new GOAP.Action("Heal")
                .Cost(1)
                .CustomEffect((State s) => s[Health] = (int)myFullHP)
                .Condition(Place, Places.Heal).NCondition(Place, Places.NearEnemy);
            foreach (var action in planner.actions)
                action.NCondition(Health, 0);

            planner.minCost = 10;

            State start = new State(typeof(Condition));
            start[Place] = nearEnemy ? (int)Places.NearEnemy : (int)Places.Nowhere;
            start[Health] = (int)myCurrentHP;
            Debug.Log(start);

            planner.Plan(start);
            Debug.Log(planner.GetPathLog(typeof(Condition), showStateChange: true));
        }
        float MoveCost(State current, State next)
        {
            return 1;
        }
        Func<State, State, float> KillCost(Equipment equipment)
        {
            return (State current, State next) =>
            {
                float damageTaken = KillDamage(equipment);
                if (damageTaken > current[Health])
                    return 9999;
                else
                    return damageTaken;
            };
        }
        Action<State> GetReduceHPEffect(float amount)
        {
            return (State current) => { current[Health] = Mathf.Max(current[Health] - (int)amount, 0); };
        }
        float FleeDamage()
        {
            if (mySpeed <= enemySpeed)
                return float.PositiveInfinity;
            float fleeTime = rangedRange / Mathf.Max(mySpeed - enemySpeed, .1f);
            float damageTaken = fleeTime * enemyRangedDPS;
            return damageTaken;
        }
        float KillDamage(Equipment equipment)
        {
            float myDPS = equipment == Equipment.Sword ? myMeleeDPS : myRangedDPS;
            float enemyDPS = equipment == Equipment.Sword ? enemyMeleeDPS : enemyRangedDPS;
            float battleTime = enemyHP / myDPS;
            return enemyDPS * battleTime;
        }
    }

}