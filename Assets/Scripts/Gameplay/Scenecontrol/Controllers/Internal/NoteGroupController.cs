using ArcCreate.Gameplay.Chart;
using EmmySharp;
using MoonSharp.Interpreter;
using UnityEngine;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Controller for a timing group")]
    public class NoteGroupController : Controller, IPositionController, INoteGroupController, IColorController
    {
        private ValueChannel angleX;
        private ValueChannel angleY;
        private ValueChannel colorA;
        private ValueChannel colorB;
        private ValueChannel colorG;
        private ValueChannel colorH;
        private ValueChannel colorR;
        private ValueChannel colorS;
        private ValueChannel colorV;
        private ValueChannel judgeOffsetX;
        private ValueChannel judgeOffsetY;
        private ValueChannel judgeOffsetZ;
        private ValueChannel judgeSizeX;
        private ValueChannel judgeSizeY;
        private ValueChannel rotationIndividualX;
        private ValueChannel rotationIndividualY;
        private ValueChannel rotationIndividualZ;
        private ValueChannel rotationX;
        private ValueChannel rotationY;
        private ValueChannel rotationZ;
        private ValueChannel scaleIndividualX;
        private ValueChannel scaleIndividualY;
        private ValueChannel scaleIndividualZ;
        private ValueChannel scaleX;
        private ValueChannel scaleY;
        private ValueChannel scaleZ;
        private ValueChannel translationX;
        private ValueChannel translationY;
        private ValueChannel translationZ;

        [MoonSharpHidden] public TimingGroup TimingGroup { get; set; }

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

        [MoonSharpHidden] public Color DefaultColor => Color.white;

        public bool EnableColorModule { get; set; }

        [MoonSharpHidden]
        public void UpdateColor(Color color)
        {
            TimingGroup.GroupProperties.Color = color;
        }

        public ValueChannel AngleX
        {
            get => angleX;
            set
            {
                angleX = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel AngleY
        {
            get => angleY;
            set
            {
                angleY = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel JudgeSizeX
        {
            get => judgeSizeX;
            set
            {
                judgeSizeX = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel JudgeSizeY
        {
            get => judgeSizeY;
            set
            {
                judgeSizeY = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel JudgeOffsetX
        {
            get => judgeOffsetX;
            set
            {
                judgeOffsetX = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel JudgeOffsetY
        {
            get => judgeOffsetY;
            set
            {
                judgeOffsetY = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel JudgeOffsetZ
        {
            get => judgeOffsetZ;
            set
            {
                judgeOffsetZ = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel RotationIndividualX
        {
            get => rotationIndividualX;
            set
            {
                rotationIndividualX = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel RotationIndividualY
        {
            get => rotationIndividualY;
            set
            {
                rotationIndividualY = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel RotationIndividualZ
        {
            get => rotationIndividualZ;
            set
            {
                rotationIndividualZ = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel ScaleIndividualX
        {
            get => scaleIndividualX;
            set
            {
                scaleIndividualX = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel ScaleIndividualY
        {
            get => scaleIndividualY;
            set
            {
                scaleIndividualY = value;
                EnableNoteGroupModule = true;
            }
        }

        public ValueChannel ScaleIndividualZ
        {
            get => scaleIndividualZ;
            set
            {
                scaleIndividualZ = value;
                EnableNoteGroupModule = true;
            }
        }

        public bool EnableNoteGroupModule { get; set; }

        [MoonSharpHidden]
        public void UpdateNoteGroup(Quaternion rotation, Vector3 scale, Vector2 angle, Vector2 judgesize,
            Vector3 judgeoffset)
        {
            TimingGroup.GroupProperties.SCAngleX = angle.x;
            TimingGroup.GroupProperties.SCAngleY = angle.y;
            TimingGroup.GroupProperties.SCJudgementSizeX = judgesize.x;
            TimingGroup.GroupProperties.SCJudgementSizeY = judgesize.y;
            TimingGroup.GroupProperties.SCJudgementOffsetX = judgeoffset.x;
            TimingGroup.GroupProperties.SCJudgementOffsetY = judgeoffset.y;
            TimingGroup.GroupProperties.SCJudgementOffsetZ = judgeoffset.z;
            TimingGroup.GroupProperties.RotationIndividual = rotation;
            TimingGroup.GroupProperties.ScaleIndividual = scale;
        }

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

        [MoonSharpHidden] public Vector3 DefaultTranslation => Vector3.zero;

        [MoonSharpHidden] public Quaternion DefaultRotation => Quaternion.identity;

        [MoonSharpHidden] public Vector3 DefaultScale => Vector3.one;

        public bool EnablePositionModule { get; set; }

        [MoonSharpHidden]
        public void UpdatePosition(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            transform.localPosition = translation;
            transform.localRotation = rotation;
            transform.localScale = scale;
            TimingGroup.GroupProperties.GroupMatrix = Matrix4x4.TRS(translation, rotation, scale);
        }

        protected override void SetActive(bool active)
        {
            base.SetActive(active);
            TimingGroup.GroupProperties.Visible = gameObject.activeInHierarchy;
        }
    }
}