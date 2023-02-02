using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

var app = new HelloTriangleApplication_24();
app.Run();

public unsafe class HelloTriangleApplication_24 : HelloTriangleApplication_23
{
    protected ImageView textureImageView;
    protected Sampler textureSampler;

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

        vk!.DestroySampler(device, textureSampler, null);
        vk!.DestroyImageView(device, textureImageView, null);

        vk!.DestroyImage(device, textureImage, null);
        vk!.FreeMemory(device, textureImageMemory, null);

        vk!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

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
        var qfIndices = FindQueueFamilies_05(physicalDevice);

        var uniqueQueueFamilies = new[] { qfIndices.GraphicsFamily!.Value, qfIndices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1
            };


            queueCreateInfos[i].PQueuePriorities = &queuePriority;
        }

        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true,
        };


        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
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

        device = physicalDevice!.CreateDevice(createInfo);

        vk!.GetDeviceQueue(device, qfIndices.GraphicsFamily!.Value, 0, out graphicsQueue);
        vk!.GetDeviceQueue(device, qfIndices.PresentFamily!.Value, 0, out presentQueue);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

    }

    protected override void CreateImageViews()
    {
        swapchainImageViews = new ImageView[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {

            swapchainImageViews[i] = CreateImageView(swapchainImages[i], swapchainImageFormat);
        }
    }

    protected virtual void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb);
    }

    protected virtual void CreateTextureSampler()
    {
        PhysicalDeviceProperties properties;
        vk!.GetPhysicalDeviceProperties(physicalDevice, out properties);

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
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

        fixed(Sampler* textureSamplerPtr = &textureSampler)
        {
            if(vk!.CreateSampler(device, samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }

    protected ImageView CreateImageView(Image image, Format format)
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
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

        ImageView imageView;

        if (vk!.CreateImageView(device, createInfo, null, out imageView) != Result.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }
    
    protected override bool IsDeviceSuitable(PhysicalDevice candidateDevice)
    {
        var qfIndices = FindQueueFamilies_05(candidateDevice);

        bool extensionsSupported = CheckDeviceExtensionsSupport(candidateDevice);

        bool swapchainAdequate = false;
        if (extensionsSupported)
        {
            var swapchainSupport = QuerySwapchainSupport(candidateDevice);
            swapchainAdequate =  swapchainSupport.Formats.Any() && swapchainSupport.PresentModes.Any();
        }

        PhysicalDeviceFeatures supportedFeatures;
        vk!.GetPhysicalDeviceFeatures(candidateDevice, out supportedFeatures);

        return qfIndices.IsComplete() && extensionsSupported && swapchainAdequate && supportedFeatures.SamplerAnisotropy;
    }
}