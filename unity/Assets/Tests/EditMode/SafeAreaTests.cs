using System.Collections;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class SafeAreaTests
{
    [Test]
    public void CssPixelsBecomePanelPixelsAtTheDevicesRatioAndScale()
    {
        // a 47 px iPhone notch at 3x, the panel scaled 1.5 device px per panel px: exactly 94 panel px (no .5 boundary)
        Assert.AreEqual(94, SafeAreaFit.Panel(47f, 3f, 1.5f));
        Assert.AreEqual(0, SafeAreaFit.Panel(0f, 3f, 1.5f), "no inset, no padding");
        Assert.AreEqual(0, SafeAreaFit.Panel(47f, 3f, 0f), "no scale yet, no padding");
    }

    [Test]
    public void DevicePixelsBecomePanelPixelsOverTheScale()
    {
        Assert.AreEqual(40, SafeAreaFit.PanelPx(48f, 1.2f));
        Assert.AreEqual(0, SafeAreaFit.PanelPx(48f, 0f), "no scale yet, no padding");
    }

    /// <summary>The padding itself: Apply writes it, the layout follows (the top bar clears the notch), and Dispose
    /// puts it back. Red without the writes in Apply, and red without the reset in Dispose.</summary>
    [UnityTest]
    public IEnumerator TheRootIsPaddedAndDisposePutsItBack()
    {
        using var ui = new UiTestKit();
        var fit = new SafeAreaFit(ui.Root);
        // The first layout fires Update(), which reads the editor's whole-screen safe area and applies zeros: let it
        // pass before the test's insets go in, or it wipes them.
        for (int i = 0; i < 20 && float.IsNaN(ui.Root.resolvedStyle.width); i++) yield return null;
        yield return null; yield return null;
        fit.Apply(10, 20, 47, 34);
        // What Apply wrote, exactly; the resolved padding snaps to the panel's pixel grid (47 -> 46.29 at this scale).
        Assert.AreEqual(47f, ui.Root.style.paddingTop.value.value, 1e-3f, "the notch's inset pads the root");
        Assert.AreEqual(34f, ui.Root.style.paddingBottom.value.value, 1e-3f, "the home indicator's inset");
        Assert.AreEqual(10f, ui.Root.style.paddingLeft.value.value, 1e-3f);
        Assert.AreEqual(20f, ui.Root.style.paddingRight.value.value, 1e-3f);
        for (int i = 0; i < 10 && ui.Root.resolvedStyle.paddingTop < 45f; i++) yield return null;
        Assert.AreEqual(47f, ui.Root.resolvedStyle.paddingTop, 1f, "the layout follows, snapped to the pixel grid");
        Assert.AreEqual(34f, ui.Root.resolvedStyle.paddingBottom, 1f);
        Assert.GreaterOrEqual(ui.Q<VisualElement>("top-bar").worldBound.yMin, 47f - 1f, "the top bar moves down out of the notch");
        fit.Dispose();
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingTop.keyword, "Dispose resets the style");
        Assert.AreEqual(StyleKeyword.Null, ui.Root.style.paddingLeft.keyword);
        for (int i = 0; i < 10 && ui.Root.resolvedStyle.paddingTop > 0f; i++) yield return null;
        Assert.AreEqual(0f, ui.Root.resolvedStyle.paddingTop, 1f, "Dispose resets the padding");
    }
}
