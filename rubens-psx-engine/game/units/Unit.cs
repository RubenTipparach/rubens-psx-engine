using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;
using System;

namespace rubens_psx_engine.game.units
{
    public enum UnitState
    {
        Idle,
        Moving,
        Selected,
        Dead
    }

    public enum UnitType
    {
        Worker,
        Soldier,
        Tank
    }

    public class Unit
    {
        public Vector3 Position { get; set; }
        public Vector3 TargetPosition { get; set; }
        public UnitState State { get; set; }
        public UnitType Type { get; set; }
        public Color TeamColor { get; set; }
        public bool IsSelected { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Speed { get; set; }
        public string Name { get; set; }

        // Rendering using proven RenderingEntity system
        private RenderingEntity renderingEntity;
        private RenderingEntity healthBarBg;
        private RenderingEntity healthBarFg;

        // Scale override
        public float ScaleOverride { get; set; } = 1.0f;

        // Terrain conformance
        private rubens_psx_engine.system.terrain.TerrainData terrainData;

        // Movement
        private bool isMoving;
        private float moveProgress;

        public Unit(UnitType type, Vector3 position, Color teamColor, string name = "Unit", float scale = 1.0f)
        {
            Type = type;
            Position = position;
            TargetPosition = position;
            TeamColor = teamColor;
            Name = name;
            ScaleOverride = scale;
            State = UnitState.Idle;
            IsSelected = false;
            isMoving = false;
            moveProgress = 0f;

            // Set unit stats based on type
            SetUnitStats();

            // Create unit mesh and material
            CreateUnitMesh();
        }

        public Unit(UnitType type, Vector3 position, Color teamColor, Material customMaterial, string name = "Unit", float scale = 1.0f)
        {
            Type = type;
            Position = position;
            TargetPosition = position;
            TeamColor = teamColor;
            Name = name;
            ScaleOverride = scale;
            State = UnitState.Idle;
            IsSelected = false;
            isMoving = false;
            moveProgress = 0f;

            // Set unit stats based on type
            SetUnitStats();

            // Create unit mesh with custom material
            CreateUnitMeshWithMaterial(customMaterial);
        }

        private void SetUnitStats()
        {
            switch (Type)
            {
                case UnitType.Worker:
                    MaxHealth = 50f;
                    Speed = 3.0f;
                    break;
                case UnitType.Soldier:
                    MaxHealth = 100f;
                    Speed = 4.0f;
                    break;
                case UnitType.Tank:
                    MaxHealth = 200f;
                    Speed = 2.0f;
                    break;
            }
            Health = MaxHealth;
        }

        private void CreateUnitMesh()
        {
            // Create RenderingEntity using the same proven system as terrain
            renderingEntity = new RenderingEntity("models/cube", "textures/white", "shaders/surface/VertexLitStandard");
            renderingEntity.Position = Position;
            renderingEntity.Scale = Vector3.One * GetUnitScale() * ScaleOverride;
            renderingEntity.Color = TeamColor.ToVector3();

            // Create health bar entities (will be positioned dynamically)
            healthBarBg = new RenderingEntity("models/cube", "textures/white", "shaders/surface/VertexLitStandard");
            healthBarBg.Color = Vector3.UnitX; // Red

            healthBarFg = new RenderingEntity("models/cube", "textures/white", "shaders/surface/VertexLitStandard");
            healthBarFg.Color = Vector3.UnitY; // Green
        }

        private void CreateUnitMeshWithMaterial(Material material)
        {
            // Create MaterialRenderingEntity using custom material
            var materialEntity = new MaterialRenderingEntity("models/cube", material);
            materialEntity.Position = Position;
            materialEntity.Scale = Vector3.One * GetUnitScale() * ScaleOverride;

            // Store as base RenderingEntity for compatibility
            renderingEntity = materialEntity;

            // Create health bar entities (will be positioned dynamically)
            healthBarBg = new RenderingEntity("models/cube", "textures/white", "shaders/surface/VertexLitStandard");
            healthBarBg.Color = Vector3.UnitX; // Red

            healthBarFg = new RenderingEntity("models/cube", "textures/white", "shaders/surface/VertexLitStandard");
            healthBarFg.Color = Vector3.UnitY; // Green
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle movement
            if (State == UnitState.Moving || isMoving)
            {
                UpdateMovement(deltaTime);
            }

            // Update rendering entity position and effects
            UpdateRenderingEntity();
        }

        private void UpdateRenderingEntity()
        {
            if (renderingEntity == null) return;

            // Conform to terrain height if terrain data is available
            ConformToTerrain();

            // Update rendering entity position and scale
            renderingEntity.Position = Position;
            renderingEntity.Scale = Vector3.One * GetUnitScale() * ScaleOverride;

            // Update color (simple team color, no fancy selection effects)
            renderingEntity.Color = TeamColor.ToVector3();
        }

        private void ConformToTerrain()
        {
            if (terrainData != null)
            {
                float terrainHeight = terrainData.GetHeightAt(Position.X, Position.Z);
                Position = new Vector3(Position.X, terrainHeight, Position.Z);
            }
        }

        public void SetTerrainData(rubens_psx_engine.system.terrain.TerrainData terrain)
        {
            terrainData = terrain;
        }

        private void UpdateMovement(float deltaTime)
        {
            Vector3 direction = TargetPosition - Position;
            float distance = direction.Length();

            if (distance < 0.1f)
            {
                // Reached target
                Position = TargetPosition;
                State = UnitState.Idle;
                isMoving = false;
                moveProgress = 0f;
            }
            else
            {
                // Move towards target
                Vector3 moveVector = Vector3.Normalize(direction) * Speed * deltaTime;
                if (moveVector.Length() > distance)
                {
                    Position = TargetPosition;
                    State = UnitState.Idle;
                    isMoving = false;
                }
                else
                {
                    Position += moveVector;
                    State = UnitState.Moving;
                    isMoving = true;
                }
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            TargetPosition = targetPosition;
            State = UnitState.Moving;
            isMoving = true;
            moveProgress = 0f;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            State = selected ? UnitState.Selected : UnitState.Idle;
        }

        public void Draw(rubens_psx_engine.RTSCamera camera)
        {
            if (State == UnitState.Dead || renderingEntity == null) return;

            // Draw main unit using RenderingEntity
            renderingEntity.Draw(null, camera);

            // Draw health bar if damaged
            if (Health < MaxHealth)
            {
                DrawHealthBar(camera);
            }
        }

        private float GetUnitScale()
        {
            switch (Type)
            {
                case UnitType.Worker:
                    return 0.8f;
                case UnitType.Soldier:
                    return 1.0f;
                case UnitType.Tank:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }

        private void DrawHealthBar(rubens_psx_engine.RTSCamera camera)
        {
            float healthPercentage = Health / MaxHealth;
            Vector3 healthBarPosition = Position + Vector3.Up * 2.0f;

            // Health bar background (red)
            healthBarBg.Position = healthBarPosition;
            healthBarBg.Scale = new Vector3(1.0f, 0.1f, 0.1f);
            healthBarBg.Draw(null, camera);

            // Health bar foreground (green)
            if (healthPercentage > 0)
            {
                healthBarFg.Position = healthBarPosition + Vector3.Right * (1.0f - healthPercentage) * 0.5f;
                healthBarFg.Scale = new Vector3(healthPercentage, 0.12f, 0.12f);
                healthBarFg.Draw(null, camera);
            }
        }

        public BoundingBox GetBoundingBox()
        {
            float scale = GetUnitScale() * ScaleOverride;
            Vector3 min = Position - Vector3.One * scale * 0.5f;
            Vector3 max = Position + Vector3.One * scale * 0.5f;
            return new BoundingBox(min, max);
        }

        public void TakeDamage(float damage)
        {
            Health = Math.Max(0, Health - damage);
            if (Health <= 0)
            {
                State = UnitState.Dead;
            }
        }

        public void Heal(float amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }

        public Model GetModel()
        {
            return renderingEntity?.Model;
        }

        public float GetPublicUnitScale()
        {
            return GetUnitScale();
        }
    }

    // Helper camera class for material application
    public class BasicCamera : Camera
    {
        public new Matrix View { get; set; }
        public new Matrix Projection { get; set; }

        public BasicCamera() : base(Globals.screenManager.getGraphicsDevice.GraphicsDevice)
        {
        }
    }
}