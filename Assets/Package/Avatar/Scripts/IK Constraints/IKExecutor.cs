using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    [DefaultExecutionOrder(-600)]
    public class IKExecutor : MonoBehaviour
    {
        public enum ExecutionTime
        {
            Update,
            LateUpdate,
            UpdateAndLateUpdate,
            AnimatorMove,
            Manual
        };

        public ExecutionTime mode;

        [System.Serializable]
        public class ConstraintEntry
        {
            public IKConstraint constraint;
            [Tooltip("Set to true if this constraint is only used for visual effects so that it may be ignored by things like network rollback code.")]
            public bool astetic;
        }
        public List<ConstraintEntry> constraints = new List<ConstraintEntry>();

        public void AddConstraint(IKConstraint constraint, bool isAstetic = false)
        {
            constraints.Add(new ConstraintEntry
            {
                constraint = constraint,
                astetic = isAstetic
            });
        }

        public void Start()
        {
            Calibrate();
        }

        public void Calibrate()
        {
            foreach (ConstraintEntry e in constraints)
            {
                e.constraint.Calibrate();
            }
        }

        public void Execute(bool executeAsteticConstraints = true)
        {
            foreach(ConstraintEntry e in constraints)
            {
                if (!e.constraint.enabled)
                    continue;
                if (e.astetic && !executeAsteticConstraints)
                    continue;
                e.constraint.Execute();
            }
        }

        private void Update()
        {
            if (mode != ExecutionTime.Update && mode != ExecutionTime.UpdateAndLateUpdate)
                return;
            Execute();
        }

        private void LateUpdate()
        {
            if (mode != ExecutionTime.LateUpdate && mode != ExecutionTime.UpdateAndLateUpdate)
                return;
            Execute();
        }

        public void OnAnimatorMove()
        {
            if (mode != ExecutionTime.AnimatorMove)
                return;
            Execute();
        }
    }

    public abstract class IKConstraint : MonoBehaviour
    {
        [Range(0,1)]
        public float weight = 1;
        public abstract void Execute();

        public virtual void Calibrate() { } 
    }

    public abstract class IKTargetedConstraint : IKConstraint
    {
        public Transform target;
    }
}

