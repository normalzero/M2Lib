﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using m2lib_csharp.interfaces;
using m2lib_csharp.types;

namespace m2lib_csharp.m2
{
    public class M2Bone : IAnimated
    {
        [Flags]
        public enum BoneFlags
        {
            SphericalBillboard = 0x8,
            CylindricalBillboardLockX = 0x10,
            CylindricalBillboardLockY = 0x20,
            CylindricalBillboardLockZ = 0x40,
            Transformed = 0x200,
            KinematicBone = 0x400, // MoP+: allow physics to influence this bone
            HelmetAnimScaled = 0x1000 // set blend_modificator to helmetAnimScalingRec.m_amount for this bone
        }

        private readonly ushort[] _unknown = new ushort[2];
        public int KeyBoneId { get; set; } = -1;
        public BoneFlags Flags { get; set; } = 0;
        public short ParentBone { get; set; } = -1;
        public ushort SubmeshId { get; set; }
        public M2Track<C3Vector> Translation { get; set; } = new M2Track<C3Vector>();
        public M2Track<C4Quaternion> Rotation { get; set; } = new M2Track<C4Quaternion>();
        public M2Track<C3Vector> Scale { get; set; } = new M2Track<C3Vector>();
        public C3Vector Pivot { get; set; } = new C3Vector();

        private M2Track<CompQuat> _compressedRotation; 

        public void Load(BinaryReader stream, M2.Format version)
        {
            Debug.Assert(version != M2.Format.Useless);
            KeyBoneId = stream.ReadInt32();
            Flags = (BoneFlags) stream.ReadUInt32();
            ParentBone = stream.ReadInt16();
            SubmeshId = stream.ReadUInt16();
            if (version > M2.Format.Classic)
            {
                _unknown[0] = stream.ReadUInt16();
                _unknown[1] = stream.ReadUInt16();
            }
            Translation.Load(stream, version);
            if (version > M2.Format.Classic)
            {
                _compressedRotation = new M2Track<CompQuat>();
                _compressedRotation.Load(stream, version);
            }
            else
                Rotation.Load(stream, version);
            Scale.Load(stream, version);
            Pivot.Load(stream, version);
        }

        public void Save(BinaryWriter stream, M2.Format version)
        {
            Debug.Assert(version != M2.Format.Useless);
            stream.Write(KeyBoneId);
            stream.Write((uint) Flags);
            stream.Write(ParentBone);
            stream.Write(SubmeshId);
            if (version > M2.Format.Classic)
            {
                stream.Write(_unknown[0]);
                stream.Write(_unknown[1]);
            }
            Translation.Save(stream, version);
            if (version > M2.Format.Classic)
            {
                _compressedRotation = new M2Track<CompQuat>();
                _compressedRotation = Rotation.Compress();
                _compressedRotation.Save(stream, version);
            }
            else
                Rotation.Save(stream, version);
            Scale.Save(stream, version);
            Pivot.Save(stream, version);
        }

        public void LoadContent(BinaryReader stream, M2.Format version)
        {
            Debug.Assert(version != M2.Format.Useless);
            Translation.LoadContent(stream, version);
            if (version > M2.Format.Classic)
            {
                _compressedRotation.LoadContent(stream, version);
                Rotation = _compressedRotation.Decompress();
            }
            else
                Rotation.LoadContent(stream, version);
            Scale.LoadContent(stream, version);
        }

        public void SaveContent(BinaryWriter stream, M2.Format version)
        {
            Debug.Assert(version != M2.Format.Useless);
            Translation.SaveContent(stream, version);
            if (version > M2.Format.Classic)
            {
                _compressedRotation.SaveContent(stream, version);
            }
            else
                Rotation.SaveContent(stream, version);
            Scale.SaveContent(stream, version);
        }

        /// <summary>
        ///     Pass the sequences reference to Tracks so they can : switch between 1 timeline & multiple timelines, open .anim
        ///     files...
        /// </summary>
        /// <param name="sequences"></param>
        public void SetSequences(IReadOnlyList<M2Sequence> sequences)
        {
            Translation.SequenceBackRef = sequences;
            Rotation.SequenceBackRef = sequences;
            Scale.SequenceBackRef = sequences;
        }

        public static M2Array<short> GenerateKeyBoneLookup(M2Array<M2Bone> bones)
        {
            var lookup = new M2Array<short>();
            var maxId = bones.Max(x => x.KeyBoneId);
            for (short i = 0; i < maxId + 1; i++) lookup.Add(-1);
            for (short i = 0; i < bones.Count; i++)
            {
                var id = bones[i].KeyBoneId;
                if (lookup[id] == -1) lookup[id] = i;
            }
            return lookup;
        }
    }
}