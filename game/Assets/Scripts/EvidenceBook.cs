using UnityEngine;

// Several sheets that are one bound document (user, 2026-10-19: the register's next page is a page to
// turn, not a separate piece of evidence). Each page slot may hold variants; the one active in the
// scene is the page as it can be seen now (the hotel register's next page shows the pressed writing
// only while the desk lamp is lowered). Each page keeps its own evidence ID, title and text, so
// reading a page records exactly what reading that sheet recorded before.
public sealed class EvidenceBook : MonoBehaviour {
    // Bound: pages turn over a spine (registers). Stack: loose sheets kept together (an envelope and the
    // copies inside it, a file folder); the top sheet is moved aside to reach the next.
    public enum Style { Bound, Stack }
    public Style style;
    public string container = "", label = "";
    public Interactable[][] pages;
    public int Count => pages == null ? 0 : pages.Length;
    public Interactable Page(int index) {
        var slot = pages[Mathf.Clamp(index, 0, Count - 1)];
        foreach (var page in slot) if (page && page.gameObject.activeInHierarchy) return page;
        return slot[0];
    }
    public static EvidenceBook Bind(Interactable first, Style style, string container, string label, params Interactable[][] pages) {
        var book = first.GetComponent<EvidenceBook>() ? first.GetComponent<EvidenceBook>() : first.gameObject.AddComponent<EvidenceBook>();
        book.pages = pages; book.style = style; book.container = container ?? ""; book.label = label ?? "";
        for (int i = 0; i < pages.Length; i++)
            foreach (var page in pages[i]) {
                if (!page) continue;
                var of = page.GetComponent<EvidencePageOf>() ? page.GetComponent<EvidencePageOf>() : page.gameObject.AddComponent<EvidencePageOf>();
                of.book = book; of.index = i;
            }
        return book;
    }
}

// Moves a sheet in or out of a stack while it is turned (positions in the model's own space).
public sealed class EvidenceSlide : MonoBehaviour {
    public Vector3 from, to;
    public float spinFrom, spinTo;
}

// Marks a sheet as a page of a book: using it opens the book at this page.
public sealed class EvidencePageOf : MonoBehaviour {
    public EvidenceBook book;
    public int index;
}
