using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class PlayerAimTests {
    [TestCase(0f, 0f, 0f, 0f)]
    [TestCase(3f, 343f, -2f, 0f)]
    [TestCase(-7f, 118f, 5f, .08f)]
    public void OffsetEyeAimsAtControlWithoutLeavingSeatFrame(float tilt, float yaw, float roll, float side) {
        var seat = new GameObject("Aim test seat");
        try {
            seat.transform.SetPositionAndRotation(new Vector3(118, .1f, 16), Quaternion.Euler(tilt, yaw, roll));
            var body = new GameObject("Aim test body");body.transform.SetParent(seat.transform, false);
            body.AddComponent<CharacterController>();
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PlayerMotor")).First(t => t != null);
            var motor = body.AddComponent(type);
            var cameraObject = new GameObject("Aim test eye");cameraObject.transform.SetParent(body.transform, false);
            var eye = cameraObject.AddComponent<Camera>();eye.enabled = false;
            eye.transform.localPosition = new Vector3(side, .72f, .05f);type.GetField("eye").SetValue(motor, eye);
            foreach (float angle in new[] { -145f, 75f, 170f }) {
                body.transform.localRotation = Quaternion.Euler(0, angle, 0);
                var target = seat.transform.TransformPoint(new Vector3(.25f, .12f, .50f));
                type.GetMethod("Aim").Invoke(motor, new object[] { target });
                Assert.Less(Vector3.Angle(eye.transform.forward, target-eye.transform.position), .05f, "Actual eye ray misses the control after turning");
                Assert.Less(Vector3.Angle(body.transform.up, seat.transform.up), .05f, "Aiming erased the vehicle's tilt");
            }
        } finally { UnityEngine.Object.DestroyImmediate(seat); }
    }
}
