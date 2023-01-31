using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Vulkan;

var app = new HelloTriangleApplication_01();
app.Run();

public unsafe class HelloTriangleApplication_01 : HelloTriangleApplication_00
{
    protected Vk? vk;

    protected Instance instance;

    protected override void InitVulkan()
    {
        CreateInstance();
    }

    

    protected override void CleanUp()
    {
        vk!.DestroyInstance(instance, null);
        vk!.Dispose();

        window?.Dispose();
    }

    protected void CreateInstance()
    {
        vk = Vk.GetApi();

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version11
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);

        createInfo.EnabledExtensionCount = glfwExtensionCount;
        createInfo.PpEnabledExtensionNames = glfwExtensions;
        createInfo.EnabledLayerCount = 0;

        if (vk.CreateInstance(createInfo, null, out instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
    }
}