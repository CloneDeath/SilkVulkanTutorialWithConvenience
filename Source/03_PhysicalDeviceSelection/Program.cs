using Silk.NET.Vulkan;

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

public unsafe class HelloTriangleApplication_03 : HelloTriangleApplication_02
{
    protected PhysicalDevice physicalDevice;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        PickPhysicalDevice();
    }
    
    protected void PickPhysicalDevice()
    {
        uint devicedCount = 0;
        vk!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

        if (devicedCount == 0)
        {
            throw new Exception("failed to find GPUs with Vulkan support!");
        }

        var devices = new PhysicalDevice[devicedCount];
        fixed (PhysicalDevice* devicesPtr = devices)
        {
            vk!.EnumeratePhysicalDevices(instance, ref devicedCount, devicesPtr);
        }

        foreach (var candidateDevice in devices)
        {
            if (IsDeviceSuitable(candidateDevice))
            {
                physicalDevice = candidateDevice;
                break;
            }
        }

        if (physicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    protected virtual bool IsDeviceSuitable(PhysicalDevice candidateDevice)
    {
        var indices = FindQueueFamilies_03(candidateDevice);

        return indices.IsComplete();
    }

    protected QueueFamilyIndices_03 FindQueueFamilies_03(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices_03();

        uint queueFamilityCount = 0;
        vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }


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