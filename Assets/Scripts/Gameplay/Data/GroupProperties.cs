using System.Collections.Generic;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay.Judgement;
using ArcCreate.Gameplay.Skin;
using UnityEngine;

namespace ArcCreate.Gameplay.Data
{
    public class GroupProperties
    {
        public GroupProperties()
        {
        }

        public GroupProperties(RawTimingGroup raw)
        {
            Name = raw.Name;
            FileName = raw.File;
            SkinOverride = (NoteSkinOverride)(int)raw.Side;
            FadingHolds = raw.FadingHolds;
            IgnoreMirror = raw.IgnoreMirror;
            NoInput = raw.NoInput;
            NoClip = raw.NoClip;
            NoHeightIndicator = raw.NoHeightIndicator;
            NoHead = raw.NoHead;
            NoShadow = raw.NoShadow;
            NoArcCap = raw.NoArcCap;
            NoConnection = raw.NoConnection;
            AngleX = raw.AngleX;
            AngleY = raw.AngleY;
            JudgementOffsetX = raw.JudgementOffsetX;
            JudgementOffsetY = raw.JudgementOffsetY;
            JudgementOffsetZ = raw.JudgementOffsetZ;
            JudgementSizeX = raw.JudgementSizeX;
            JudgementSizeY = raw.JudgementSizeY;
            ArcResolution = raw.ArcResolution;
            Editable = raw.Editable;
            Autoplay = raw.Autoplay;
            foreach (var pair in raw.JudgementMaps)
                JudgementMaps.Add((JudgementResult)(int)pair.Key, (JudgementResult)(int)pair.Value);
        }

        public string Name { get; set; }

        public string FileName { get; set; }

        public bool Editable { get; set; } = true;

        public NoteSkinOverride SkinOverride { get; set; } = NoteSkinOverride.Default;

        public Color Color { get; set; } = Color.white;

        public Vector3 ScaleIndividual { get; set; } = Vector3.one;

        public Quaternion RotationIndividual { get; set; } = Quaternion.identity;

        public bool NoInput { get; set; }

        public bool NoClip { get; set; }

        public bool NoHeightIndicator { get; set; }

        public bool NoHead { get; set; }

        public bool NoShadow { get; set; }

        public bool NoArcCap { get; set; }

        public bool NoConnection { get; set; }

        public bool FadingHolds { get; set; }

        public bool IgnoreMirror { get; set; }

        public bool Autoplay { get; set; }

        public float AngleX { get; set; }

        public float AngleY { get; set; }

        public float JudgementSizeX { get; set; } = 1;

        public float JudgementSizeY { get; set; } = 1;

        public float JudgementOffsetX { get; set; }

        public float JudgementOffsetY { get; set; }

        public float JudgementOffsetZ { get; set; }

        public float ArcResolution { get; set; } = 1;

        public float SCAngleX { get; set; } = 0;

        public float SCAngleY { get; set; } = 0;

        public float SCJudgementSizeX { get; set; } = 1;

        public float SCJudgementSizeY { get; set; } = 1;

        public float SCJudgementOffsetX { get; set; } = 0;

        public float SCJudgementOffsetY { get; set; } = 0;

        public float SCJudgementOffsetZ { get; set; } = 0;

        public Matrix4x4 GroupMatrix { get; set; } = Matrix4x4.identity;

        public bool Visible { get; set; } = true;

        public Dictionary<JudgementResult, JudgementResult> JudgementMaps { get; } = new();

        public Vector3 FallDirection
        {
            get
            {
                var angleXf = 90.0f - AngleX - SCAngleX;
                var angleYf = (AngleY + SCAngleY) * (Settings.MirrorNotes.Value ? -1 : 1);

                var x = Mathf.Sin(angleXf * Mathf.Deg2Rad) * Mathf.Sin(angleYf * Mathf.Deg2Rad);
                var y = -Mathf.Cos(angleXf * Mathf.Deg2Rad);
                var z = Mathf.Sin(angleXf * Mathf.Deg2Rad) * Mathf.Cos(angleYf * Mathf.Deg2Rad);
                return new Vector3(x, y, z);
            }
        }

        public Vector2 CurrentJudgementSize =>
            new(JudgementSizeX * SCJudgementSizeX, JudgementSizeY * SCJudgementSizeY);

        public Vector3 CurrentJudgementOffset => new(
            JudgementOffsetX + SCJudgementOffsetX,
            JudgementOffsetY + SCJudgementOffsetY,
            JudgementOffsetZ + SCJudgementOffsetZ);

        public RawTimingGroup ToRaw()
        {
            var rtg = new RawTimingGroup
            {
                Name = Name,
                File = FileName,
                Side = (SideOverride)(int)SkinOverride,
                FadingHolds = FadingHolds,
                NoInput = NoInput,
                NoHeightIndicator = NoHeightIndicator,
                NoHead = NoHead,
                NoShadow = NoShadow,
                NoClip = NoClip,
                NoArcCap = NoArcCap,
                NoConnection = NoConnection,
                AngleX = AngleX,
                AngleY = AngleY,
                JudgementOffsetX = JudgementOffsetX,
                JudgementOffsetY = JudgementOffsetY,
                JudgementOffsetZ = JudgementOffsetZ,
                JudgementSizeX = JudgementSizeX,
                JudgementSizeY = JudgementSizeY,
                ArcResolution = ArcResolution,
                Autoplay = Autoplay,
                IgnoreMirror = IgnoreMirror
            };

            foreach (var pair in JudgementMaps)
                rtg.JudgementMaps.Add((JudgementMap)(int)pair.Key, (JudgementMap)(int)pair.Value);

            return rtg;
        }

        public JudgementResult MapJudgementResult(JudgementResult from)
        {
            var inputMode = (InputMode)Settings.InputMode.Value;
            var isAuto = inputMode == InputMode.Auto || inputMode == InputMode.AutoController;
            if (isAuto || Autoplay) return JudgementResult.Max;

            if (JudgementMaps.TryGetValue(from, out var to)) return to;

            return from;
        }
    }
}