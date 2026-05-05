using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BLINK
{
    public class AnimationDemo : MonoBehaviour
    {

        public enum AnimationType
        {
            Trigger,
            Bool
        }

        [System.Serializable]
        public class AnimationEntry
        {
            public string animationName;
            public AnimationType type;
        }

        public List<AnimationEntry> entries = new List<AnimationEntry>();

        public List<Animator> animators = new List<Animator>();

        public int entryIndex;
        public Text animationNameText;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                NextAnimation();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ReplayAnimation();
            }
        }

        public void NextAnimation()
        {
            entryIndex++;
            if (entries.Count - 1 < entryIndex) entryIndex = 0;
            animationNameText.text = entries[entryIndex].animationName;
            PlayAnimation();
        }

        public void PreviousAnimation()
        {
            entryIndex--;
            if (entryIndex < 0) entryIndex = entries.Count - 1;
            animationNameText.text = entries[entryIndex].animationName;
            PlayAnimation();
        }

        public void ReplayAnimation()
        {
            PlayAnimation();
        }

        private void ResetAllBool()
        {
            foreach (var entry in entries)
            {
                if (entry == null || entry.type != AnimationType.Bool || string.IsNullOrEmpty(entry.animationName))
                    continue;
                foreach (var animator in animators)
                {
                    if (animator == null)
                        continue;
                    animator.SetBool(entry.animationName, false);
                }
            }
        }

        private void PlayAnimation()
        {
            if (entries == null || entries.Count == 0 || animators == null)
                return;

            ResetAllBool();

            var current = entries[entryIndex];
            if (current == null || string.IsNullOrEmpty(current.animationName))
                return;

            if (current.type == AnimationType.Bool)
            {
                foreach (var animator in animators)
                {
                    if (animator == null)
                        continue;
                    animator.SetBool(current.animationName, true);
                }
            }
            else
            {
                foreach (var animator in animators)
                {
                    if (animator == null || animator.runtimeAnimatorController == null)
                        continue;
                    animator.SetTrigger(current.animationName);
                }
            }
        }
    }
}
