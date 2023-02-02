using Silk.NET.Vulkan;
using SilkNetConvenience.Devices;
using SilkNetConvenience.Queues;

var app = new HelloTriangleApplication_04();
app.Run();

public class HelloTriangleApplication_04 : HelloTriangleApplication_03
{
    protected VulkanDevice? device;
    protected VulkanQueue? graphicsQueue;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        PickPhysicalDevice();
        CreateLogicalDevice();
    }
    
    protected override void CleanUp()
    {
        device!.Dispose();
        
        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugMessenger!.Dispose();
        }

        instance!.Dispose();
        vk!.Dispose();

        window?.Dispose();
    }

    protected virtual void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies_03(physicalDevice!);

        DeviceQueueCreateInformation queueCreateInfo = new()
        {
            QueueFamilyIndex = indices.GraphicsFamily!.Value,
            QueuePriorities = new[]{1f}
        };

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInformation createInfo = new()
        {
            QueueCreateInfos = new []{queueCreateInfo},
            EnabledFeatures = deviceFeatures
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayers = validationLayers;
        }

        device = physicalDevice!.CreateDevice(createInfo);

        graphicsQueue = device.GetDeviceQueue(indices.GraphicsFamily!.Value, 0);
    }
}