using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.Buffers;
using SilkNetConvenience.Images;
using SilkNetConvenience.Memory;
using SilkNetConvenience.Pipelines;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_29();
app.Run();

public class HelloTriangleApplication_29 : HelloTriangleApplication_28
{
    protected SampleCountFlags msaaSamples = SampleCountFlags.Count1Bit;

    protected VulkanImage? colorImage;
    protected VulkanDeviceMemory? colorImageMemory;
    protected VulkanImageView? colorImageView;

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

    protected override void CleanUpSwapchain()
    {
        depthImageView!.Dispose();
        depthImage!.Dispose();
        depthImageMemory!.Dispose();

        colorImageView?.Dispose();
        colorImage?.Dispose();
        colorImageMemory?.Dispose();

        foreach (var framebuffer in swapchainFramebuffers!)
        {
            framebuffer.Dispose();
        }

        foreach (var commandBuffer in commandBuffers!) {
            commandBuffer.Dispose();
        }

        graphicsPipeline!.Dispose();
        pipelineLayout!.Dispose();
        renderPass!.Dispose();

        foreach (var imageView in swapchainImageViews!)
        {
            imageView.Dispose();
        }

        swapchain!.Dispose();

        for (int i = 0; i < swapchainImages!.Length; i++)
        {
            uniformBuffers![i].Dispose();
            uniformBuffersMemory![i].Dispose();
        }

        descriptorPool!.Dispose();
    }
    
    protected override void RecreateSwapchain()
    {
        Vector2D<int> framebufferSize = window!.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = window.FramebufferSize;
            window.DoEvents();
        }

        device!.WaitIdle();

        CleanUpSwapchain();

        CreateSwapchain();
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

        imagesInFlight = new VulkanFence[swapchainImages!.Length];
    }

    protected override void PickPhysicalDevice() {
        var devices = instance!.PhysicalDevices;

        foreach (var physDevice in devices)
        {
            if (IsDeviceSuitable(physDevice))
            {
                physicalDevice = physDevice;
                msaaSamples = GetMaxUsableSampleCount();
                break;
            }
        }

        if (physicalDevice == null)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    protected override void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapchainImageFormat,
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
            Format = swapchainImageFormat,
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

        SubpassDescriptionInformation subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachments = new []{colorAttachmentRef},
            DepthStencilAttachment = depthAttachmentRef,
            ResolveAttachments = new[]{colorAttachmentResolveRef},
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

        RenderPassCreateInformation renderPassInfo = new() {
            Attachments = attachments,
            Subpasses = new[]{subpass},
            Dependencies = new[]{dependency},
        };

        renderPass = device!.CreateRenderPass(renderPassInfo);
    }
    
    protected override void CreateGraphicsPipeline()
    {
        var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
        var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

        using var vertShaderModule = CreateShaderModule(vertShaderCode);
        using var fragShaderModule = CreateShaderModule(fragShaderCode);

        PipelineShaderStageCreateInformation vertShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            Name = "main"
        };

        PipelineShaderStageCreateInformation fragShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            Name = "main"
        };

        var shaderStages = new []
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var bindingDescription = Vertex_26.GetBindingDescription();
        var attributeDescriptions = Vertex_26.GetAttributeDescriptions();

        PipelineVertexInputStateCreateInformation vertexInputInfo = new() {
            VertexBindingDescriptions = new[] { bindingDescription },
            VertexAttributeDescriptions = attributeDescriptions,
        };

        PipelineInputAssemblyStateCreateInformation inputAssembly = new() {
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new() {
            X = 0,
            Y = 0,
            Width = swapchainExtent.Width,
            Height = swapchainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new() {
            Offset = { X = 0, Y = 0 },
            Extent = swapchainExtent,
        };

        PipelineViewportStateCreateInformation viewportState = new() {
            Viewports = new[] { viewport },
            Scissors = new[] { scissor }
        };

        PipelineRasterizationStateCreateInformation rasterizer = new() {
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.CounterClockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInformation multisampling = new() {
            SampleShadingEnable = false,
            RasterizationSamples = msaaSamples,
        };

        PipelineDepthStencilStateCreateInformation depthStencil = new() {
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.Less,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new() {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                             ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInformation colorBlending = new() {
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            Attachments = new[] { colorBlendAttachment }
        };

        colorBlending.BlendConstants.X = 0;
        colorBlending.BlendConstants.Y = 0;
        colorBlending.BlendConstants.Z = 0;
        colorBlending.BlendConstants.W = 0;

        PipelineLayoutCreateInformation pipelineLayoutInfo = new() {
            SetLayouts = new[] { descriptorSetLayout!.DescriptorSetLayout }
        };

        pipelineLayout = device!.CreatePipelineLayout(pipelineLayoutInfo);

        GraphicsPipelineCreateInformation pipelineInfo = new() {
            Stages = shaderStages,
            VertexInputState = vertexInputInfo,
            InputAssemblyState = inputAssembly,
            ViewportState = viewportState,
            RasterizationState = rasterizer,
            MultisampleState = multisampling,
            DepthStencilState = depthStencil,
            ColorBlendState = colorBlending,
            Layout = pipelineLayout,
            RenderPass = renderPass!,
            Subpass = 0
        };

        graphicsPipeline = device.CreateGraphicsPipeline(pipelineInfo);


    }

    protected override void CreateFramebuffers()
    {
        swapchainFramebuffers = new VulkanFramebuffer[swapchainImageViews!.Length];

        for(int i = 0; i < swapchainImageViews.Length; i++)
        {
            var attachments = new ImageView[] { colorImageView!, depthImageView!, swapchainImageViews[i] };

            FramebufferCreateInformation framebufferInfo = new() {
                RenderPass = renderPass!,
                Attachments = attachments,
                Width = swapchainExtent.Width,
                Height = swapchainExtent.Height,
                Layers = 1,
            };

            swapchainFramebuffers[i] = device!.CreateFramebuffer(framebufferInfo);
        }
    }
    
    protected void CreateColorResources()
    {
        Format colorFormat = swapchainImageFormat;

        (colorImage, colorImageMemory) = CreateImage(swapchainExtent.Width, swapchainExtent.Height, 1, msaaSamples, colorFormat, ImageTiling.Optimal, ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit, MemoryPropertyFlags.DeviceLocalBit);
        colorImageView = CreateImageView(colorImage, colorFormat, ImageAspectFlags.ColorBit, 1);
    }

    protected override void CreateDepthResources()
    {
        Format depthFormat = FindDepthFormat();

        (depthImage, depthImageMemory) = CreateImage(swapchainExtent.Width, swapchainExtent.Height, 1, msaaSamples, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit);
        depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);
    }

    protected override void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(TEXTURE_PATH);
        
        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        mipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1);

        var (stagingBuffer, stagingBufferMemory) = CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var data = stagingBufferMemory.MapMemory();
        img.CopyPixelDataTo(data);
        stagingBufferMemory.UnmapMemory();

        (textureImage, textureImageMemory) = CreateImage((uint)img.Width, (uint)img.Height, mipLevels, SampleCountFlags.Count1Bit, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit);

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, mipLevels);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        //Transitioned to VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL while generating mipmaps

        stagingBuffer.Dispose();
        stagingBufferMemory.Dispose();

        GenerateMipMaps(textureImage, Format.R8G8B8A8Srgb, (uint)img.Width, (uint)img.Height, mipLevels);
    }
    
    protected SampleCountFlags GetMaxUsableSampleCount() {
        var physicalDeviceProperties = physicalDevice!.GetProperties();

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

    protected (VulkanImage, VulkanDeviceMemory) CreateImage(uint width, uint height, uint withMipLevels, 
                                                            SampleCountFlags numSamples, Format format, ImageTiling tiling, 
                                                            ImageUsageFlags usage, MemoryPropertyFlags properties)
    {
        ImageCreateInformation imageInfo = new()
        {
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

        var image = device!.CreateImage(imageInfo);
        var imageMemory = device!.AllocateMemoryFor(image, properties);
        image.BindMemory(imageMemory);
        return (image, imageMemory);
    }
}