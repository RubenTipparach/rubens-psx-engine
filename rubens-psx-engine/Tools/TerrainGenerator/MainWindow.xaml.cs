using Microsoft.Win32;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProceduralTerrain;

namespace TerrainGenerator
{
    public partial class MainWindow : Window
    {
        private TerrainData currentTerrain;
        private Thread previewThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int width = int.Parse(WidthTextBox.Text);
                int height = int.Parse(HeightTextBox.Text);
                int seed = int.Parse(SeedTextBox.Text);
                float noiseScale = (float)NoiseScaleSlider.Value;
                int octaves = (int)OctavesSlider.Value;
                float persistence = (float)PersistenceSlider.Value;
                float heightScale = (float)HeightScaleSlider.Value;

                currentTerrain = new TerrainData(width, height);
                currentTerrain.HeightScale = heightScale;
                currentTerrain.GenerateTerrain(seed, noiseScale, octaves, persistence);

                UpdateHeightmapPreview();

                PreviewButton.IsEnabled = true;
                SaveButton.IsEnabled = true;
                SaveHeightmapButton.IsEnabled = true;

                //MessageBox.Show("Terrain generated successfully!", "Success",
                //              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating terrain: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RandomSeedButton_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            SeedTextBox.Text = random.Next(0, 100000).ToString();
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTerrain == null)
            {
                MessageBox.Show("Please generate terrain first!", "Warning",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (previewThread != null && previewThread.IsAlive)
            {
                MessageBox.Show("Preview window is already open!", "Information",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            previewThread = new Thread(() =>
            {
                using (var preview = new TerrainPreview(currentTerrain))
                {
                    preview.Run();
                }
            });

            previewThread.SetApartmentState(ApartmentState.STA);
            previewThread.Start();

            //MessageBox.Show("Preview window opened!\n\nControls:\n" +
            //              "W/S - Move Forward/Backward\n" +
            //              "Q/E - Strafe Left/Right\n" +
            //              "A/D - Turn Left/Right\n" +
            //              "Space/Shift - Move Up/Down\n" +
            //              "Right Mouse - Look Around\n" +
            //              "ESC - Close Preview",
            //              "Preview Controls", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTerrain == null)
            {
                MessageBox.Show("Please generate terrain first!", "Warning",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                           @"..\..\Content\Assets\models"));
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            saveFileDialog.InitialDirectory = basePath;
            saveFileDialog.FileName = FilenameTextBox.Text;

            if (ObjRadio.IsChecked == true)
            {
                saveFileDialog.Filter = "Wavefront OBJ files (*.obj)|*.obj";
                saveFileDialog.DefaultExt = ".obj";
            }
            else
            {
                saveFileDialog.Filter = "Autodesk FBX files (*.fbx)|*.fbx";
                saveFileDialog.DefaultExt = ".fbx";
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (ObjRadio.IsChecked == true)
                    {
                        currentTerrain.SaveToOBJ(saveFileDialog.FileName);
                    }
                    else
                    {
                        currentTerrain.SaveToFBX(saveFileDialog.FileName);
                    }

                    string mtlPath = Path.ChangeExtension(saveFileDialog.FileName, ".mtl");
                    CreateMaterialFile(mtlPath);

                    MessageBox.Show($"Terrain saved successfully to:\n{saveFileDialog.FileName}",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving terrain: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateMaterialFile(string mtlPath)
        {
            using (StreamWriter writer = new StreamWriter(mtlPath))
            {
                writer.WriteLine("# Material file for terrain");
                writer.WriteLine("newmtl TerrainMaterial");
                writer.WriteLine("Ns 96.078431");
                writer.WriteLine("Ka 1.000000 1.000000 1.000000");
                writer.WriteLine("Kd 0.640000 0.640000 0.640000");
                writer.WriteLine("Ks 0.500000 0.500000 0.500000");
                writer.WriteLine("Ke 0.000000 0.000000 0.000000");
                writer.WriteLine("Ni 1.000000");
                writer.WriteLine("d 1.000000");
                writer.WriteLine("illum 2");
                writer.WriteLine("map_Kd ..\\textures\\prototype\\prototype_512x512_green1.png");
            }
        }

        private void UpdateHeightmapPreview()
        {
            if (currentTerrain == null) return;

            using (var bitmap = HeightmapUtilities.GenerateHeightmapImage(currentTerrain.HeightMap))
            {
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    HeightmapPreviewImage.Source = bitmapImage;
                }
            }
        }

        private void SaveHeightmapButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTerrain == null)
            {
                MessageBox.Show("Please generate terrain first!", "Warning",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                           @"..\..\Content\Assets\textures"));
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            saveFileDialog.InitialDirectory = basePath;
            saveFileDialog.FileName = FilenameTextBox.Text + "_heightmap";
            saveFileDialog.Filter = "PNG Image (*.png)|*.png";
            saveFileDialog.DefaultExt = ".png";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    HeightmapUtilities.SaveHeightmapTexture(currentTerrain, saveFileDialog.FileName);
                    MessageBox.Show($"Heightmap saved successfully to:\n{saveFileDialog.FileName}",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving heightmap: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (previewThread != null && previewThread.IsAlive)
            {
                previewThread.Join(1000);
            }
        }
    }
}