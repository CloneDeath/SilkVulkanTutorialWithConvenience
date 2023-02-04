using Silk.NET.Vulkan;
using SilkNetConvenience.Devices;

var app = new HelloTriangleApplication_03();
app.Run();

public struct QueueFamilyIndices_03
{
    public uint? GraphicsFamily { get; set; }
    public bool IsComplete()
    {
        return GraphicsFamily.HasValue;
    }
}

public class HelloTriangleApplication_03 : HelloTriangleApplication_02
{
    protected VulkanPhysicalDevice? physicalDevice;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        PickPhysicalDevice();
    }
    
    protected virtual void PickPhysicalDevice() {
        var devices = instance!.PhysicalDevices;

        foreach (var physDevice in devices)
        {
            if (IsDeviceSuitable(physDevice))
            {
                physicalDevice = physDevice;
                break;
            }
        }

        if (physicalDevice == null)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    protected virtual bool IsDeviceSuitable(VulkanPhysicalDevice physDevice)
    {
        var indices = FindQueueFamilies_03(physDevice);

        return indices.IsComplete();
    }

    protected QueueFamilyIndices_03 FindQueueFamilies_03(VulkanPhysicalDevice device)
    {
        var indices = new QueueFamilyIndices_03();

        var queueFamilies = device.GetQueueFamilyProperties();

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
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