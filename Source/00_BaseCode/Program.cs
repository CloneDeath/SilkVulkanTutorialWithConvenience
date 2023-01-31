using Silk.NET.Maths;
using Silk.NET.Windowing;

var app = new HelloTriangleApplication_00();
app.Run();

public class HelloTriangleApplication_00
{
    protected const int WIDTH = 800;
    protected const int HEIGHT = 600;

    protected IWindow? window;

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    protected virtual void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(WIDTH, HEIGHT),
            Title = "Vulkan"
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    protected virtual void InitVulkan()
    {
        
    }

    protected virtual void MainLoop()
    {
        window!.Run();
    }

    protected virtual void CleanUp()
    {
        window?.Dispose();
    }
}