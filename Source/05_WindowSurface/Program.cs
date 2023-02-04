using Silk.NET.Vulkan;
using SilkNetConvenience.Devices;
using SilkNetConvenience.KHR;
using SilkNetConvenience.Queues;

var app = new HelloTriangleApplication_05();
app.Run();

public struct QueueFamilyIndices_05
{
    public uint? GraphicsFamily { get; set; }
    public uint? PresentFamily { get; set; }

    public bool IsComplete()
    {
        return GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}

public class HelloTriangleApplication_05 : HelloTriangleApplication_04
{
    protected VulkanSurface? surface;
    protected VulkanQueue? presentQueue;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
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

        surface!.Dispose();
        instance!.Dispose();
        vk!.Dispose();

        window?.Dispose();
    }

    protected void CreateSurface() {
        surface = instance!.KhrSurface.CreateSurface(window!.VkSurface!);
    }

    protected override void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies_05(physicalDevice!);

        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        var queueCreateInfos = new DeviceQueueCreateInformation[uniqueQueueFamilies.Length];

        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueuePriorities = new[]{1f}
            };
        }

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInformation createInfo = new()
        {
            QueueCreateInfos = queueCreateInfos,
            EnabledFeatures = deviceFeatures
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayers = validationLayers;
        }

        device = physicalDevice!.CreateDevice(createInfo);

        graphicsQueue = device.GetDeviceQueue(indices.GraphicsFamily!.Value, 0);
        presentQueue = device.GetDeviceQueue(indices.PresentFamily!.Value, 0);
    }
    
    protected QueueFamilyIndices_05 FindQueueFamilies_05(VulkanPhysicalDevice physDevice)
    {
        var indices = new QueueFamilyIndices_05();

        var queueFamilies = physDevice.GetQueueFamilyProperties();

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            var presentSupport = instance!.KhrSurface.GetPhysicalDeviceSurfaceSupport(physDevice, i, surface!);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }
}