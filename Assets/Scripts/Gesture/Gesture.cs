using System;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using NormalizedLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using UnityEngine;

public abstract class Gesture
{
    public IReadOnlyList<Vector3> FingerPos             = new List<Vector3>();
    public IReadOnlyList<Vector2> FingerPos2D           = new List<Vector2>();
    public IReadOnlyList<NormalizedLandmark> Landmarks  = new List<NormalizedLandmark>();
    public EFingerState FingerState                     = EFingerState.None;

    private const float _OPEN_THRESHOLD         = 400f;
    private const float _OPEN_THRESHOLD_THUMB   = 310f;

    public static bool Get(List<NormalizedLandmark> landmarks, out Gesture gesture)
    {
        gesture = null;

        var wristPosXY = ConvertLandmarkToVector2(landmarks[GestureIndex.WRIST]);

        Func<int, bool> comparer = (fingerIndex) =>
        {
            var finger = GestureIndex.FINGERS[fingerIndex];
            var sum = SumFingerAngle(landmarks, finger);
            if (fingerIndex == GestureIndex.THUMB)  
                return sum > _OPEN_THRESHOLD_THUMB; 
            
            return sum >_OPEN_THRESHOLD;
        };

        var fingerState = (EFingerState)0;

        fingerState |= (comparer(GestureIndex.THUMB)         ? EFingerState.ThumbOpen  : EFingerState.None); 
        fingerState |= (comparer(GestureIndex.INDEX_FINGER)  ? EFingerState.IndexOpen  : EFingerState.None);
        fingerState |= (comparer(GestureIndex.MIDDLE_FINGER) ? EFingerState.MiddleOpen : EFingerState.None);
        fingerState |= (comparer(GestureIndex.RING_FINGER)   ? EFingerState.RingOpen   : EFingerState.None);
        fingerState |= (comparer(GestureIndex.PINKY)         ? EFingerState.PinkyOpen  : EFingerState.None);

        Debug.Assert(landmarks.Count == 21);
        var fingerPos   = ConvertAllLandmarksToVector(landmarks);
        var fingerPos2D = ConvertAllLandmarksToVector2(landmarks);

        GestureInfo info = new()
        {
            FingerState = fingerState,
            FingerPos   = fingerPos,
            FingerPos2D = fingerPos2D,
            Landmarks   = landmarks
        };

        return new IndexOpen(). InitializeIfIsThisGestureElseNull(info, out gesture)
            || new Pinch().     InitializeIfIsThisGestureElseNull(info, out gesture)
            || new Fist().      InitializeIfIsThisGestureElseNull(info, out gesture);
            
    }
    public static Vector3 ConvertLandmarkToVector(NormalizedLandmark landmarks) => new(landmarks.x, landmarks.y, landmarks.z);
    public static Vector2 ConvertLandmarkToVector2(NormalizedLandmark landmark) => new(landmark.x, landmark.y);
    protected static List<Vector3> ConvertAllLandmarksToVector(List<NormalizedLandmark> landmarks)
    {
        var result = new List<Vector3>();
        foreach (var landmark in landmarks)
        {
            result.Add(ConvertLandmarkToVector(landmark));
        }
        return result;
    }
    protected static List<Vector2> ConvertAllLandmarksToVector2(List<NormalizedLandmark> landmarks)
    {
        var result = new List<Vector2>();
        foreach (var landmark in landmarks)
        {
            result.Add(ConvertLandmarkToVector2(landmark));
        }
        return result;
    }
    public Vector3 GetScreenPos(int pixelWidth, int pixelHeight, float sensity = 1.5f)
    {
        var normalized = NormalizedControlPoint();

        // sensity 计算：
        // 1. [0, 1] -> [-1, 1]
        // 2. [-1, 1] -> [-Sensity, Sensity]
        // 3. [-Sensity, Sensity] -> Clamp to [-1, 1]
        // 4. [-1, 1] -> [0, 1]
        Func<float, float> computer = (x) =>
        {
            x = Mathf.Clamp01(x) * 2 - 1;
            x *= sensity;
            x = Mathf.Clamp(x, -1, 1);
            x = x * 0.5f + 0.5f;
            return x;
        };

        var x = computer(normalized.x);
        var y = computer(1 - normalized.y);

        return new Vector3(x * pixelWidth, y * pixelHeight, 0);
    }
    public abstract Vector2 NormalizedControlPoint();
    protected abstract bool IsThisGesture(GestureInfo info);
    protected static float AngleBetween(Vector3 j1Pos, Vector3 j2Pos, Vector3 j3Pos)
    {
        var v1 = j1Pos - j2Pos;
        var v2 = j3Pos - j2Pos;

        var cos = Vector3.Dot(v1.normalized, v2.normalized);
        return Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
    }

    protected static float SumFingerAngle(IReadOnlyList<Vector3> fingerPos, int[] joints)
    {
        var sum = 0f;
        for (var i = 0; i < joints.Length - 2; i++)
        {
            sum += AngleBetween(fingerPos[joints[i]], fingerPos[joints[i + 1]], fingerPos[joints[i + 2]]);
        }
        return sum;
    }
    protected static float SumFingerAngle(IReadOnlyList<NormalizedLandmark> fingerPos, int[] joints)
    {
        var sum = 0f;
        for (var i = 0; i < joints.Length - 2; i++)
        {
            sum += AngleBetween(ConvertLandmarkToVector(fingerPos[joints[i]]), ConvertLandmarkToVector(fingerPos[joints[i + 1]]), ConvertLandmarkToVector(fingerPos[joints[i + 2]]));
        }
        return sum;
    }
    private bool InitializeIfIsThisGestureElseNull(GestureInfo info, out Gesture gesture)
    {
        gesture = null;

        if (IsThisGesture(info))
        {
            Landmarks   = info.Landmarks;
            FingerState = info.FingerState;
            FingerPos   = info.FingerPos;
            FingerPos2D = info.FingerPos2D;
            gesture     = this;
            return true;
        }

        return false;
    }

    public struct GestureInfo
    {
        public IReadOnlyList<Vector3> FingerPos;
        public IReadOnlyList<Vector2> FingerPos2D;
        public IReadOnlyList<NormalizedLandmark> Landmarks;
        public EFingerState FingerState;
    }

    [System.Flags]
    public enum EFingerState
    {
        None = 0,

        ThumbOpen   = 1,
        IndexOpen   = 1 << 1,
        MiddleOpen  = 1 << 2,
        RingOpen    = 1 << 3,
        PinkyOpen   = 1 << 4,
    }
}

public class Fist : Gesture
{
    // 四根手指的弯曲度需要小于 340 我拿我自己的手测的
    private const float _THRESHOLD = 340f;
    public override Vector2 NormalizedControlPoint()
    {
        var middlePip = FingerPos2D[GestureIndex.MIDDLE_FINGER_PIP];
        var wrist     = FingerPos2D[GestureIndex.WRIST];

        return (middlePip + wrist) / 2;
    }

    public override string ToString() => "Gesture:Fist";

    protected override bool IsThisGesture(GestureInfo info)
    {
        int[][] fingers = GestureIndex.FINGERS;

        // 检查四根手指（不包括拇指）的弯曲度是否足够大
        for (var fJoints = 1; fJoints < fingers.Length; fJoints++)
        {
            if (SumFingerAngle(info.FingerPos, fingers[fJoints]) > _THRESHOLD) return false;
        }
        return true;
    }
}

public class IndexOpen : Gesture
{
    // 食指累积夹角阈值，越大越伸直
    private const float _THRESHOLD = 500f;
    // 与拇指距离以区分 Pinch
    private const float _MIN_PINCH_DIST = 0.05f;
    public override Vector2 NormalizedControlPoint()
    {
        if (Landmarks.Count > 8)
            return ConvertLandmarkToVector2(Landmarks[GestureIndex.INDEX_FINGER_TIP]);
        if (Landmarks.Count > 6)
            return ConvertLandmarkToVector2(Landmarks[GestureIndex.INDEX_FINGER_PIP]);
        return Vector2.zero;
    }
    public override string ToString() => "Gesture:IndexOpen";

    protected override bool IsThisGesture(GestureInfo info)
    {
        // Flag 初步判断 确保张开
        if (!info.FingerState.HasFlag(EFingerState.IndexOpen))
            return false;

        if (info.FingerState.HasFlag(EFingerState.MiddleOpen)
          || info.FingerState.HasFlag(EFingerState.RingOpen)
          || info.FingerState.HasFlag(EFingerState.PinkyOpen))
        {
            return false;
        }

        var indexCurl = SumFingerAngle(info.FingerPos, GestureIndex.FINGERS[GestureIndex.INDEX_FINGER]);
        if (indexCurl < _THRESHOLD)
            return false;

        var thumbTip = info.FingerPos2D[GestureIndex.THUMB_TIP];
        var indexTip = info.FingerPos2D[GestureIndex.INDEX_FINGER_TIP];
        if (Vector2.Distance(thumbTip, indexTip) < _MIN_PINCH_DIST)
            return false;

        return true;
    }
}

public class Pinch : Gesture
{
    private const float _THRESHOLD = 0.05f;
    public override Vector2 NormalizedControlPoint()
    {
        var thumbTip = FingerPos2D[GestureIndex.THUMB_TIP];
        var indexTip = FingerPos2D[GestureIndex.INDEX_FINGER_TIP];

        return (thumbTip + indexTip) / 2;
    }
    public override string ToString() => "Gesture:Pinch";

    protected override bool IsThisGesture(GestureInfo info)
    {
        // 确保张开
        if (!info.FingerState.HasFlag(EFingerState.IndexOpen) || !info.FingerState.HasFlag(EFingerState.ThumbOpen))
            return false;

        var thumbTipPos = info.FingerPos2D[GestureIndex.THUMB_TIP];
        var indexTipPos = info.FingerPos2D[GestureIndex.INDEX_FINGER_TIP];

        return Vector2.Distance(thumbTipPos, indexTipPos) < _THRESHOLD;
    }
}

public struct GestureIndex
{
    public const int WRIST      = 0;

    public const int THUMB_CMC      = 1;
    public const int THUMB_MCP      = 2;
    public const int THUMB_IP       = 3;
    public const int THUMB_TIP      = 4;

    public const int INDEX_FINGER_PIP   = 6;
    public const int INDEX_FINGER_TIP   = 8;

    public const int MIDDLE_FINGER_PIP  = 10;
    public const int MIDDLE_FINGER_TIP  = 12;

    public const int RING_FINGER_PIP    = 14;
    public const int RING_FINGER_TIP    = 16;

    public const int PINKY_PIP      = 18;
    public const int PINKY_TIP      = 20;

    public const int THUMB          = 0;
    public const int INDEX_FINGER   = 1;
    public const int MIDDLE_FINGER  = 2;
    public const int RING_FINGER    = 3;
    public const int PINKY          = 4;

    public static readonly int[][] FINGERS =
    {
        new[]{4,  3,  2,  1},       // 拇指
        new[]{8,  7,  6,  5,  0},   // 食指
        new[]{12, 11, 10, 9,  0},   // 中指
        new[]{16, 15, 14, 13, 0},   // 无名指
        new[]{20, 19, 18, 17, 0}    // 小拇指
    };

}


