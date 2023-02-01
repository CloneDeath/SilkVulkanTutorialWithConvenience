using Silk.NET.Vulkan;
using SilkNetConvenience.CreateInfo;
using SilkNetConvenience.CreateInfo.Pipelines;
using SilkNetConvenience.Wrappers;

var app = new HelloTriangleApplication_11();
app.Run();

public class HelloTriangleApplication_11 : HelloTriangleApplication_10
{
    protected VulkanRenderPass? renderPass;

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
        CreateGraphicsPipeline();
    }

    protected override void CleanUp()
    {
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
    
    protected virtual void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        SubpassDescriptionInformation subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachments = new []{colorAttachmentRef}
        };

        RenderPassCreateInformation renderPassInfo = new() 
        { 
            Attachments = new[]{colorAttachment},
            Subpasses = new[]{subpass}
        };

        renderPass = device!.CreateRenderPass(renderPassInfo);
    }
}