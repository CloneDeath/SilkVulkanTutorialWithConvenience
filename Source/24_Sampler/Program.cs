using Silk.NET.Vulkan;
using SilkNetConvenience.Devices;
using SilkNetConvenience.Images;

var app = new HelloTriangleApplication_24();
app.Run();

public class HelloTriangleApplication_24 : HelloTriangleApplication_23
{
    protected VulkanImageView? textureImageView;
    protected VulkanSampler? textureSampler;

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
        CreateImageViews();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateTextureImage();
        CreateTextureImageView();
        CreateTextureSampler();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

        textureSampler!.Dispose();
        textureImageView!.Dispose();

        textureImage!.Dispose();
        textureImageMemory!.Dispose();

        descriptorSetLayout!.Dispose();

        indexBuffer?.Dispose();
        indexBufferMemory?.Dispose();

        vertexBuffer!.Dispose();
        vertexBufferMemory!.Dispose();

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            renderFinishedSemaphores![i].Dispose();
            imageAvailableSemaphores![i].Dispose();
            inFlightFences![i].Dispose();
        }

        commandPool?.Dispose();

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
        var qfIndices = FindQueueFamilies_05(physicalDevice!);

        var uniqueQueueFamilies = new[] { qfIndices.GraphicsFamily!.Value, qfIndices.PresentFamily!.Value };
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

        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true,
        };


        DeviceCreateInformation createInfo = new()
        {
            QueueCreateInfos = queueCreateInfos,
            EnabledFeatures = deviceFeatures,
            EnabledExtensions = deviceExtensions
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayers = validationLayers;
        }

        device = physicalDevice!.CreateDevice(createInfo);

        graphicsQueue = device.GetDeviceQueue(qfIndices.GraphicsFamily!.Value, 0);
        presentQueue = device.GetDeviceQueue(qfIndices.PresentFamily!.Value, 0);
    }

    protected override void CreateImageViews()
    {
        swapchainImageViews = new VulkanImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            swapchainImageViews[i] = CreateImageView(swapchainImages[i], swapchainImageFormat);
        }
    }

    protected virtual void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage!, Format.R8G8B8A8Srgb);
    }

    protected virtual void CreateTextureSampler()
    {
        var properties = physicalDevice!.GetProperties();

        SamplerCreateInformation samplerInfo = new()
        {
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
        };

        textureSampler = device!.CreateSampler(samplerInfo);
    }

    protected VulkanImageView CreateImageView(Image image, Format format)
    {
        ImageViewCreateInformation createInfo = new()
        {
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

        };

        return device!.CreateImageView(createInfo);
    }
    
    protected override bool IsDeviceSuitable(VulkanPhysicalDevice physDevice)
    {
        var qfIndices = FindQueueFamilies_05(physDevice);

        bool extensionsSupported = CheckDeviceExtensionsSupport(physDevice);

        bool swapchainAdequate = false;
        if (extensionsSupported)
        {
            var swapchainSupport = QuerySwapchainSupport(physDevice);
            swapchainAdequate =  swapchainSupport.Formats.Any() && swapchainSupport.PresentModes.Any();
        }

        var supportedFeatures = physDevice.GetFeatures();
        return qfIndices.IsComplete() && extensionsSupported && swapchainAdequate && supportedFeatures.SamplerAnisotropy;
    }
}