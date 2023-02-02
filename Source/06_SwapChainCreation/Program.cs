using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using SilkNetConvenience;
using SilkNetConvenience.Devices;
using SilkNetConvenience.KHR;

var app = new HelloTriangleApplication_06();
app.Run();

public struct SwapchainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}

public class HelloTriangleApplication_06 : HelloTriangleApplication_05
{
    protected readonly string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName
    };

    protected VulkanKhrSwapchain? khrSwapchain;
    protected VulkanSwapchain? swapchain;
    protected VulkanSwapchainImage[]? swapchainImages;
    protected Format swapchainImageFormat;
    protected Extent2D swapchainExtent;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
    }

    protected override void CleanUp() {
        swapchain!.Dispose();

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
            EnabledFeatures = deviceFeatures,
            EnabledExtensions = deviceExtensions,
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayers = validationLayers;
        }

        device = physicalDevice!.CreateDevice(createInfo);

        graphicsQueue = device.GetDeviceQueue(indices.GraphicsFamily!.Value, 0);
        presentQueue = device.GetDeviceQueue(indices.PresentFamily!.Value, 0);
    }

    protected virtual void CreateSwapchain()
    {
        var swapchainSupport = QuerySwapchainSupport(physicalDevice!);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapchainSupport.Formats);
        var presentMode = ChoosePresentMode(swapchainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapchainSupport.Capabilities);

        var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
        if (swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapchainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInformation creatInfo = new()
        {
            Surface = surface!,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = FindQueueFamilies_05(physicalDevice!);
        var queueFamilyIndices = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily)
        {
            creatInfo.ImageSharingMode = SharingMode.Concurrent;
            creatInfo.QueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creatInfo.PreTransform = swapchainSupport.Capabilities.CurrentTransform;
        creatInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        creatInfo.PresentMode = presentMode;
        creatInfo.Clipped = true;
        creatInfo.OldSwapchain = default;

        khrSwapchain = device!.GetKhrSwapchainExtension();

        swapchain = khrSwapchain!.CreateSwapchain(creatInfo);

        swapchainImages = swapchain.GetImages();

        swapchainImageFormat = surfaceFormat.Format;
        swapchainExtent = extent;
    }

    protected SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if(availableFormat is { Format: Format.B8G8R8A8Srgb, ColorSpace: ColorSpaceKHR.SpaceSrgbNonlinearKhr })
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    protected PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if(availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    protected Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            var framebufferSize = window!.FramebufferSize;

            Extent2D actualExtent = new () {
                Width = (uint)framebufferSize.X,
                Height = (uint)framebufferSize.Y
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    protected SwapchainSupportDetails QuerySwapchainSupport(VulkanPhysicalDevice physDevice)
    {
        return new SwapchainSupportDetails {
            Capabilities = khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physDevice, surface!),
            Formats = khrSurface!.GetPhysicalDeviceSurfaceFormats(physDevice, surface!),
            PresentModes = khrSurface.GetPhysicalDeviceSurfacePresentModes(physDevice, surface!)
        };
    }

    protected override bool IsDeviceSuitable(VulkanPhysicalDevice candidateDevice)
    {
        var indices = FindQueueFamilies_05(candidateDevice);

        bool extensionsSupported = CheckDeviceExtensionsSupport(candidateDevice);

        bool swapchainAdequate = false;
        if (extensionsSupported)
        {
            var swapchainSupport = QuerySwapchainSupport(candidateDevice);
            swapchainAdequate =  swapchainSupport.Formats.Any() && swapchainSupport.PresentModes.Any();
        }

        return indices.IsComplete() && extensionsSupported && swapchainAdequate;
    }

    protected bool CheckDeviceExtensionsSupport(VulkanPhysicalDevice physDevice)
    {
        var availableExtensions = physDevice.EnumerateExtensionProperties();
        var availableExtensionNames = availableExtensions.Select(extension => extension.GetExtensionName()).ToHashSet();
        return deviceExtensions.All(availableExtensionNames.Contains);
    }
}