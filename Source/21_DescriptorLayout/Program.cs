using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.Buffers;
using SilkNetConvenience.Descriptors;
using SilkNetConvenience.Exceptions.ResultExceptions;
using SilkNetConvenience.KHR;
using SilkNetConvenience.Memory;
using SilkNetConvenience.Pipelines;
using SilkNetConvenience.Queues;

var app = new HelloTriangleApplication_21();
app.Run();

public struct UniformBufferObject
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;
}

public class HelloTriangleApplication_21 : HelloTriangleApplication_20
{
    protected VulkanDescriptorSetLayout? descriptorSetLayout;

    protected VulkanBuffer[]? uniformBuffers;
    protected VulkanDeviceMemory[]? uniformBuffersMemory;

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
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffers();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void CleanUpSwapchain()
    {
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
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

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
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateCommandBuffers();

        imagesInFlight = new VulkanFence[swapchainImages!.Length];
    }

    protected virtual void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBindingInformation uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutCreateInformation layoutInfo = new()
        {
            Bindings = new []{uboLayoutBinding}
        };

        descriptorSetLayout = device!.CreateDescriptorSetLayout(layoutInfo);
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

        var bindingDescription = Vertex_17.GetBindingDescription();
        var attributeDescriptions = Vertex_17.GetAttributeDescriptions();

        PipelineVertexInputStateCreateInformation vertexInputInfo = new()
        {
            VertexBindingDescriptions = new[]{bindingDescription},
            VertexAttributeDescriptions = attributeDescriptions
        };

        PipelineInputAssemblyStateCreateInformation inputAssembly = new()
        {
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new()
        {
            X = 0,
            Y = 0,
            Width = swapchainExtent.Width,
            Height = swapchainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new()
        {
            Offset = { X = 0, Y = 0 },
            Extent = swapchainExtent,
        };

        PipelineViewportStateCreateInformation viewportState = new()
        {
            Viewports = new[]{viewport},
            Scissors = new[]{scissor}
        };

        PipelineRasterizationStateCreateInformation rasterizer = new()
        {
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInformation multisampling = new()
        {
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInformation colorBlending = new()
        {
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            Attachments = new[]{colorBlendAttachment},
        };

        colorBlending.BlendConstants.X = 0;
        colorBlending.BlendConstants.Y = 0;
        colorBlending.BlendConstants.Z = 0;
        colorBlending.BlendConstants.W = 0;

        PipelineLayoutCreateInformation pipelineLayoutInfo = new()
        {
            SetLayouts = new []{descriptorSetLayout!.DescriptorSetLayout}
        };

        pipelineLayout = device!.CreatePipelineLayout(pipelineLayoutInfo);

        GraphicsPipelineCreateInformation pipelineInfo = new()
        {
            Stages = shaderStages,
            VertexInputState = vertexInputInfo,
            InputAssemblyState = inputAssembly,
            ViewportState = viewportState,
            RasterizationState = rasterizer,
            MultisampleState = multisampling,
            ColorBlendState = colorBlending,
            Layout = pipelineLayout!,
            RenderPass = renderPass!,
            Subpass = 0
        };

        graphicsPipeline = device!.CreateGraphicsPipeline(pipelineInfo);
    }

    protected void CreateUniformBuffers()
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        uniformBuffers = new VulkanBuffer[swapchainImages!.Length];
        uniformBuffersMemory = new VulkanDeviceMemory[swapchainImages!.Length];

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            (uniformBuffers[i], uniformBuffersMemory[i]) = CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);   
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
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), swapchainExtent.Width * 1f / swapchainExtent.Height, 0.1f, 10.0f),
        };
        ubo.proj.M22 *= -1;

        var data = uniformBuffersMemory![currentImage].MapMemory<UniformBufferObject>();
        data[0] = ubo;
        uniformBuffersMemory![currentImage].UnmapMemory();
    }

    protected override void DrawFrame(double delta)
    {
        inFlightFences![currentFrame].Wait();

        uint imageIndex;
        try {
            imageIndex = swapchain!.AcquireNextImage(imageAvailableSemaphores![currentFrame]);
        }
        catch (ErrorOutOfDateKhrException) {
            RecreateSwapchain();
            return;
        }

        UpdateUniformBuffer(imageIndex);

        if(imagesInFlight![imageIndex] != null)
        {
            imagesInFlight![imageIndex]!.Wait();
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        SubmitInformation submitInfo = new();

        var waitSemaphores = new [] {imageAvailableSemaphores[currentFrame].Semaphore};
        var waitStages = new [] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = commandBuffers![imageIndex];

        submitInfo.WaitSemaphores = waitSemaphores;
        submitInfo.WaitDstStageMask = waitStages;
        submitInfo.CommandBuffers = new[]{buffer.CommandBuffer};

        var signalSemaphores = new[] { renderFinishedSemaphores![currentFrame].Semaphore };
        submitInfo.SignalSemaphores = signalSemaphores;

        inFlightFences[currentFrame].Reset();

        graphicsQueue!.Submit(submitInfo, inFlightFences[currentFrame]);

        var swapchains = new[] { swapchain.Swapchain };
        PresentInformation presentInfo = new()
        {
            WaitSemaphores = signalSemaphores,
            Swapchains = swapchains,
            ImageIndices = new[]{imageIndex}
        };

        try {
            khrSwapchain!.QueuePresent(presentQueue!, presentInfo);
        }
        catch (ErrorOutOfDateKhrException) {
            frameBufferResized = true;
        }
        catch (SuboptimalKhrException) {
            frameBufferResized = true;
        }
        if (frameBufferResized){
            frameBufferResized = false;
            RecreateSwapchain();
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

    }
}