using NUnit.Framework;
using UnityEngine;

public class MobileAimSettingsTests {
    private bool hadKey;
    private int originalValue;

    [SetUp]
    public void SetUp() {
        hadKey = PlayerPrefs.HasKey(MobileAimSettings.PrefsKey);
        originalValue = PlayerPrefs.GetInt(MobileAimSettings.PrefsKey, 0);
        PlayerPrefs.DeleteKey(MobileAimSettings.PrefsKey);
    }

    [TearDown]
    public void TearDown() {
        if (hadKey)
        {
            PlayerPrefs.SetInt(MobileAimSettings.PrefsKey, originalValue);
        }
        else
        {
            PlayerPrefs.DeleteKey(MobileAimSettings.PrefsKey);
        }
    }

    [Test]
    public void Default_IsAutoAim() {
        Assert.AreEqual(MobileAimMode.AutoAim, MobileAimSettings.Mode);
    }

    [Test]
    public void SetMode_PersistsAcrossReads() {
        MobileAimSettings.Mode = MobileAimMode.TwinStick;
        Assert.AreEqual(1, PlayerPrefs.GetInt(MobileAimSettings.PrefsKey, -1));
        Assert.AreEqual(MobileAimMode.TwinStick, MobileAimSettings.Mode);
    }

    [Test]
    public void InvalidStoredValue_FallsBackToAutoAim() {
        PlayerPrefs.SetInt(MobileAimSettings.PrefsKey, 99);
        Assert.AreEqual(MobileAimMode.AutoAim, MobileAimSettings.Mode);
    }

    [Test]
    public void Changed_FiresOnlyWhenValueChanges() {
        int count = 0;
        MobileAimMode last = MobileAimMode.AutoAim;
        System.Action<MobileAimMode> handler = m => { count++; last = m; };
        MobileAimSettings.Changed += handler;
        try
        {
            MobileAimSettings.Mode = MobileAimMode.AutoAim; // 같은 값
            Assert.AreEqual(0, count);
            MobileAimSettings.Mode = MobileAimMode.TwinStick;
            Assert.AreEqual(1, count);
            Assert.AreEqual(MobileAimMode.TwinStick, last);
        }
        finally
        {
            MobileAimSettings.Changed -= handler;
        }
    }
}
