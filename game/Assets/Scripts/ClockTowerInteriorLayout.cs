using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One metre-scale definition shared by the shell, interior and runtime walk review.</summary>
public sealed class ClockTowerInteriorLayout : MonoBehaviour {
    public const float ExteriorWidth = 3.6f;
    public const float InteriorWidth = 3.05f;
    public const float ExteriorHeight = 20f;
    public const float InnerWallHeight = 18.5f;
    public const float DialHeight = 16.7f;
    public const float DialDiameter = 3.40f;
    public const float ChamberExteriorWidth = 4.8f;
    public const float ChamberInteriorWidth = 4.25f;
    public const float MechanismX = -1.72f;
    public const float DialPlaneDistance = 2.46f;
    public const float GroundFloor = .12f;
    public const float MiddleFloor = 8.530435f;
    public const float MachineFloor = 15f;
    public const float DoorWidth = 1.1f;
    public const float DoorHeight = 2.15f;
    public const float DoorLocalZ = -1.8f;
    public const int StepCount = 92;
    public const int MiddleStepCount = 52;
    public const int MiddleLandingSegments = 5;
    public const int StepsPerTurn = 20;
    public const float InnerRadius = .28f;
    public const float OuterRadius = 1.40f;
    public const float WalkingRadius = .94f;
    public const float TreadThickness = .11f;
    public const float StartAngle = -90f;
    public const int OpenHeadroomSteps = 15;
    public static readonly Vector3 TownPosition = new Vector3(0, 0, 10);
    public static float Riser => (MiddleFloor - GroundFloor) / MiddleStepCount;
    public static float UpperRiser => (MachineFloor - MiddleFloor) / (StepCount - MiddleStepCount - MiddleLandingSegments);
    public static float MaximumRiser => Mathf.Max(Riser, UpperRiser);
    public static float StepAngle => 360f / StepsPerTurn;
    public static float EndAngle => StartAngle + StepCount * StepAngle;
    public static float MiddleEndAngle => StartAngle + MiddleStepCount * StepAngle;
    public static float MiddleExitAngle => MiddleEndAngle + MiddleLandingSegments * StepAngle;
    public static float OpeningStartAngle => EndAngle - OpenHeadroomSteps * StepAngle;
    public static float TurnHeadroom {
        get {
            float clearance = float.PositiveInfinity;
            for (int i = 0; i < StepCount - StepsPerTurn; i++)
                clearance = Mathf.Min(clearance, StepTop(i + StepsPerTurn) - StepTop(i) - TreadThickness);
            return clearance;
        }
    }
    public static float MiddleDeckHeadroom => MiddleFloor - StepTop(MiddleStepCount - OpenHeadroomSteps - 1) - TreadThickness;
    public static float MachineDeckHeadroom => MachineFloor - StepTop(StepCount - OpenHeadroomSteps - 1) - TreadThickness;
    public static float DeckHeadroom => Mathf.Min(MiddleDeckHeadroom, MachineDeckHeadroom);
    public static float ClearStairWidth => 1.35f - .32f - .05f;

    [Tooltip("Local-space feet positions. These are review targets, never teleport destinations.")]
    public Vector3[] ascendingPath;
    public Vector3 machineStandingPoint = new Vector3(-.64f, MachineFloor, 0);
    public Vector3 mechanismLookPoint = new Vector3(MechanismX + .21f, MachineFloor + 1.05f, 0);
    public Vector3 footprintLookPoint;
    public Vector3 middleStandingPoint;
    public Vector3 middleLookPoint;
    public int middleLandingPathIndex;
    public Transform machinery;
    public MeshCollider staircaseCollider;
    public MeshCollider platformCollider;
    public MeshCollider middlePlatformCollider;
    public int footprintCount;

    public static Vector3 Polar(float angleDegrees, float radius, float height) {
        float a = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
    }

    /// <summary>Continuous height at an angular step boundary; includes the flat middle landing.</summary>
    public static float PositionHeight(float completedSegments) {
        float progress = Mathf.Clamp(completedSegments, 0, StepCount);
        if (progress <= MiddleStepCount) return GroundFloor + progress * Riser;
        if (progress <= MiddleStepCount + MiddleLandingSegments) return MiddleFloor;
        return MiddleFloor + (progress - MiddleStepCount - MiddleLandingSegments) * UpperRiser;
    }

    /// <summary>Top of a zero-based stair segment. Five middle segments share the landing height.</summary>
    public static float StepTop(int segmentIndex) => PositionHeight(segmentIndex + 1);
    public static bool IsMiddleLandingSegment(int segmentIndex) =>
        segmentIndex >= MiddleStepCount && segmentIndex < MiddleStepCount + MiddleLandingSegments;

    public static void ValidateDimensions() {
        if (OuterRadius * 2 > InteriorWidth - .20f)
            throw new InvalidOperationException("Clock tower stairs need at least 3.0m clear interior width.");
        if (MaximumRiser > .19f || ClearStairWidth < .8f || TurnHeadroom < 2.25f || DeckHeadroom < 2.25f)
            throw new InvalidOperationException("Clock tower stair rise, width or standing headroom is unsafe.");
        if (DoorWidth < .90f || DoorHeight < 2.05f)
            throw new InvalidOperationException("Clock tower door opening is too small for the player capsule.");
        if (Mathf.Abs(PositionHeight(MiddleStepCount) - MiddleFloor) > .0001f ||
            Mathf.Abs(PositionHeight(StepCount) - MachineFloor) > .0001f ||
            MiddleLandingSegments * StepAngle < 90f)
            throw new InvalidOperationException("Clock tower middle landing or final height is inconsistent.");
        for (int i = 0; i < StepCount; i++) {
            float rise = StepTop(i) - PositionHeight(i);
            if (rise < -.0001f || rise > .19f)
                throw new InvalidOperationException("Clock tower segment has an invalid rise: " + i);
        }
    }

    public void PopulateReviewPath() {
        var path = new List<Vector3> {
            new Vector3(0, GroundFloor, DoorLocalZ - .55f),
            new Vector3(0, GroundFloor, -1.40f),
            Polar(StartAngle - 3, WalkingRadius, GroundFloor)
        };
        machineStandingPoint = new Vector3(-.64f, MachineFloor, 0);
        mechanismLookPoint = new Vector3(MechanismX + .21f, MachineFloor + 1.05f, 0);
        middleStandingPoint = Polar(MiddleEndAngle + 45f, .67f, MiddleFloor);
        middleLookPoint = new Vector3(.55f, MiddleFloor + .25f, -.65f);
        for (int i = 0; i < StepCount; i++) {
            Vector3 point = Polar(StartAngle + (i + .5f) * StepAngle, WalkingRadius, StepTop(i));
            path.Add(point);
            if (i == MiddleStepCount + 2) {
                // Walk into the flat west platform, stop there, then rejoin the
                // ascending route. The review must exercise an actual middle floor.
                middleLandingPathIndex = path.Count;
                path.Add(middleStandingPoint);
                path.Add(point);
            }
        }
        path.Add(Polar(EndAngle + 9, WalkingRadius, MachineFloor));
        path.Add(new Vector3(-.65f, MachineFloor, .55f));
        path.Add(new Vector3(-.65f, MachineFloor, .25f));
        path.Add(machineStandingPoint);
        ascendingPath = path.ToArray();
        footprintLookPoint = Polar(StartAngle + 3.5f * StepAngle, WalkingRadius,
            StepTop(3) + .004f);
    }

    public Vector3[] WorldAscendingPath() {
        var result = new Vector3[ascendingPath.Length];
        for (int i = 0; i < result.Length; i++) result[i] = transform.TransformPoint(ascendingPath[i]);
        return result;
    }
}
