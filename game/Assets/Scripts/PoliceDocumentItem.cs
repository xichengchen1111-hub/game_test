using UnityEngine;

// A police document, notice or approved copy: ordinary inspection plus a paper sound.
// Copies of E01/E08 keep the stable evidence ID, so DemoApp records them only once.
public sealed class PoliceDocumentItem : Interactable {
    public string documentId;
    public AudioSource paperAudio;
    public override void Use() {
        if (paperAudio) paperAudio.Play();
        base.Use();
    }
}
