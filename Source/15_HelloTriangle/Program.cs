using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.KHR;
using SilkNetConvenience.Pipelines;
using SilkNetConvenience.Queues;
using SilkNetConvenience.RenderPasses;

var app = new HelloTriangleApplication_15();
app.Run();

public class HelloTriangleApplication_15 : HelloTriangleApplication_14
{
    protected const int MAX_FRAMES_IN_FLIGHT = 2;

    protected VulkanSemaphore[]? imageAvailableSemaphores;
    protected VulkanSemaphore[]? renderFinishedSemaphores;
    protected VulkanFence[]? inFlightFences;
    protected VulkanFence?[]? imagesInFlight;
    protected int currentFrame;

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
        CreateFramebuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    protected override void MainLoop()
    {
        window!.Render += DrawFrame;
        window!.Run();
        device!.WaitIdle();
    }

    protected override void CleanUp()
    {
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            renderFinishedSemaphores![i].Dispose();
            imageAvailableSemaphores![i].Dispose();
            inFlightFences![i].Dispose();
        }

        commandPool?.Dispose();

        foreach (var framebuffer in swapchainFramebuffers!)
        {
            framebuffer.Dispose();
        }

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

    protected override void CreateRenderPass()
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
            ColorAttachments = new[]{colorAttachmentRef}
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };

        RenderPassCreateInformation renderPassInfo = new() 
        {
            Attachments = new[]{colorAttachment},
            Subpasses = new[]{subpass},
            Dependencies = new[]{dependency}
        };

        renderPass = device!.CreateRenderPass(renderPassInfo);
    }

    protected void CreateSyncObjects()
    {
        imageAvailableSemaphores = new VulkanSemaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new VulkanSemaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new VulkanFence[MAX_FRAMES_IN_FLIGHT];
        imagesInFlight = new VulkanFence[swapchainImages!.Length];

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++) {
            imageAvailableSemaphores[i] = device!.CreateSemaphore();
            renderFinishedSemaphores[i] = device!.CreateSemaphore();
            inFlightFences[i] = device!.CreateFence(FenceCreateFlags.SignaledBit);
        }
    }

    protected virtual void DrawFrame(double delta) {
        inFlightFences![currentFrame].Wait();

        var imageIndex = swapchain!.AcquireNextImage(imageAvailableSemaphores![currentFrame]);

        if (imagesInFlight![imageIndex] != null)
        {
            imagesInFlight![imageIndex]!.Wait();
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        var waitSemaphores = new [] {imageAvailableSemaphores[currentFrame].Semaphore};
        var waitStages = new [] { PipelineStageFlags.ColorAttachmentOutputBit };

        SubmitInformation submitInfo = new();
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
        
        khrSwapchain!.QueuePresent(presentQueue!, presentInfo);

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

    }
}