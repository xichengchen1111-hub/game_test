using UnityEngine;
public class Interactable : MonoBehaviour {
    public string title, evidenceId, source;
    [TextArea(3,12)] public string body;
    public Texture2D illustration;
    // The reverse of a paper, shown when it is turned over in the close-up (e.g. a date on the back of a drawing).
    public Texture2D reverse;
    public virtual string ActionText=>title!=null&&title.StartsWith("查看")?title:"查看 · "+title;
    public virtual void Use() { DemoApp.Instance.Inspect(this); }
}
