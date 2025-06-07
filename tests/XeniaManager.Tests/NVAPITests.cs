using XeniaManager.Core.GPU.NVIDIA;

namespace XeniaManager.Tests;

public class NvidiaApiTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void InitializationTest()
    {
        if (NVAPI.Initialize())
        {
            Assert.Pass();
        }
        else
        {
            Assert.Fail();
        }
    }

    [Test]
    public void FindProfileTest()
    {
        NVAPI.FindAppProfile();
    }   
    
    [Test]
    public void GetSettingsTest()
    {
        NVAPI.GetSetting(NVAPI_SETTINGS.VSYNC_MODE);
        NVAPI.GetSetting(NVAPI_SETTINGS.FRAMERATE_LIMITER);
    }
    
    [Test]
    public void SetVSyncSettingTest()
    {
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.DEFAULT);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.FORCE_OFF);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.FORCE_ON);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.HALF_REFRESH_RATE);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.THIRD_REFRESH_RATE);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.QUARTER_REFRESH_RATE);
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)NVAPI_VSYNC_MODE.ADAPTIVE);
    }
}