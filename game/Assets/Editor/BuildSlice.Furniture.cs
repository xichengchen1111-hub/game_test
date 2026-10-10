using System;
using System.Linq;
using UnityEngine;

public static partial class BuildSlice {
    static FurnitureSeat RestSeat(Transform parent,string id,string caption,Vector3 feet,Vector3 facing,Vector3 exit,float eyeHeight,Vector3 targetSize,float targetY) {
        var root=Group("Rest seat "+id,feet+Vector3.up*targetY,parent);
        var collider=root.AddComponent<BoxCollider>();collider.size=targetSize;collider.isTrigger=true;
        var seat=root.AddComponent<FurnitureSeat>();seat.seatId=id;seat.title="坐下休息 · "+caption;seat.eyeHeight=eyeHeight;
        seat.sittingPoint=Group("Sitting pose",-Vector3.up*targetY,root.transform).transform;
        seat.sittingPoint.localRotation=Quaternion.LookRotation(facing);
        seat.standingPoint=Group("Clear standing aisle",exit-root.transform.localPosition,root.transform).transform;
        seat.standingPoint.localRotation=Quaternion.LookRotation(facing);
        return seat;
    }
    static void StreetBenchSeat(GameObject bench,int index) {
        var seat=bench.AddComponent<FurnitureSeat>();seat.seatId="street-bench-"+index;seat.title="坐下休息 · 街边长椅";seat.eyeHeight=1.10f;
        var facing=bench.transform.Find("Seat facing");
        seat.sittingPoint=Group("Resting pose",new Vector3(0,.025f,0),bench.transform).transform;seat.sittingPoint.localRotation=facing.localRotation;
        seat.standingPoint=Group("Clear standing aisle",facing.localRotation*Vector3.forward*.95f+Vector3.up*.025f,bench.transform).transform;seat.standingPoint.localRotation=facing.localRotation;
    }
    static void WorldItem(Transform parent,string title,string source,string body,Vector3 position,Vector3 size) {
        var root=Group("Item · "+title,position,parent);var c=root.AddComponent<BoxCollider>();c.size=size;c.isTrigger=true;
        var item=root.AddComponent<Interactable>();item.title=title;item.source=source;item.body=body;
    }
    static void StationItems(Transform station) {
        int index=0;
        foreach(float start in new[]{-10.5f,5.5f})foreach(float z in new[]{-3.4f,-1.8f})foreach(float dx in new[]{1.25f,3.75f}) {
            var facing=z<-2.5f?Vector3.forward:Vector3.back;
            var feet=new Vector3(start+dx,.12f,z);
            RestSeat(station,"hall-bench-"+index++,"候车长椅",feet,facing,feet+facing*.95f,1.15f,new Vector3(1.10f,.17f,.54f),.49f);
        }
        const string rules="青川车站 · 旅客须知";
        WorldItem(station,"查看旅客须知",rules,"检票时请交验车票。\n大件行李请妥善放置。\n\n请从候车厅后门前往站台，请勿在轨道上停留。",new Vector3(10,2.70f,5.81f),new Vector3(1.20f,1.10f,.07f));
        WorldItem(station,"查看候车告示",rules,"售票与候车\n\n请从后门前往站台。\n请勿在轨道上停留。",new Vector3(-14.80f,2.60f,-3.5f),new Vector3(.07f,1.10f,1.15f));
        WorldItem(station,"查看行李须知",rules,"大件行李请寄存。\n请勿阻塞过道。",new Vector3(14.80f,2.60f,-3.5f),new Vector3(.07f,1.10f,1.15f));
        WorldItem(station,"查看行李秤","青川车站 · 行李寄存处","金属托盘用于放置行李，上方是圆形表盘。",new Vector3(11.5f,1.80f,-1.48f),new Vector3(.55f,.55f,.12f));
        WorldItem(station,"查看行李箱","青川车站 · 站台行李推车","皮革旅行箱。束带收紧，金属搭扣扣好。",new Vector3(-21.75f,.85f,20.5f),new Vector3(.88f,.65f,.73f));
    }
    static void TrainItems(Transform train,Transform visual) {
        for(int coach=0;coach<2;coach++) {
            float offset=coach==0?0:-16;string number=coach==0?"01":"02";
            float[] bays={-4.5f,-2.7f,-.9f,.9f,2.7f};
            for(int bay=0;bay<bays.Length;bay++)foreach(int face in new[]{1,-1})foreach(int side in new[]{-1,1}) {
                float x=offset+bays[bay]-face*.45f;
                var feet=new Vector3(x,1.05f,side*.92f);
                RestSeat(train,"coach-"+number+"-"+bay+"-"+face+"-"+side,"车厢座椅",feet,Vector3.right*face,new Vector3(x,1.05f,0),1.18f,new Vector3(.42f,.17f,.75f),.55f);
            }
            foreach(int side in new[]{-1,1}) {
                if(coach==0&&side==-1)continue; // Existing departure seat has its own explicit action.
                var feet=new Vector3(offset+3.8f,1.05f,side*.92f);
                RestSeat(train,"coach-"+number+"-end-"+side,"车厢座椅",feet,Vector3.right,new Vector3(feet.x,1.05f,0),1.18f,new Vector3(.44f,.17f,.75f),.55f);
            }
            var target=Group("Coach "+number+" lamp switch",new Vector3(offset+6.55f,2.16f,-1.385f),train);
            var trigger=target.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(.18f,.23f,.09f);
            var action=target.AddComponent<CoachLampSwitch>();action.offId="coach-light-off-"+number;action.coachName=number;
            action.knob=visual.GetComponentsInChildren<Transform>().Single(t=>t.name=="SwitchKnob_C"+(coach+1));
            action.lamps=train.GetComponentsInChildren<DayNightLamp>().Where(t=>t.name=="Coach warm pendant"&&Mathf.Abs(train.InverseTransformPoint(t.transform.position).x-offset)<7.4f).ToArray();
            if(action.lamps.Length!=4)throw new Exception("Coach switch must control exactly four existing lamps");
        }
        WorldItem(train,"查看旅行卷包","客车01 · 行李架","布卷用束带扎好，放在窗上方的行李架里。",new Vector3(-2.7f,3.29f,-1.25f),new Vector3(.55f,.19f,.21f));
        WorldItem(train,"查看皮革小箱","客车01 · 行李架","皮革小箱，金属搭扣扣好。",new Vector3(.9f,3.28f,-1.25f),new Vector3(.51f,.17f,.29f));
        WorldItem(train,"查看红色小箱","客车01 · 行李架","红色旅行小箱，放在窗上方的行李架里。",new Vector3(-.9f,3.28f,1.25f),new Vector3(.47f,.18f,.27f));
    }
}
