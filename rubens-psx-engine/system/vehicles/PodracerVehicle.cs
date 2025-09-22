using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.entities;
using System;
using anakinsoft.utilities;
using anakinsoft.system.physics;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using XnaMathHelper = Microsoft.Xna.Framework.MathHelper;

namespace rubens_psx_engine.system.vehicles
{
    public class PodracerVehicle
    {
        private PhysicsSystem physicsSystem;
        private Simulation simulation;

        private BodyHandle vehicleBody;
        private RenderingEntity vehicleVisual;

        // Vehicle properties
        private float forwardSpeed = 100f;
        private float boostSpeed = 200f;
        private float backwardSpeed = 40f;
        private float turnSpeed = 3f;
        private float hoverHeight = 3f;
        private float hoverStiffness = 150f;
        private float hoverDamping = 8f;

        // Current input values
        private float currentThrust = 0f;
        private float targetThrust = 0f;
        private float currentSteering = 0f;
        private float targetSteering = 0f;

        // Vehicle dimensions
        private XnaVector3 vehicleSize = new XnaVector3(2f, 0.5f, 3f);

        public XnaVector3 Position
        {
            get
            {
                var pose = simulation.Bodies.GetBodyReference(vehicleBody).Pose;
                return pose.Position.ToVector3();
            }
        }

        public XnaQuaternion Rotation
        {
            get
            {
                var pose = simulation.Bodies.GetBodyReference(vehicleBody).Pose;
                return pose.Orientation.ToQuaternion();
            }
        }

        public XnaVector3 Forward
        {
            get
            {
                var rotation = Rotation;
                return XnaVector3.Transform(XnaVector3.Forward, rotation);
            }
        }

        public XnaVector3 Velocity
        {
            get
            {
                var velocity = simulation.Bodies.GetBodyReference(vehicleBody).Velocity.Linear;
                return velocity.ToVector3();
            }
        }

        public PodracerVehicle(PhysicsSystem physics, XnaVector3 position)
        {
            this.physicsSystem = physics;
            this.simulation = physics.Simulation;

            CreateVehiclePhysics(position);
            CreateVehicleVisual();
        }

        private void CreateVehiclePhysics(XnaVector3 position)
        {
            // Create a simple box shape for the vehicle
            var vehicleShape = new Box(vehicleSize.X, vehicleSize.Y, vehicleSize.Z);
            var vehicleInertia = vehicleShape.ComputeInertia(20f); // Light vehicle for better hover

            var vehicleShapeIndex = simulation.Shapes.Add(vehicleShape);

            var vehiclePose = new RigidPose(position.ToVector3N(), System.Numerics.Quaternion.Identity);
            var vehicleDesc = BodyDescription.CreateDynamic(vehiclePose, vehicleInertia, vehicleShapeIndex, 0.02f);
            vehicleBody = simulation.Bodies.Add(vehicleDesc);
        }

        private void CreateVehicleVisual()
        {
            vehicleVisual = new RenderingEntity("models/cube", "textures/prototype/concrete", "shaders/surface/Unlit", false)
            {
                Scale = vehicleSize
            };
        }

        public void Update(GameTime gameTime, KeyboardState keyboardState)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleInput(keyboardState);
            ApplyHoverForces();
            ApplyMovement(deltaTime);
            UpdateVisual();
        }

        private void HandleInput(KeyboardState keyboardState)
        {
            targetThrust = 0f;
            targetSteering = 0f;

            // Forward/backward input
            if (keyboardState.IsKeyDown(Keys.W))
            {
                targetThrust = keyboardState.IsKeyDown(Keys.LeftShift) ? boostSpeed : forwardSpeed;
            }
            else if (keyboardState.IsKeyDown(Keys.S))
            {
                targetThrust = -backwardSpeed;
            }

            // Steering input
            if (keyboardState.IsKeyDown(Keys.A))
            {
                targetSteering = -1f;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                targetSteering = 1f;
            }

            // Smooth input interpolation
            currentThrust = XnaMathHelper.Lerp(currentThrust, targetThrust, 0.1f);
            currentSteering = XnaMathHelper.Lerp(currentSteering, targetSteering, 0.15f);
        }

        private void ApplyHoverForces()
        {
            var body = simulation.Bodies.GetBodyReference(vehicleBody);
            var position = body.Pose.Position;
            var orientation = body.Pose.Orientation;

            // Calculate the four corners of the vehicle in world space
            var halfSize = new BepuVector3(vehicleSize.X * 0.5f, 0, vehicleSize.Z * 0.5f);
            var corners = new BepuVector3[]
            {
                BepuVector3.Transform(new BepuVector3(-halfSize.X, 0, -halfSize.Z), orientation) + position, // Front-left
                BepuVector3.Transform(new BepuVector3(halfSize.X, 0, -halfSize.Z), orientation) + position,  // Front-right
                BepuVector3.Transform(new BepuVector3(-halfSize.X, 0, halfSize.Z), orientation) + position,  // Back-left
                BepuVector3.Transform(new BepuVector3(halfSize.X, 0, halfSize.Z), orientation) + position    // Back-right
            };

            // Apply hover force to each corner
            var totalHoverForce = BepuVector3.Zero;
            var totalTorque = BepuVector3.Zero;

            for (int i = 0; i < corners.Length; i++)
            {
                var cornerPos = corners[i];

                // Simple ground check - assume ground at Y=0
                float groundHeight = 0f;
                float currentHeight = cornerPos.Y - groundHeight;

                if (currentHeight < hoverHeight * 2f)
                {
                    float heightError = hoverHeight - currentHeight;

                    // Calculate spring force
                    float springForce = heightError * hoverStiffness;

                    // Calculate damping force based on vertical velocity at this corner
                    var cornerVelocity = body.Velocity.Linear + BepuVector3.Cross(body.Velocity.Angular, cornerPos - position);
                    float dampingForce = -cornerVelocity.Y * hoverDamping;

                    float totalForce = (springForce + dampingForce) * 0.25f; // Divide by 4 corners

                    var forceVector = BepuVector3.UnitY * totalForce;
                    totalHoverForce += forceVector;

                    // Calculate torque from this corner
                    var leverArm = cornerPos - position;
                    totalTorque += BepuVector3.Cross(leverArm, forceVector);
                }
            }

            // Apply the combined forces
            if (totalHoverForce.LengthSquared() > 0.001f)
            {
                body.ApplyLinearImpulse(totalHoverForce * deltaTime);
                body.ApplyAngularImpulse(totalTorque * deltaTime * 0.1f); // Reduced torque for stability
            }

            // Apply stabilization torque to keep vehicle upright
            var orientationMatrix = System.Numerics.Matrix4x4.CreateFromQuaternion(orientation);
            var currentUp = BepuVector3.TransformNormal(BepuVector3.UnitY, orientationMatrix);
            var stabilizationTorque = BepuVector3.Cross(currentUp, BepuVector3.UnitY) * 10f;
            body.ApplyAngularImpulse(stabilizationTorque * deltaTime);
        }

        private float deltaTime;

        private void ApplyMovement(float dt)
        {
            deltaTime = dt;
            var body = simulation.Bodies.GetBodyReference(vehicleBody);

            // Apply forward/backward thrust
            if (Math.Abs(currentThrust) > 0.01f)
            {
                var orientationMatrix = System.Numerics.Matrix4x4.CreateFromQuaternion(body.Pose.Orientation);
                var forward = BepuVector3.TransformNormal(BepuVector3.UnitZ, orientationMatrix);

                var thrustForce = forward * currentThrust;
                body.ApplyLinearImpulse(thrustForce * deltaTime);
            }

            // Apply steering (yaw rotation)
            if (Math.Abs(currentSteering) > 0.01f)
            {
                var steeringTorque = BepuVector3.UnitY * currentSteering * turnSpeed;
                body.ApplyAngularImpulse(steeringTorque * deltaTime);
            }

            // Apply air resistance to prevent infinite acceleration
            var linearDrag = body.Velocity.Linear * -0.1f;
            var angularDrag = body.Velocity.Angular * -0.5f;

            body.ApplyLinearImpulse(linearDrag * deltaTime);
            body.ApplyAngularImpulse(angularDrag * deltaTime);
        }

        private void UpdateVisual()
        {
            var body = simulation.Bodies.GetBodyReference(vehicleBody);

            vehicleVisual.Position = body.Pose.Position.ToVector3();
            vehicleVisual.Rotation = body.Pose.Orientation.ToQuaternion();
        }

        public void Draw(GameTime gameTime, Camera camera)
        {
            vehicleVisual.Draw(gameTime, camera);
        }

        public void RemoveFromPhysics()
        {
            if (simulation.Bodies.BodyExists(vehicleBody))
            {
                simulation.Bodies.Remove(vehicleBody);
            }
        }
    }
}