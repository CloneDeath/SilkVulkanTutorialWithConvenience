using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkNetConvenience.Barriers;
using SilkNetConvenience.Exceptions.ResultExceptions;
using SilkNetConvenience.KHR;
using SilkNetConvenience.Queues;

var app = new HelloTriangleApplication_16();
app.Run();

public class HelloTriangleApplication_16 : HelloTriangleApplication_15
{
    protected bool frameBufferResized;
    
    protected override void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "Vulkan",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        window.Resize += FramebufferResizeCallback;
    }

    protected void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }

    protected virtual void CleanUpSwapchain()
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
    }

    protected override void CleanUp()
    {
        CleanUpSwapchain();

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

    protected virtual void RecreateSwapchain()
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
        CreateCommandBuffers();

        imagesInFlight = new VulkanFence[swapchainImages!.Length];
    }

    protected override void CreateSwapchain()
    {
        var swapchainSupport = QuerySwapchainSupport(physicalDevice!);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapchainSupport.Formats);
        var presentMode = ChoosePresentMode(swapchainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapchainSupport.Capabilities);

        var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
        if(swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapchainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInformation creatInfo = new()
        {
            Surface = surface!,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = FindQueueFamilies_05(physicalDevice!);
        var queueFamilyIndices = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily) {
            creatInfo.ImageSharingMode = SharingMode.Concurrent;
            creatInfo.QueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creatInfo.PreTransform = swapchainSupport.Capabilities.CurrentTransform;
        creatInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        creatInfo.PresentMode = presentMode;
        creatInfo.Clipped = true;

        khrSwapchain ??= device!.GetKhrSwapchainExtension();

        swapchain = khrSwapchain.CreateSwapchain(creatInfo);

        swapchainImages = swapchain!.GetImages();

        swapchainImageFormat = surfaceFormat.Format;
        swapchainExtent = extent;
    }

    protected override void DrawFrame(double delta)
    {
        inFlightFences![currentFrame].Wait();

        uint imageIndex = 0;
        var result = khrSwapchain!.KhrSwapchain.AcquireNextImage(device!, swapchain!, long.MaxValue,
                                                    imageAvailableSemaphores![currentFrame], default,
                                                    ref imageIndex);
        if (result == Result.ErrorOutOfDateKhr) {
            RecreateSwapchain();
            return;
        }

        if (result != Result.SuboptimalKhr && result != Result.Success) {
            throw new Exception(result.ToString());
        }

        if (imagesInFlight![imageIndex] != null) {
            imagesInFlight[imageIndex]!.Wait();
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        SubmitInformation submitInfo = new();

        var waitSemaphores = new [] {imageAvailableSemaphores![currentFrame].Semaphore};
        var waitStages = new [] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = commandBuffers![imageIndex];

        submitInfo.WaitSemaphores = waitSemaphores;
        submitInfo.WaitDstStageMask = waitStages;
        submitInfo.CommandBuffers = new[]{buffer.CommandBuffer};

        var signalSemaphores = new[] { renderFinishedSemaphores![currentFrame].Semaphore };
        submitInfo.SignalSemaphores = signalSemaphores;

        inFlightFences[currentFrame].Reset();

        graphicsQueue!.Submit(submitInfo, inFlightFences[currentFrame]);

        var swapchains = new[] { swapchain!.Swapchain };
        PresentInformation presentInfo = new()
        {
            WaitSemaphores = signalSemaphores,
            Swapchains = swapchains,
            ImageIndices = new[]{imageIndex}
        };

        try {
            khrSwapchain!.QueuePresent(presentQueue!, presentInfo);
        }
        catch (VulkanResultException ex) {
            if (ex.Result == Result.ErrorOutOfDateKhr || ex.Result == Result.SuboptimalKhr || frameBufferResized)
            {
                frameBufferResized = true;
            }
            else {
                throw;
            }
        }

        if (frameBufferResized) {
            frameBufferResized = false;
            RecreateSwapchain();
            return;
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
    }
}