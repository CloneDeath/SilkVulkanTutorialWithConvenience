using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using Silk.NET.Assimp;

var app = new HelloTriangleApplication_29();
app.Run();

public struct Vertex
{
    public Vector3D<float> pos;
    public Vector3D<float> color;
    public Vector2D<float> textCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(textCoord)),
            }
        };

        return attributeDescriptions;
    }
}

public struct UniformBufferObject
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;
}

public unsafe class HelloTriangleApplication_29 : HelloTriangleApplication_28
{
    const string MODEL_PATH = @"Assets/viking_room.obj";
    const string TEXTURE_PATH = @"Assets/viking_room.png";

    protected SampleCountFlags msaaSamples = SampleCountFlags.Count1Bit;

    protected DescriptorSetLayout descriptorSetLayout;

    protected Image colorImage;
    protected DeviceMemory colorImageMemory;
    protected ImageView colorImageView;

    protected Image depthImage;
    protected DeviceMemory depthImageMemory;
    protected ImageView depthImageView;

    protected uint mipLevels;
    protected Image textureImage;
    protected DeviceMemory textureImageMemory;
    protected ImageView textureImageView;
    protected Sampler textureSampler;

    protected Buffer vertexBuffer;
    protected DeviceMemory vertexBufferMemory;
    protected Buffer indexBuffer;
    protected DeviceMemory indexBufferMemory;

    protected Buffer[]? uniformBuffers;
    protected DeviceMemory[]? uniformBuffersMemory;

    protected DescriptorPool descriptorPool;
    protected DescriptorSet[]? descriptorSets;

    protected Vertex[]? vertices;

    protected uint[]? indices;

    protected void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }

    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateCommandPool();
        CreateColorResources();
        CreateDepthResources();
        CreateFramebuffers();
        CreateTextureImage();
        CreateTextureImageView();
        CreateTextureSampler();
        LoadModel();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    

    protected void CleanUpSwapChain()
    {
        vk!.DestroyImageView(device, depthImageView, null);
        vk!.DestroyImage(device, depthImage, null);
        vk!.FreeMemory(device, depthImageMemory, null);

        vk!.DestroyImageView(device, colorImageView, null);
        vk!.DestroyImage(device, colorImage, null);
        vk!.FreeMemory(device, colorImageMemory, null);

        foreach (var framebuffer in swapChainFramebuffers!)
        {
            vk!.DestroyFramebuffer(device, framebuffer, null);
        }

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            vk!.FreeCommandBuffers(device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
        }

        vk!.DestroyPipeline(device, graphicsPipeline, null);
        vk!.DestroyPipelineLayout(device, pipelineLayout, null);
        vk!.DestroyRenderPass(device, renderPass, null);

        foreach (var imageView in swapChainImageViews!)
        {
            vk!.DestroyImageView(device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(device, swapChain, null);

        for (int i = 0; i < swapChainImages!.Length; i++)
        {
            vk!.DestroyBuffer(device, uniformBuffers![i], null);
            vk!.FreeMemory(device, uniformBuffersMemory![i], null);
        }

        vk!.DestroyDescriptorPool(device, descriptorPool, null);
    }

    protected override void CleanUp()
    {
        CleanUpSwapChain();

        vk!.DestroySampler(device, textureSampler, null);
        vk!.DestroyImageView(device, textureImageView, null);

        vk!.DestroyImage(device, textureImage, null);
        vk!.FreeMemory(device, textureImageMemory, null);

        vk!.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

        vk!.DestroyBuffer(device, indexBuffer, null);
        vk!.FreeMemory(device, indexBufferMemory, null);

        vk!.DestroyBuffer(device, vertexBuffer, null);
        vk!.FreeMemory(device,vertexBufferMemory, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk!.DestroySemaphore(device, renderFinishedSemaphores![i], null);
            vk!.DestroySemaphore(device, imageAvailableSemaphores![i], null);
            vk!.DestroyFence(device, inFlightFences![i], null);
        }

        vk!.DestroyCommandPool(device, commandPool, null);

        vk!.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        khrSurface!.DestroySurface(instance, surface, null);
        vk!.DestroyInstance(instance, null);
        vk!.Dispose();

        window?.Dispose();
    }

    protected void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        vk!.DeviceWaitIdle(device);

        CleanUpSwapChain();

        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateColorResources();
        CreateDepthResources();
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();

        imagesInFlight = new Fence[swapChainImages!.Length];
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

        foreach (var physDevice in devices)
        {
            if (IsDeviceSuitable(physDevice))
            {
                physicalDevice = physDevice;
                msaaSamples = GetMaxUsableSampleCount();
                break;
            }
        }

        if (physicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    

    

    

    protected void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapChainImageFormat,
            Samples = msaaSamples,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.ColorAttachmentOptimal,
        };

        AttachmentDescription depthAttachment = new()
        {
            Format = FindDepthFormat(),
            Samples = msaaSamples,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout= ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentDescription colorAttachmentResolve = new()
        {
            Format = swapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.DontCare,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp= AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentReference colorAttachmentResolveRef = new()
        {
            Attachment = 2,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
            PResolveAttachments = &colorAttachmentResolveRef,
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        var attachments = new[] { colorAttachment, depthAttachment, colorAttachmentResolve };

        fixed (AttachmentDescription* attachmentsPtr = attachments)
        {
            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentsPtr,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            if (vk!.CreateRenderPass(device, renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            } 
        }
    }

    protected void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutBinding samplerLayoutBinding = new()
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.FragmentBit,
        };

        var bindings = new[] { uboLayoutBinding, samplerLayoutBinding };
        
        fixed(DescriptorSetLayoutBinding* bindingsPtr = bindings)
        fixed(DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            DescriptorSetLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = bindingsPtr,
            };

            if (vk!.CreateDescriptorSetLayout(device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }
    }

    protected void CreateGraphicsPipeline()
    {
        var vertShaderCode = System.IO.File.ReadAllBytes("shaders/vert.spv");
        var fragShaderCode = System.IO.File.ReadAllBytes("shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc []
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var bindingDescription = Vertex.GetBindingDescription();
        var attributeDescriptions = Vertex.GetAttributeDescriptions();

        fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {

            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                PVertexBindingDescriptions = &bindingDescription,
                PVertexAttributeDescriptions = attributeDescriptionsPtr,
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = swapChainExtent,
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor,
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = msaaSamples,
            };

            PipelineDepthStencilStateCreateInfo depthStencil = new()
            {
                SType= StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false,
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;

            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,                
                SetLayoutCount = 1,
                PSetLayouts = descriptorSetLayoutPtr
            };

            if (vk!.CreatePipelineLayout(device, pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                Layout = pipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineHandle = default
            };

            if (vk!.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        vk!.DestroyShaderModule(device, fragShaderModule, null);
        vk!.DestroyShaderModule(device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    protected void CreateFramebuffers()
    {
        swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

        for(int i = 0; i < swapChainImageViews.Length; i++)
        {
            var attachments = new[] { colorImageView, depthImageView, swapChainImageViews[i] };

            fixed(ImageView* attachmentsPtr = attachments)
            {
                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    Layers = 1,
                };

                if (vk!.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                } 
            }
        }
    }

    

    protected void CreateColorResources()
    {
        Format colorFormat = swapChainImageFormat;

        CreateImage(swapChainExtent.Width, swapChainExtent.Height, 1, msaaSamples, colorFormat, ImageTiling.Optimal, ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref colorImage, ref colorImageMemory);
        colorImageView = CreateImageView(colorImage, colorFormat, ImageAspectFlags.ColorBit, 1);
    }

    protected void CreateDepthResources()
    {
        Format depthFormat = FindDepthFormat();

        CreateImage(swapChainExtent.Width, swapChainExtent.Height, 1, msaaSamples, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref depthImage, ref depthImageMemory);
        depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);
    }

    protected Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vk!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);
            
            if(tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    protected Format FindDepthFormat()
    {
        return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    protected void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(TEXTURE_PATH);
        
        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        mipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateImage((uint)img.Width, (uint)img.Height, mipLevels, SampleCountFlags.Count1Bit, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref textureImage, ref textureImageMemory);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        //Transitioned to VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL while generating mipmaps

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);

        GenerateMipMaps(textureImage, Format.R8G8B8A8Srgb, (uint)img.Width, (uint)img.Height, mipLevels);
    }

    protected void GenerateMipMaps(Image image, Format imageFormat, uint width, uint height, uint withMipLevels)
    {
        vk!.GetPhysicalDeviceFormatProperties(physicalDevice, imageFormat, out var formatProperties);

        if((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
        {
            throw new Exception("texture image format does not support linear blitting!");
        }

        var commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            Image = image,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                LevelCount = 1,
            }
        };

        var mipWidth = width;
        var mipHeight = height;

        for (uint i = 1; i < withMipLevels; i++)
        {
            barrier.SubresourceRange.BaseMipLevel = i - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.TransferSrcOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;

            vk!.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0,
                0, null,
                0, null,
                1, barrier);

            ImageBlit blit = new()
            {
                SrcOffsets =
                {
                    Element0 = new Offset3D(0,0,0),
                    Element1 = new Offset3D((int)mipWidth, (int)mipHeight, 1),
                },
                SrcSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i - 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,                    
                },
                DstOffsets =
                {
                    Element0 = new Offset3D(0,0,0),
                    Element1 = new Offset3D((int)(mipWidth > 1 ? mipWidth / 2 : 1), (int)(mipHeight > 1 ? mipHeight / 2 : 1),1),
                },
                DstSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },

            };

            vk!.CmdBlitImage(commandBuffer,
                image, ImageLayout.TransferSrcOptimal,
                image, ImageLayout.TransferDstOptimal,
                1, blit,
                Filter.Linear);

            barrier.OldLayout = ImageLayout.TransferSrcOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            vk!.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0,
                0, null,
                0, null,
                1, barrier);

            if (mipWidth > 1) mipWidth /= 2;
            if (mipHeight > 1) mipHeight /= 2;
        }

        barrier.SubresourceRange.BaseMipLevel = withMipLevels - 1;
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit;

        vk!.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0,
            0, null,
            0, null,
            1, barrier);

        EndSingleTimeCommands(commandBuffer);
    }

    protected SampleCountFlags GetMaxUsableSampleCount()
    {
        vk!.GetPhysicalDeviceProperties(physicalDevice, out var physicalDeviceProperties);

        var counts = physicalDeviceProperties.Limits.FramebufferColorSampleCounts & physicalDeviceProperties.Limits.FramebufferDepthSampleCounts;

        return counts switch
        {
            var c when (c & SampleCountFlags.Count64Bit) != 0 => SampleCountFlags.Count64Bit,
            var c when (c & SampleCountFlags.Count32Bit) != 0 => SampleCountFlags.Count32Bit,
            var c when (c & SampleCountFlags.Count16Bit) != 0 => SampleCountFlags.Count16Bit,
            var c when (c & SampleCountFlags.Count8Bit) != 0 => SampleCountFlags.Count8Bit,
            var c when (c & SampleCountFlags.Count4Bit) != 0 => SampleCountFlags.Count4Bit,
            var c when (c & SampleCountFlags.Count2Bit) != 0 => SampleCountFlags.Count2Bit,
            _ => SampleCountFlags.Count1Bit
        };
    }

    protected void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, mipLevels);
    }

    protected void CreateTextureSampler()
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
            MinLod = 0,
            MaxLod = mipLevels,
            MipLodBias = 0,
        };

        fixed(Sampler* textureSamplerPtr = &textureSampler)
        {
            if(vk!.CreateSampler(device, samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }

    protected ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint withMipLevels)
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
                    AspectMask = aspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = withMipLevels,
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

    protected void CreateImage(uint width, uint height, uint withMipLevels, SampleCountFlags numSamples, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = withMipLevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = numSamples,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Image* imagePtr = &image)
        {
            if(vk!.CreateImage(device,imageInfo,null, imagePtr) != Result.Success)
            {
                throw new Exception("failed to create image!");
            }
        }

        MemoryRequirements memRequirements;
        vk!.GetImageMemoryRequirements(device, image, out memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,properties),
        };

        fixed(DeviceMemory* imageMemoryPtr = &imageMemory) 
        {
            if (vk!.AllocateMemory(device, allocInfo, null, imageMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate image memory!");
            }
        }

        vk!.BindImageMemory(device, image, imageMemory, 0);
    }

    // ReSharper disable once UnusedParameter.Local
    protected void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint withMipLevels)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = withMipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if(oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }

        vk!.CmdPipelineBarrier(commandBuffer,sourceStage,destinationStage,0,0,null,0,null,1,barrier);

        EndSingleTimeCommands(commandBuffer);

    }

    protected void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,                
            },
            ImageOffset = new Offset3D(0,0,0),
            ImageExtent = new Extent3D(width, height, 1),
            
        };

        vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

        EndSingleTimeCommands(commandBuffer);
    }

    protected void LoadModel()
    {
        using var assimp = Assimp.GetApi();
        var scene = assimp.ImportFile(MODEL_PATH, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        var vertexMap = new Dictionary<Vertex, uint>();
        var localVertices = new List<Vertex>();
        var localIndices = new List<uint>();

        VisitSceneNode(scene->MRootNode);

        assimp.ReleaseImport(scene);

        this.vertices = localVertices.ToArray();
        this.indices = localIndices.ToArray();

        void VisitSceneNode(Node* node)
        {
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];

                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];
                    
                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices[i];                      

                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords[0][(int)index];

                        Vertex vertex = new Vertex
                        {
                            pos = new Vector3D<float>(position.X, position.Y, position.Z),
                            color = new Vector3D<float>(1, 1, 1),
                            //Flip Y for OBJ in Vulkan
                            textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                        };

                        if(vertexMap.TryGetValue(vertex, out var meshIndex))
                        {
                            localIndices.Add(meshIndex);
                        }
                        else
                        {
                            localIndices.Add((uint)localVertices.Count);
                            vertexMap[vertex] = (uint)localVertices.Count;
                            localVertices.Add(vertex);
                        }                        
                    }
                }
            }

            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]);
            }
        }
    }


    protected void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);
        
        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
            vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref vertexBuffer, ref vertexBufferMemory);

        CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);
    }

    protected void CreateIndexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices!.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
            indices.AsSpan().CopyTo(new Span<uint>(data, indices.Length));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref indexBuffer, ref indexBufferMemory);

        CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

        vk!.DestroyBuffer(device, stagingBuffer, null);
        vk!.FreeMemory(device, stagingBufferMemory, null);
    }

    protected void CreateUniformBuffers()
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        uniformBuffers = new Buffer[swapChainImages!.Length];
        uniformBuffersMemory = new DeviceMemory[swapChainImages!.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref uniformBuffers[i], ref uniformBuffersMemory[i]);   
        }

    }

    protected void CreateDescriptorPool()
    {
        var poolSizes = new[]
        {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapChainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImages!.Length,
            }
        };
        
        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
        {

            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)swapChainImages!.Length,
            };

            if (vk!.CreateDescriptorPool(device, poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }

        }
    }

    protected void CreateDescriptorSets()
    {
        var layouts = new DescriptorSetLayout[swapChainImages!.Length];
        Array.Fill(layouts, descriptorSetLayout); 

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = (uint)swapChainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            descriptorSets = new DescriptorSet[swapChainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
            {
                if (vk!.AllocateDescriptorSets(device, allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }
        

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = uniformBuffers![i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };

            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = textureImageView,
                Sampler = textureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                vk!.UpdateDescriptorSets(device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
            }
        }

    }

    protected void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            if (vk!.CreateBuffer(device, bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        MemoryRequirements memRequirements;
        vk!.GetBufferMemoryRequirements(device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (vk!.AllocateMemory(device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        vk!.BindBufferMemory(device, buffer, bufferMemory, 0);
    }

    protected CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        CommandBuffer commandBuffer;
        vk!.AllocateCommandBuffers(device, allocateInfo, out commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk!.BeginCommandBuffer(commandBuffer, beginInfo);

        return commandBuffer;
    }

    protected void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
        vk!.QueueWaitIdle(graphicsQueue);

        vk!.FreeCommandBuffers(device, commandPool, 1, commandBuffer);
    }

    protected void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,                
        };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }

    protected uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memProperties;
        vk!.GetPhysicalDeviceMemoryProperties(physicalDevice, out memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }

    protected void CreateCommandBuffers()
    {
        commandBuffers = new CommandBuffer[swapChainFramebuffers!.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };

        fixed(CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk!.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }
        

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if(vk!.BeginCommandBuffer(commandBuffers[i], beginInfo ) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType= StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 }, 
                    Extent = swapChainExtent,
                }
            };

            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
                },
                new()
                {
                    DepthStencil = new () { Depth = 1, Stencil = 0 }
                }
            };


            fixed(ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                vk!.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            }

            vk!.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline);

                var vertexBuffers = new[] { vertexBuffer };
                var offsets = new ulong[] { 0 };

                fixed (ulong* offsetsPtr = offsets)
                fixed (Buffer* vertexBuffersPtr = vertexBuffers)
                {
                    vk!.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr);
                }

                vk!.CmdBindIndexBuffer(commandBuffers[i], indexBuffer, 0, IndexType.Uint32);

                vk!.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, descriptorSets![i], 0, null);

                vk!.CmdDrawIndexed(commandBuffers[i], (uint)indices!.Length, 1, 0, 0, 0);

            vk!.CmdEndRenderPass(commandBuffers[i]);
            
            if (vk!.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }

        }
    }

    

    protected void UpdateUniformBuffer(uint currentImage)
    {
        //Silk Window has timing information so we are skipping the time code.
        var time = (float)window!.Time;

        UniformBufferObject ubo = new()
        {
            model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0,0,1), time * Scalar.DegreesToRadians(90.0f)),
            view = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), swapChainExtent.Width * 1f / swapChainExtent.Height, 0.1f, 10.0f),
        };
        ubo.proj.M22 *= -1;


        void* data;
        vk!.MapMemory(device, uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
            new Span<UniformBufferObject>(data, 1)[0] = ubo;
        vk!.UnmapMemory(device, uniformBuffersMemory![currentImage]);

    }

    protected void DrawFrame(double delta)
    {
        vk!.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

        if(result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if(result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }

        UpdateUniformBuffer(imageIndex);

        if(imagesInFlight![imageIndex].Handle != default)
        {
            vk!.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc [] {imageAvailableSemaphores[currentFrame]};
        var waitStages = stackalloc [] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = commandBuffers![imageIndex];

        submitInfo = submitInfo with 
        { 
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            
            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        vk!.ResetFences(device, 1,inFlightFences[currentFrame]);

        if(vk!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = khrSwapChain.QueuePresent(presentQueue, presentInfo);

        if(result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
        {
            frameBufferResized = false;
            RecreateSwapChain();
        }
        else if(result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

    }

    

    

    

    

    

    

    

    

    

    

    
}