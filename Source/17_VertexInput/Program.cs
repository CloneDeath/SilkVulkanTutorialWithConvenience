using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SilkNetConvenience.Pipelines;

var app = new HelloTriangleApplication_17();
app.Run();

public struct Vertex_17
{
    public Vector2D<float> pos;
    public Vector3D<float> color;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex_17>(),
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
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_17>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex_17>(nameof(color)),
            }
        };

        return attributeDescriptions;
    }
}

public class HelloTriangleApplication_17 : HelloTriangleApplication_16
{
    protected virtual Vertex_17[] vertices { get; } = new Vertex_17[]
    {
        new Vertex_17 { pos = new Vector2D<float>(0.0f,-0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f) },
        new Vertex_17 { pos = new Vector2D<float>(0.5f,0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f) },
        new Vertex_17 { pos = new Vector2D<float>(-0.5f,0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f) },
    };

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
            VertexBindingDescriptions = new []{ bindingDescription },
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
            Attachments = new [] {colorBlendAttachment}
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

        graphicsPipeline = device!.CreateGraphicsPipeline(pipelineInfo);
        
    }
}