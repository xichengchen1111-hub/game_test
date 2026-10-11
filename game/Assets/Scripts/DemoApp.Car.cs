using UnityEngine;

// Boarding, seats, exits and HUD for the hotel car. Driving input lives in HotelCar.
public partial class DemoApp {
    HotelCar seatedCar;
    CarSeatPoint carSeat;
    Vector3 carEyeStart;
    float carEyeBlend = 1;
    string carStatus;
    float carStatusUntil;
    public HotelCar SeatedCar => seatedCar;
    public CarSeatPoint CarSeat => carSeat;
    public bool CarSeated => seatedCar;
    public bool CarSeatIsDriver => carSeat && carSeat.driver;
    // Hands stay on the wheel while the driver is moving or the hand brake is off.
    bool CarHandsBusy => CarSeatIsDriver && seatedCar && (!seatedCar.HandbrakeOn || Mathf.Abs(seatedCar.SignedSpeed) > HotelCar.StopSpeed);
    bool HeldItemBlockedByDriving() {
        if (!CarHandsBusy) return false;
        CarMessage("双手握着方向盘——先停车、拉起手刹（空格）再拿东西。");
        return true;
    }
    void CarMessage(string text) { carStatus = text; carStatusUntil = Time.unscaledTime + 3.2f; }

    public void EnterCar(HotelCar car, CarSeatPoint seat) {
        if (!car || !seat || Blocked || RailSeated || FurnitureSeated || railTravelling || CarSeated) return;
        // A physical reach check, like the other seats.
        if (Vector3.Distance(player.eye.transform.position, seat.transform.position + Vector3.up * .5f) > 3.4f) return;
        SetMapOpen(false); SetWatchOpen(false);
        var eyeWorld = player.eye.transform.position;
        seatedCar = car; carSeat = seat;
        player.Mount(seat.transform);
        carEyeStart = player.transform.InverseTransformPoint(eyeWorld);
        carEyeBlend = 0;
        car.CloseDoorAfterBoarding(seat);
        location = "青川 · 旅馆用车"; locationAge = Time.time;
        Save(); CursorMode();
        CarMessage(seat.driver ? car.EngineRunning ? "W 前进  S 刹车／倒车  A／D 转向  空格 手刹" : "I 打开点火开关，按住 R 踩起动钮；冷车先按 C 拉出阻风门。" : "坐好了。");
        Debug.Log("CAR_BOARDED: " + seat.seatId);
    }
    public void MoveCarSeat(CarSeatPoint seat) {
        if (!CarSeated || !seat || seat == carSeat || Blocked) return;
        var eyeWorld = player.eye.transform.position;
        carSeat = seat; player.Mount(seat.transform);
        carEyeStart = player.transform.InverseTransformPoint(eyeWorld); carEyeBlend = 0;
        Save();
    }
    public bool ExitCar() {
        if (!CarSeated || Blocked) return false;
        if (Mathf.Abs(seatedCar.SignedSpeed) > HotelCar.StopSpeed && !seatedCar.Parked && !seatedCar.FrozenByUi) { CarMessage("停稳后才能下车。"); return false; }
        if (!seatedCar.FindExit(carSeat, out var feet, out var door)) { CarMessage("两侧车门外都被挡住了，暂时下不了车。"); return false; }
        if (carSeat.driver) seatedCar.ApplyHandbrakeForExit();
        seatedCar.OpenDoorForExit(door);
        float yaw = player.transform.eulerAngles.y;
        string seatId = carSeat.seatId;
        seatedCar = null; carSeat = null; carEyeBlend = 1;
        player.Dismount(transform, feet, yaw);
        location = "青川 · 小镇广场"; locationAge = Time.time;
        Save(); CursorMode();
        Debug.Log("CAR_EXIT: " + seatId + " feet=" + feet);
        return true;
    }
    public void CarRecovered() { Debug.LogWarning("CAR_RECOVERED: car left the valid town area and returned to its parking place"); }
    void RestoreCarSeat() {
        if (string.IsNullOrEmpty(state.carSeatId)) return;
        var car = HotelCar.Current;
        var seat = car ? car.Seat(state.carSeatId) : null;
        if (!seat || RailSeated || FurnitureSeated || CurrentScene != "Town") { state.carSeatId = ""; return; }
        seatedCar = car; carSeat = seat;
        player.Mount(seat.transform); player.eye.transform.localPosition = seat.eyeLocal; carEyeBlend = 1;
        location = "青川 · 旅馆用车";
        Debug.Log("CAR_SEAT_RESTORED: " + seat.seatId);
    }
    // The saved feet are a clear standing point beside the car, so a missing seat restores safely.
    Vector3 CarStandingPoint() {
        if (seatedCar.FindExit(carSeat, out var feet, out _)) return feet;
        return seatedCar.transform.position + Vector3.ProjectOnPlane(seatedCar.transform.right, Vector3.up).normalized * -2f;
    }
    void TickCar() {
        if (!CarSeated) return;
        if (carEyeBlend < 1) {
            carEyeBlend = Mathf.MoveTowards(carEyeBlend, 1, Time.deltaTime / .45f);
            player.eye.transform.localPosition = Vector3.Lerp(carEyeStart, carSeat.eyeLocal, Mathf.SmoothStep(0, 1, carEyeBlend));
        }
        if (CarSeatIsDriver && Mathf.Abs(seatedCar.SignedSpeed) > HotelCar.StopSpeed) { if (mapOpen) SetMapOpen(false); if (watch) SetWatchOpen(false); }
    }
    string CarHelpLine() {
        if (CarSeatIsDriver)
            return "W／S 油门·刹车（停住后 S 倒车）  A／D 转向  空格 手刹  I 点火  按住R 起动  C 阻风门  L 车灯  按住H 喇叭  J 雨刷  E 互动";
        return "鼠标 观察    E 互动    Q 怀表    T 车票    M 纸地图    Tab 调查簿    Esc 暂停";
    }
    void DrawCarStatus() {
        var car = HotelCar.Current;
        string message = Time.unscaledTime < carStatusUntil ? carStatus : null;
        if (car && car.Feedback != null) message = car.Feedback;
        if (CarSeated && !Blocked && !mapOpen && message == null) {
            if (focus == null) message = Mathf.Abs(seatedCar.SignedSpeed) > HotelCar.StopSpeed ? null : "E 下车";
            if (CarSeatIsDriver) {
                string state = (seatedCar.EngineRunning ? "发动机运转" : seatedCar.Cranking ? "起动中" : seatedCar.Ignition ? "点火已开 · 熄火" : "熄火") + "  ·  " + seatedCar.GearName +
                    (seatedCar.HandbrakeOn ? "  ·  手刹拉起" : "") + "  ·  " + Mathf.RoundToInt(Mathf.Abs(seatedCar.SignedSpeed) * 3.6f) + " 公里/时";
                var info = new Rect(W - 470, H - 150, 430, 40);
                Box(info, new Color(.04f, .08f, .11f, .78f)); Label(info, state, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }
        }
        if (message == null || Blocked || mapOpen) return;
        var rect = new Rect(W / 2 - 360, H - 104, 720, 46);
        Box(rect, new Color(.04f, .08f, .11f, .9f)); Label(rect, message, button);
    }
}
