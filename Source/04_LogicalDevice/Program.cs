using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

var app = new HelloTriangleApplication_04();
app.Run();

public unsafe class HelloTriangleApplication_04 : HelloTriangleApplication_03
{
    protected Device device;

    // ReSharper disable once NotAccessedField.Local
    protected Queue graphicsQueue;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        PickPhysicalDevice();
        CreateLogicalDevice();
    }
    
    protected override void CleanUp()
    {
        vk!.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        vk!.DestroyInstance(instance, null);
        vk!.Dispose();

        window?.Dispose();
    }

    protected virtual void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies_03(physicalDevice);

        DeviceQueueCreateInfo queueCreateInfo = new()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = indices.GraphicsFamily!.Value,
            QueueCount = 1
        };

        float queuePriority = 1.0f;
        queueCreateInfo.PQueuePriorities = &queuePriority;

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = 0
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (vk!.CreateDevice(physicalDevice, in createInfo, null, out device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        vk!.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out graphicsQueue);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }
}