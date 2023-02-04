using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.Descriptors;
using SilkNetConvenience.Pipelines;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_22();
app.Run();

public class HelloTriangleApplication_22 : HelloTriangleApplication_21
{
    protected VulkanDescriptorPool? descriptorPool;
    protected VulkanDescriptorSet[]? descriptorSets;

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
        CreateDescriptorPool();
        CreateDescriptorSets();
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
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();

        imagesInFlight = new VulkanFence[swapchainImages!.Length];
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
            RasterizationSamples = SampleCountFlags.Count1Bit,
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
            ColorBlendState = colorBlending,
            Layout = pipelineLayout,
            RenderPass = renderPass!,
            Subpass = 0,
        };

        graphicsPipeline = device.CreateGraphicsPipeline(pipelineInfo);


    }

    protected virtual void CreateDescriptorPool()
    {
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = (uint)swapchainImages!.Length,
        };


        DescriptorPoolCreateInformation poolInfo = new()
        {
            PoolSizes = new[]{poolSize},
            MaxSets = (uint)swapchainImages!.Length,
        };

        descriptorPool = device!.CreateDescriptorPool(poolInfo);
    }

    protected virtual void CreateDescriptorSets()
    {
        descriptorSets = descriptorPool!.AllocateDescriptorSets(swapchainImages!.Length, descriptorSetLayout!);

        for (int i = 0; i < swapchainImages.Length; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = uniformBuffers![i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };

            WriteDescriptorSetInformation descriptorWrite = new()
            {
                DstSet = descriptorSets[i],
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                BufferInfo = new[]{bufferInfo}
            };

            device!.UpdateDescriptorSets(descriptorWrite);
        }

    }

    protected override void CreateCommandBuffers()
    {
        commandBuffers = commandPool!.AllocateCommandBuffers((uint)swapchainFramebuffers!.Length);

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            commandBuffers[i].Begin();

            RenderPassBeginInformation renderPassInfo = new()
            {
                RenderPass = renderPass!,
                Framebuffer = swapchainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 }, 
                    Extent = swapchainExtent,
                }
            };

            ClearValue clearColor = new()
            {
                Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },                
            };

            renderPassInfo.ClearValues = new[]{clearColor};

            commandBuffers[i].BeginRenderPass(renderPassInfo, SubpassContents.Inline);
                commandBuffers[i].BindPipeline(PipelineBindPoint.Graphics, graphicsPipeline!);
                commandBuffers[i].BindVertexBuffer(0, vertexBuffer!);
                commandBuffers[i].BindIndexBuffer(indexBuffer!, 0, IndexType.Uint16);
                commandBuffers[i].BindDescriptorSet(PipelineBindPoint.Graphics, pipelineLayout!, 0, descriptorSets![i]);
                commandBuffers[i].DrawIndexed((uint)indices.Length);
            commandBuffers[i].EndRenderPass();
            commandBuffers[i].End();

        }
    }
}