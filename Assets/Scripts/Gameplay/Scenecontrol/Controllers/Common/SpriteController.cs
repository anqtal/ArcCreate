using EmmySharp;
using MoonSharp.Interpreter;
using UnityEngine;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Controller for a sprite")]
    public class SpriteController : Controller, IPositionController, ILayerController, ITextureController,
        IColorController
    {
        private static readonly int TextureModifyShaderId = Shader.PropertyToID("_Modify");
        [SerializeField] private SpriteRenderer spriteRenderer;
        private ValueChannel alpha;
        private ValueChannel colorA;
        private ValueChannel colorB;
        private ValueChannel colorG;
        private ValueChannel colorH;
        private ValueChannel colorR;
        private ValueChannel colorS;
        private ValueChannel colorV;
        private StringChannel layer;
        private MaterialPropertyBlock mpb;
        private ValueChannel rotationX;
        private ValueChannel rotationY;
        private ValueChannel rotationZ;
        private ValueChannel scaleX;
        private ValueChannel scaleY;
        private ValueChannel scaleZ;
        private ValueChannel sort;
        private ValueChannel textureOffsetX;
        private ValueChannel textureOffsetY;
        private ValueChannel textureScaleX;
        private ValueChannel textureScaleY;
        private ValueChannel translationX;
        private ValueChannel translationY;
        private ValueChannel translationZ;

        [MoonSharpHidden] public SpriteRenderer SpriteRenderer => spriteRenderer;

        [MoonSharpHidden] public Color DefaultColor { get; private set; }

        public ValueChannel ColorR
        {
            get => colorR;
            set
            {
                colorR = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorG
        {
            get => colorG;
            set
            {
                colorG = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorB
        {
            get => colorB;
            set
            {
                colorB = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorH
        {
            get => colorH;
            set
            {
                colorH = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorS
        {
            get => colorS;
            set
            {
                colorS = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorV
        {
            get => colorV;
            set
            {
                colorV = value;
                EnableColorModule = true;
            }
        }

        public ValueChannel ColorA
        {
            get => colorA;
            set
            {
                colorA = value;
                EnableColorModule = true;
            }
        }

        public bool EnableColorModule { get; set; }

        [MoonSharpHidden]
        public virtual void UpdateColor(Color color)
        {
            spriteRenderer.color = color;
        }

        [MoonSharpHidden] public string DefaultLayer { get; private set; }

        [MoonSharpHidden] public int DefaultSort { get; private set; }

        [MoonSharpHidden] public float DefaultAlpha { get; private set; }

        public StringChannel Layer
        {
            get => layer;
            set
            {
                layer = value;
                EnableLayerModule = true;
            }
        }

        public ValueChannel Sort
        {
            get => sort;
            set
            {
                sort = value;
                EnableLayerModule = true;
            }
        }

        public ValueChannel Alpha
        {
            get => alpha;
            set
            {
                alpha = value;
                EnableLayerModule = true;
            }
        }

        public bool EnableLayerModule { get; set; }

        [MoonSharpHidden]
        public void UpdateLayer(string layer, int sort = 0, float alpha = 255f)
        {
            spriteRenderer.sortingLayerName = layer;
            spriteRenderer.sortingOrder = sort;
        }

        [MoonSharpHidden] public Vector3 DefaultTranslation { get; private set; }

        [MoonSharpHidden] public Quaternion DefaultRotation { get; private set; }

        [MoonSharpHidden] public Vector3 DefaultScale { get; private set; }

        public ValueChannel TranslationX
        {
            get => translationX;
            set
            {
                translationX = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel TranslationY
        {
            get => translationY;
            set
            {
                translationY = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel TranslationZ
        {
            get => translationZ;
            set
            {
                translationZ = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel RotationX
        {
            get => rotationX;
            set
            {
                rotationX = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel RotationY
        {
            get => rotationY;
            set
            {
                rotationY = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel RotationZ
        {
            get => rotationZ;
            set
            {
                rotationZ = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel ScaleX
        {
            get => scaleX;
            set
            {
                scaleX = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel ScaleY
        {
            get => scaleY;
            set
            {
                scaleY = value;
                EnablePositionModule = true;
            }
        }

        public ValueChannel ScaleZ
        {
            get => scaleZ;
            set
            {
                scaleZ = value;
                EnablePositionModule = true;
            }
        }

        public bool EnablePositionModule { get; set; }

        [MoonSharpHidden]
        public void UpdatePosition(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            transform.localScale = scale;
            transform.localRotation = rotation;
            transform.localPosition = translation;
        }

        [MoonSharpHidden] public Vector2 DefaultTextureOffset { get; private set; }

        [MoonSharpHidden] public Vector2 DefaultTextureScale { get; private set; }

        public ValueChannel TextureOffsetX
        {
            get => textureOffsetX;
            set
            {
                textureOffsetX = value;
                EnableTextureModule = true;
            }
        }

        public ValueChannel TextureOffsetY
        {
            get => textureOffsetY;
            set
            {
                textureOffsetY = value;
                EnableTextureModule = true;
            }
        }

        public ValueChannel TextureScaleX
        {
            get => textureScaleX;
            set
            {
                textureScaleX = value;
                EnableTextureModule = true;
            }
        }

        public ValueChannel TextureScaleY
        {
            get => textureScaleY;
            set
            {
                textureScaleY = value;
                EnableTextureModule = true;
            }
        }

        public bool EnableTextureModule { get; set; }

        [MoonSharpHidden]
        public void UpdateTexture(Vector2 offset, Vector2 scale)
        {
            var modifyVec = new Vector4(offset.x, offset.y, scale.x, scale.y);
            mpb = mpb ?? new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetVector(TextureModifyShaderId, modifyVec);
            spriteRenderer.SetPropertyBlock(mpb);
        }

        [MoonSharpHidden]
        public override void SetupDefault()
        {
            DefaultColor = spriteRenderer.color;
            DefaultTranslation = transform.localPosition;
            DefaultRotation = transform.localRotation;
            DefaultScale = transform.localScale;
            DefaultLayer = spriteRenderer.sortingLayerName;
            DefaultSort = spriteRenderer.sortingOrder;
            DefaultAlpha = spriteRenderer.color.a;

            var defaultModifyVec = spriteRenderer.material.GetVector(TextureModifyShaderId);
            DefaultTextureOffset = new Vector2(defaultModifyVec.x, defaultModifyVec.y);
            DefaultTextureScale = new Vector2(defaultModifyVec.z, defaultModifyVec.w);

            mpb = mpb ?? new MaterialPropertyBlock();
        }

        [EmmyDoc("Creates a copy of this controller")]
        public virtual SpriteController Copy()
        {
            var c = Instantiate(gameObject, transform.parent).GetComponent<SpriteController>();
            c.spriteRenderer.material = Instantiate(c.spriteRenderer.material);
            c.IsPersistent = false;
            c.SerializedType = SerializedType;
            if (c.SerializedType[0] != '$') c.SerializedType = "$" + c.SerializedType;

            var children = GetChildren();
            var i = 0;
            foreach (var child in c.GetChildren())
            {
                child.IsPersistent = false;
                child.Start();
                child.CopyAllChannelsFrom(children[i]);
                i++;
            }

            c.Start();
            c.CopyAllChannelsFrom(this);
            Services.Scenecontrol.AddReferencedController(c);
            return c;
        }
    }
}