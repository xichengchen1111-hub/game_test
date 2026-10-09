using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class MilkDelivery { public string shop; public int bottles; public bool delivered; }
[Serializable] public class MilkDeliveryPage {
    public int year,month,day;
    public List<MilkDelivery> deliveries;
    public string DateId => new DateTime(year,month,day).ToString("yyyy-MM-dd");
}
[Serializable] public class MilkDeliveryLedger { public List<MilkDeliveryPage> pages; }

public class MilkOrderBook : Interactable {
    List<MilkDeliveryPage> pages;
    public IReadOnlyList<MilkDeliveryPage> Pages {
        get {
            if(pages!=null)return pages;
            var data=Resources.Load<TextAsset>("MilkDeliveryOrders");
            if(!data)throw new InvalidOperationException("Milk delivery records are missing");
            pages=JsonUtility.FromJson<MilkDeliveryLedger>(data.text).pages;
            if(pages==null||pages.Count==0)throw new InvalidOperationException("Milk delivery ledger has no pages");
            var dates=new HashSet<string>();
            foreach(var page in pages) {
                if(!dates.Add(page.DateId)||page.deliveries==null||page.deliveries.Count==0||page.deliveries.Count>5)
                    throw new InvalidOperationException("Invalid or duplicate delivery page: "+page.DateId);
                foreach(var row in page.deliveries)if(string.IsNullOrWhiteSpace(row.shop)||row.bottles<=0)
                    throw new InvalidOperationException("Invalid milk delivery row");
            }
            pages.Sort((a,b)=>string.CompareOrdinal(a.DateId,b.DateId));
            return pages;
        }
    }
    public override void Use() { DemoApp.Instance.OpenMilkOrders(this); }
}
