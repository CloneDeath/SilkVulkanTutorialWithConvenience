using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using SilkNetConvenience.CreateInfo.Pipelines;
using SilkNetConvenience.Wrappers.Pipelines;

var app = new HelloTriangleApplication_12();
app.Run();

public unsafe class HelloTriangleApplication_12 : HelloTriangleApplication_11
{
    protected VulkanPipeline? graphicsPipeline;

    protected override void CleanUp()
    {
        graphicsPipeline!.Dispose();
        pipelineLayout!.Dispose();
        renderPass!.Dispose();

        foreach (var imageView in swapchainImageViews!)
        {
            imageView.Dispose();
        }

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

        PipelineVertexInputStateCreateInformation vertexInputInfo = new();

        PipelineInputAssemblyStateCreateInformation inputAssembly = new()
        {
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
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
            Attachments = new[]{colorBlendAttachment}
        };

        colorBlending.BlendConstants.X = 0;
        colorBlending.BlendConstants.Y = 0;
        colorBlending.BlendConstants.Z = 0;
        colorBlending.BlendConstants.W = 0;

        PipelineLayoutCreateInformation pipelineLayoutInfo = new();

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
            Layout = pipelineLayout,
            RenderPass = renderPass!,
            Subpass = 0
        };

        graphicsPipeline = device.CreateGraphicsPipeline(pipelineInfo);
    }
}