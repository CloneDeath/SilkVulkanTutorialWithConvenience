using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using SilkNetConvenience;
using SilkNetConvenience.EXT;
using SilkNetConvenience.Instances;

var app = new HelloTriangleApplication_02();
app.Run();

public unsafe class HelloTriangleApplication_02 : HelloTriangleApplication_01
{
    protected bool EnableValidationLayers = true;

    protected readonly string[] validationLayers = new []
    {
        "VK_LAYER_KHRONOS_validation"
    };

    protected VulkanDebugUtilsMessenger? debugMessenger;
    
    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
    }

    protected override void CleanUp()
    {
        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugMessenger!.Dispose();
        }

        instance!.Dispose();
        vk!.Dispose();

        window?.Dispose();
    }

    protected override void CreateInstance()
    {
        vk = new VulkanContext();

        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("validation layers requested, but not available!");
        }

        ApplicationInformation appInfo = new()
        {
            ApplicationName = "Hello Triangle",
            ApplicationVersion = new Version32(1, 0, 0),
            EngineName = "No Engine",
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInformation createInfo = new()
        {
            ApplicationInfo = appInfo
        };

        var extensions = GetRequiredExtensions();
        createInfo.EnabledExtensions = extensions;
        
        if (EnableValidationLayers)
        {
            createInfo.EnabledLayers = validationLayers;

            DebugUtilsMessengerCreateInformation debugCreateInfo = new ();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.DebugUtilsMessengerCreateInfo = debugCreateInfo;
        }
        else 
        {
            createInfo.EnabledLayers = Array.Empty<string>();
        }

        instance = vk.CreateInstance(createInfo);
    }

    protected void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInformation createInfo)
    {
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }

    protected void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        DebugUtilsMessengerCreateInformation createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        debugMessenger = instance!.DebugUtils.CreateDebugUtilsMessenger(createInfo);
    }

    protected string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    protected bool CheckValidationLayerSupport() {
        var availableLayers = vk!.EnumerateInstanceLayerProperties();

        var availableLayerNames = availableLayers.Select(layer => layer.GetLayerName()).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    protected uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }
}