var app = new HelloTriangleApplication_08();
app.Run();

public class HelloTriangleApplication_08 : HelloTriangleApplication_07
{
    protected override void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
        CreateImageViews();
        CreateGraphicsPipeline();
    }
    
    protected virtual void CreateGraphicsPipeline()
    {

    }
}