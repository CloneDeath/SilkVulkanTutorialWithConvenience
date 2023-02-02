using Silk.NET.Vulkan;
using SilkNetConvenience.Pipelines;

var app = new HelloTriangleApplication_10();
app.Run();

public class HelloTriangleApplication_10 : HelloTriangleApplication_09
{
    protected VulkanPipelineLayout? pipelineLayout;
    
    protected override void CleanUp()
    {
        pipelineLayout!.Dispose();

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

        // ReSharper disable once UnusedVariable
        var shaderStages = new []
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        // ReSharper disable once UnusedVariable
        PipelineVertexInputStateCreateInformation vertexInputInfo = new();

        // ReSharper disable once UnusedVariable
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

        // ReSharper disable once UnusedVariable
        PipelineViewportStateCreateInformation viewportState = new()
        {
            Viewports = new[]{viewport},
            Scissors = new[]{scissor}
        };

        // ReSharper disable once UnusedVariable
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

        // ReSharper disable once UnusedVariable
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
    }
}