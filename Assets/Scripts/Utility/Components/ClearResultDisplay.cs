using System;
using ArcCreate.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    [RequireComponent(typeof(Image))]
    public class ClearResultDisplay : MonoBehaviour
    {
        [SerializeField] private ClearResultSprite[] resultSprites;

        public void Display(ClearResult result)
        {
            var image = GetComponent<Image>();
            foreach (var spr in resultSprites)
                if (spr.Result == result)
                {
                    image.sprite = spr.Sprite;
                    return;
                }
        }

        [Serializable]
        private struct ClearResultSprite
        {
            public ClearResult Result;
            public Sprite Sprite;
        }
    }
}