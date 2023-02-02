using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using SilkNetConvenience.Pipelines;
using SilkNetConvenience.ShaderModules;

var app = new HelloTriangleApplication_09();
app.Run();

public unsafe class HelloTriangleApplication_09 : HelloTriangleApplication_08
{
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
    }

    protected VulkanShaderModule CreateShaderModule(byte[] code) {
        return device!.CreateShaderModule(code);
    }
}
