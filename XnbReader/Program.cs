using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.animation;

if (args.Length == 0)
{
    Console.WriteLine("Usage: XnbReader <path-to-xnb-file>");
    return 1;
}

var xnbPath = args[0];

if (!File.Exists(xnbPath))
{
    Console.WriteLine($"ERROR: File not found: {xnbPath}");
    return 1;
}

try
{
    // Initialize MonoGame in headless mode
    using var game = new DummyGame();
    game.InitializeGame();

    var directory = Path.GetDirectoryName(xnbPath) ?? "";
    var fileName = Path.GetFileNameWithoutExtension(xnbPath);

    using var content = new ContentManager(game.Services, directory);

    Console.WriteLine($"Loading: {xnbPath}");
    Console.WriteLine($"Directory: {directory}");
    Console.WriteLine($"File name: {fileName}");
    Console.WriteLine();

    var model = content.Load<Model>(fileName);

    Console.WriteLine("=== MODEL INFO ===");
    Console.WriteLine($"Meshes: {model.Meshes.Count}");
    Console.WriteLine($"Bones: {model.Bones.Count}");
    Console.WriteLine($"Root: {model.Root?.Name ?? "None"}");
    Console.WriteLine();

    Console.WriteLine("=== MESH DETAILS ===");
    foreach (var mesh in model.Meshes)
    {
        Console.WriteLine($"Mesh: {mesh.Name}");
        Console.WriteLine($"  Parts: {mesh.MeshParts.Count}");
        Console.WriteLine($"  Parent Bone: {mesh.ParentBone?.Name ?? "None"}");
    }
    Console.WriteLine();

    Console.WriteLine("=== BONE HIERARCHY ===");
    for (int i = 0; i < model.Bones.Count; i++)
    {
        var bone = model.Bones[i];
        var parent = bone.Parent?.Name ?? "ROOT";
        Console.WriteLine($"[{i}] {bone.Name} -> Parent: {parent}");
    }
    Console.WriteLine();

    var skinningData = model.Tag as SkinningData;

    if (skinningData != null)
    {
        Console.WriteLine("=== SKINNING DATA FOUND ===");
        Console.WriteLine($"Bind Pose Bones: {skinningData.BindPose.Count}");
        Console.WriteLine($"Animation Clips: {skinningData.AnimationClips.Count}");
        Console.WriteLine();

        if (skinningData.AnimationClips.Count > 0)
        {
            Console.WriteLine("=== ANIMATION CLIPS ===");
            foreach (var kvp in skinningData.AnimationClips)
            {
                Console.WriteLine($"\nClip: {kvp.Key}");
                Console.WriteLine($"  Duration: {kvp.Value.Duration.TotalSeconds:F3} seconds");
                Console.WriteLine($"  Keyframes: {kvp.Value.Keyframes.Count}");

                if (kvp.Value.Keyframes.Count > 0)
                {
                    Console.WriteLine($"  First keyframe:");
                    var first = kvp.Value.Keyframes[0];
                    Console.WriteLine($"    Bone: {first.Bone}");
                    Console.WriteLine($"    Time: {first.Time.TotalSeconds:F3}s");
                }
            }
        }

        Console.WriteLine("\n=== SKELETON HIERARCHY ===");
        for (int i = 0; i < skinningData.SkeletonHierarchy.Count; i++)
        {
            var parentIdx = skinningData.SkeletonHierarchy[i];
            Console.WriteLine($"Bone {i}: Parent = {(parentIdx == -1 ? "ROOT" : parentIdx.ToString())}");
        }
    }
    else
    {
        Console.WriteLine("=== NO SKINNING DATA ===");
        Console.WriteLine("Model.Tag is null or not SkinningData type");
        Console.WriteLine($"Model.Tag type: {model.Tag?.GetType().FullName ?? "null"}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    return 1;
}

// Minimal Game class to initialize MonoGame
class DummyGame : Game
{
    private GraphicsDeviceManager graphics;

    public DummyGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 1;
        graphics.PreferredBackBufferHeight = 1;
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    public void InitializeGame()
    {
        base.RunOneFrame();
    }
}
