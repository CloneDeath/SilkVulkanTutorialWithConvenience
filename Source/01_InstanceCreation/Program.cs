using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using SilkNetConvenience.CreateInfo;
using SilkNetConvenience.Wrappers;

var app = new HelloTriangleApplication_01();
app.Run();

public unsafe class HelloTriangleApplication_01 : HelloTriangleApplication_00
{
    protected VulkanContext? vk;

    protected VulkanInstance? instance;

    protected override void InitVulkan()
    {
        CreateInstance();
    }

    protected override void CleanUp()
    {
        instance!.Dispose();
        vk!.Dispose();

        window?.Dispose();
    }

    protected virtual void CreateInstance()
    {
        vk = new VulkanContext();

        ApplicationInformation appInfo = new()
        {
            ApplicationName = "Hello Triangle",
            ApplicationVersion = new Version32(1, 0, 0),
            EngineName = "No Engine",
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version11
        };

        InstanceCreateInformation createInfo = new()
        {
            ApplicationInfo = appInfo
        };

        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        
        createInfo.EnabledExtensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
        createInfo.EnabledLayers = Array.Empty<string>();

        instance = vk.CreateInstance(createInfo);
    }
}