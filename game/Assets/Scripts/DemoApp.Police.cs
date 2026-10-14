using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stage report and the police duty officer (D police station, 2026-10-12).
// The player chooses a statement, a judgment and supporting notebook cards. The desk
// only checks whether the submitted relation is backed by the required records; it
// never names a culprit, never auto-completes from a full notebook and never ends
// exploration. Reading-type modal: movement/look stop, the shared clock keeps running.
[Serializable] public class PoliceReportOption { public string id, text; }
[Serializable] public class PoliceReportRule { public string id, statement, kind, boundary; public string[] required, supporting; public bool core; }
[Serializable] public class PoliceReportData {
    public PoliceReportOption[] statements, kinds;
    public PoliceReportRule[] rules;
    public string unsupported, unresolved, closing;
    public static PoliceReportData Load() {
        var asset = Resources.Load<TextAsset>("PoliceReport");
        if (!asset) throw new InvalidOperationException("Missing Resources/PoliceReport.json");
        var data = JsonUtility.FromJson<PoliceReportData>(asset.text);
        if (data.statements == null || data.kinds == null || data.rules == null) throw new InvalidOperationException("Incomplete police report data");
        return data;
    }
}

public partial class DemoApp {
    bool policeReportOpen;
    public bool PoliceReportOpen => policeReportOpen;
    PoliceReportData reportData;
    int reportStatement = -1, reportKind = -1;
    readonly HashSet<string> reportCards = new HashSet<string>();
    string reportFeedback = "";
    Vector2 reportCardScroll;
    DialogueScript policeScript;
    public string LastReportRule { get; private set; }

    public void OpenPoliceReport() {
        if (DialogueOpen) CloseDialogue();
        if (opening || transitioning || paused || inspected || MilkOrdersOpen || policeReportOpen) return;
        journal = false;
        reportData ??= PoliceReportData.Load();
        reportStatement = -1; reportKind = -1; reportCards.Clear(); reportFeedback = ""; LastReportRule = null;
        policeReportOpen = true; player.scriptedDirection = Vector3.zero; CursorMode();
        Debug.Log("POLICE_REPORT_OPEN");
    }
    public void ClosePoliceReport() {
        if (!policeReportOpen) return;
        policeReportOpen = false; CursorMode(); Save();
        Debug.Log("POLICE_REPORT_CLOSED");
    }
    public void SelectReportStatement(int index) { if (policeReportOpen && index >= 0 && index < reportData.statements.Length) reportStatement = index; }
    public void SelectReportKind(int index) { if (policeReportOpen && index >= 0 && index < reportData.kinds.Length) reportKind = index; }
    public void ToggleReportCard(string evidenceId) {
        if (!policeReportOpen || !state.entries.Exists(e => e.id == evidenceId)) return;
        if (!reportCards.Remove(evidenceId)) reportCards.Add(evidenceId);
    }
    // Returns the supported rule ID, or null when the attached records do not check out.
    public string SubmitPoliceReport() {
        if (!policeReportOpen) return null;
        if (reportStatement < 0 || reportKind < 0) { reportFeedback = "请先选一句陈述，再选一种判断。"; return null; }
        string statement = reportData.statements[reportStatement].id, kind = reportData.kinds[reportKind].id;
        var cards = state.entries.Where(e => reportCards.Contains(e.id)).ToList();
        var rule = reportData.rules.FirstOrDefault(r => r.statement == statement && r.kind == kind && r.required.All(id => reportCards.Contains(id)));
        state.reportSubmissions.Add(statement + "|" + kind + "|" + string.Join(",", cards.Select(c => c.id)) + "|" + (rule != null ? rule.id : "-"));
        string attached = cards.Count == 0 ? "（未附材料）" : string.Join("、", cards.Select(c => c.title));
        if (rule != null) {
            if (!state.reportSupported.Contains(rule.id)) state.reportSupported.Add(rule.id);
            reportFeedback = "这份判断有可以核对的材料支撑。\n已附材料：" + attached + "\n" + rule.boundary;
        } else reportFeedback = reportData.unsupported + "\n已附材料：" + attached + "\n" + reportData.unresolved;
        var core = reportData.rules.Where(r => r.core).Select(r => r.id).ToArray();
        if (!state.reportReinvestigation && core.All(id => state.reportSupported.Contains(id))) {
            state.reportReinvestigation = true;
            reportFeedback += "\n\n" + reportData.closing;
        }
        LastReportRule = rule?.id;
        Save();
        Debug.Log("POLICE_REPORT_SUBMIT: " + statement + "/" + kind + " cards=" + string.Join(",", reportCards) + " rule=" + (rule?.id ?? "none") + " reinvestigation=" + state.reportReinvestigation);
        return rule?.id;
    }
    void UpdatePoliceReportInput() {
        if (Input.GetKeyDown(KeyCode.Escape)) ClosePoliceReport();
    }
    void DrawPoliceReport() {
        Box(new Rect(0, 0, W, H), new Color(0.015f, 0.03f, 0.04f, 0.62f));
        Rect p = new Rect(W / 2 - 600, H / 2 - 330, 1200, 660); Panel(p);
        Label(new Rect(p.x + 40, p.y + 26, 700, 52), "阶段报告", title);
        Label(new Rect(p.x + 42, p.y + 84, 1100, 30), "选一句你要质疑的陈述，判断它哪里需要核对，再附上你认为能支撑的材料。警署只核对材料，不替你下结论。", italic);
        Box(new Rect(p.x + 40, p.y + 120, 1120, 1), new Color(0.29f, 0.37f, 0.37f));
        var small2 = new GUIStyle(small) { wordWrap = true };
        Label(new Rect(p.x + 40, p.y + 132, 460, 26), "一 · 质疑哪句陈述", small);
        // Five statements share the column above the history line.
        float step = Mathf.Min(62f, 256f / reportData.statements.Length);
        for (int i = 0; i < reportData.statements.Length; i++) {
            var r = new Rect(p.x + 40, p.y + 162 + i * step, 460, step - 6);
            if (i == reportStatement) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.68f, 0.5f, 0.3f, .55f));
            if (Button(r, "", i == reportStatement)) reportStatement = i;
            Label(new Rect(r.x + 12, r.y + 6, r.width - 24, r.height - 8), reportData.statements[i].text, small2);
        }
        Label(new Rect(p.x + 530, p.y + 132, 260, 26), "二 · 你的判断", small);
        for (int i = 0; i < reportData.kinds.Length; i++) {
            var r = new Rect(p.x + 530, p.y + 162 + i * 62, 250, 56);
            if (i == reportKind) Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.68f, 0.5f, 0.3f, .55f));
            if (Button(r, reportData.kinds[i].text, i == reportKind)) reportKind = i;
        }
        Label(new Rect(p.x + 810, p.y + 132, 350, 26), "三 · 附上材料（调查簿）", small);
        var area = new Rect(p.x + 810, p.y + 162, 350, 244);
        if (state.entries.Count == 0) Label(new Rect(area.x, area.y + 8, area.width, 80), "调查簿里还没有材料。", small2);
        else {
            reportCardScroll = GUI.BeginScrollView(area, reportCardScroll, new Rect(0, 0, area.width - 20, state.entries.Count * 50));
            for (int i = 0; i < state.entries.Count; i++) {
                var e = state.entries[i]; var r = new Rect(0, i * 50, area.width - 24, 44);
                bool on = reportCards.Contains(e.id);
                if (on) Box(new Rect(r.x, r.y, r.width, r.height), new Color(0.68f, 0.5f, 0.3f, .55f));
                if (Button(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), (on ? "已附 · " : "") + e.title, on)) ToggleReportCard(e.id);
            }
            GUI.EndScrollView();
        }
        Box(new Rect(p.x + 40, p.y + 420, 1120, 1), new Color(0.29f, 0.37f, 0.37f));
        string history = state.reportSupported.Count == 0 ? "" : "已核对：" + string.Join("、", reportData.rules.Where(r => state.reportSupported.Contains(r.id)).Select(r => reportData.statements.First(s => s.id == r.statement).text.Split('：')[0] + "的" + reportData.kinds.First(k => k.id == r.kind).text).Distinct());
        if (state.reportReinvestigation) history += (history.Length > 0 ? "　·　" : "") + "案件已进入重新调查";
        Label(new Rect(p.x + 40, p.y + 430, 1120, 28), history, italic);
        Label(new Rect(p.x + 40, p.y + 462, 820, 180), reportFeedback, small2);
        if (Button(new Rect(p.x + 900, p.y + 520, 260, 52), "提交这份判断", true)) SubmitPoliceReport();
        if (Button(new Rect(p.x + 900, p.y + 584, 260, 52), "收起  ·  Esc")) ClosePoliceReport();
    }

    void OpenPoliceOfficerDialogue(Interactable item) {
        policeScript ??= DialogueScript.Load("PoliceOfficerDialogue");
        milkboyScript = policeScript;
        BindDialoguePerformance(item);
        dialogueHistory.Clear(); dialogueHistoryOpen = false; player.scriptedDirection = Vector3.zero;
        VisitDialogue(milkboyScript.first); CursorMode(); Debug.Log("DIALOGUE_OPEN: police-officer");
    }
    // The officer's "report" line hands over the paper: advancing it opens the stage report.
    bool TryPoliceDialogueAction() {
        if (policeScript == null || milkboyScript != policeScript || dialogueNode == null || dialogueNode.id != "report") return false;
        CloseDialogue(); OpenPoliceReport(); return true;
    }
}
