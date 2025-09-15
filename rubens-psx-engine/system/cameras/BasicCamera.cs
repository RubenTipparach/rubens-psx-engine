using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using rubens_psx_engine;

namespace anakinsoft.system.cameras
{
    /// <summary>
    /// Basic camera that automatically updates view and projection matrices when position/rotation change
    /// </summary>
    public class BasicCamera : Camera
    {
        private Quaternion _rotationQuaternion = Quaternion.Identity;
        private bool _matrixDirty = true;

        public BasicCamera(GraphicsDevice graphicsDevice, Vector3 position, Vector3 rotation) 
            : base(graphicsDevice)
        {
            Position = position;
            SetRotationFromEuler(rotation);
            UpdateMatrices();
        }

        public BasicCamera(GraphicsDevice graphicsDevice, Vector3 position, Quaternion rotation) 
            : base(graphicsDevice)
        {
            Position = position;
            _rotationQuaternion = rotation;
            SyncEulerToBaseClass();
            UpdateMatrices();
        }

        public BasicCamera(GraphicsDevice graphicsDevice, Vector3 position) 
            : this(graphicsDevice, position, Vector3.Zero)
        {
        }

        /// <summary>
        /// Internal quaternion rotation (preferred for calculations)
        /// </summary>
        public Quaternion RotationQuaternion
        {
            get => _rotationQuaternion;
            set
            {
                if (_rotationQuaternion != value)
                {
                    _rotationQuaternion = Quaternion.Normalize(value);
                    SyncEulerToBaseClass();
                    _matrixDirty = true;
                }
            }
        }

        /// <summary>
        /// Camera rotation (Pitch, Yaw, Roll) in radians - converted from quaternion
        /// </summary>
        public Vector3 Rotation
        {
            get => QuaternionToEuler(_rotationQuaternion);
            set => SetRotationFromEuler(value);
        }

        /// <summary>
        /// Camera pitch (X-axis rotation) in radians
        /// </summary>
        public float Pitch
        {
            get => QuaternionToEuler(_rotationQuaternion).X;
            set
            {
                var euler = QuaternionToEuler(_rotationQuaternion);
                if (euler.X != value)
                {
                    euler.X = value;
                    SetRotationFromEuler(euler);
                }
            }
        }

        /// <summary>
        /// Camera yaw (Y-axis rotation) in radians
        /// </summary>
        public float Yaw
        {
            get => QuaternionToEuler(_rotationQuaternion).Y;
            set
            {
                var euler = QuaternionToEuler(_rotationQuaternion);
                if (euler.Y != value)
                {
                    euler.Y = value;
                    SetRotationFromEuler(euler);
                }
            }
        }

        /// <summary>
        /// Camera roll (Z-axis rotation) in radians
        /// </summary>
        public float Roll
        {
            get => QuaternionToEuler(_rotationQuaternion).Z;
            set
            {
                var euler = QuaternionToEuler(_rotationQuaternion);
                if (euler.Z != value)
                {
                    euler.Z = value;
                    SetRotationFromEuler(euler);
                }
            }
        }

        /// <summary>
        /// Set the camera to look at a specific direction
        /// </summary>
        /// <param name="direction">Normalized direction vector</param>
        public void LookAtDirection(Vector3 direction)
        {
            direction = Vector3.Normalize(direction);
            
            // Create quaternion that looks in the given direction
            var lookRotation = CreateLookRotation(direction, Vector3.Up);
            RotationQuaternion = lookRotation;
        }

        /// <summary>
        /// Set the camera to look at a specific point
        /// </summary>
        /// <param name="target">Target position to look at</param>
        public void LookAt(Vector3 target)
        {
            Vector3 direction = target - Position;
            if (direction.LengthSquared() > 0.001f)
            {
                LookAtDirection(direction);
            }
        }

        /// <summary>
        /// Translate the camera by the given offset
        /// </summary>
        /// <param name="translation">Translation offset</param>
        public void Translate(Vector3 translation)
        {
            Position += translation;
        }

        /// <summary>
        /// Rotate the camera by the given offset (Euler angles in radians)
        /// </summary>
        /// <param name="rotationDelta">Rotation offset in radians</param>
        public void Rotate(Vector3 rotationDelta)
        {
            var deltaQuaternion = Quaternion.CreateFromYawPitchRoll(rotationDelta.Y, rotationDelta.X, rotationDelta.Z);
            RotationQuaternion = Quaternion.Normalize(_rotationQuaternion * deltaQuaternion);
        }

        /// <summary>
        /// Rotate the camera by the given quaternion offset
        /// </summary>
        /// <param name="rotationDelta">Quaternion rotation offset</param>
        public void Rotate(Quaternion rotationDelta)
        {
            RotationQuaternion = Quaternion.Normalize(_rotationQuaternion * rotationDelta);
        }

        /// <summary>
        /// Move the camera relative to its current orientation
        /// </summary>
        /// <param name="localTranslation">Translation in camera's local space</param>
        public void MoveRelative(Vector3 localTranslation)
        {
            EnsureMatricesUpdated();
            Vector3 worldTranslation = Right * localTranslation.X + 
                                     Up * localTranslation.Y + 
                                     Forward * localTranslation.Z;
            Position += worldTranslation;
        }

        /// <summary>
        /// Smoothly interpolate between current camera state and target state
        /// </summary>
        /// <param name="targetPosition">Target position</param>
        /// <param name="targetRotation">Target rotation quaternion</param>
        /// <param name="t">Interpolation factor (0 = current, 1 = target)</param>
        public void Lerp(Vector3 targetPosition, Quaternion targetRotation, float t)
        {
            Position = Vector3.Lerp(Position, targetPosition, t);
            RotationQuaternion = Quaternion.Slerp(_rotationQuaternion, targetRotation, t);
        }

        public override void Update(GameTime gameTime)
        {
            EnsureMatricesUpdated();
            base.Update(gameTime);
        }

        /// <summary>
        /// Ensure matrices are updated if they're dirty
        /// </summary>
        private void EnsureMatricesUpdated()
        {
            if (_matrixDirty)
            {
                UpdateMatrices();
                _matrixDirty = false;
            }
        }

        /// <summary>
        /// Update target and up vectors based on current quaternion rotation
        /// </summary>
        private void UpdateMatrices()
        {
            // Create rotation matrix from quaternion
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(_rotationQuaternion);
            
            // Calculate forward direction and target
            Vector3 forward = Vector3.Transform(Vector3.Forward, rotationMatrix);
            Target = Position + forward;
            
            // Calculate up vector (accounting for roll)
            Up = Vector3.Transform(Vector3.Up, rotationMatrix);
        }

        /// <summary>
        /// Set rotation from Euler angles and sync with base class
        /// </summary>
        /// <param name="euler">Euler angles in radians (pitch, yaw, roll)</param>
        private void SetRotationFromEuler(Vector3 euler)
        {
            _rotationQuaternion = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
            SyncEulerToBaseClass();
            _matrixDirty = true;
        }

        /// <summary>
        /// Sync the current quaternion rotation to the base class pitch/yaw fields
        /// </summary>
        private void SyncEulerToBaseClass()
        {
            var euler = QuaternionToEuler(_rotationQuaternion);
            pitch = euler.X;
            yaw = euler.Y;
        }

        /// <summary>
        /// Convert quaternion to Euler angles (pitch, yaw, roll) in radians
        /// </summary>
        /// <param name="quaternion">Input quaternion</param>
        /// <returns>Euler angles as Vector3 (X=pitch, Y=yaw, Z=roll)</returns>
        private static Vector3 QuaternionToEuler(Quaternion quaternion)
        {
            Vector3 euler = new Vector3();

            // Normalize quaternion
            quaternion.Normalize();

            // Extract pitch (X-axis rotation)
            float sinr_cosp = 2 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            float cosr_cosp = 1 - 2 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
            euler.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // Extract yaw (Y-axis rotation)
            float sinp = 2 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            if (Math.Abs(sinp) >= 1)
            {
                euler.Y = (float)Math.CopySign(Math.PI / 2, sinp); // Use 90 degrees if out of range
            }
            else
            {
                euler.Y = (float)Math.Asin(sinp);
            }

            // Extract roll (Z-axis rotation)
            float siny_cosp = 2 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            float cosy_cosp = 1 - 2 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            euler.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return euler;
        }

        /// <summary>
        /// Create a look rotation quaternion from a forward direction and up vector
        /// </summary>
        /// <param name="forward">Forward direction (normalized)</param>
        /// <param name="up">Up direction (normalized)</param>
        /// <returns>Look rotation quaternion</returns>
        private static Quaternion CreateLookRotation(Vector3 forward, Vector3 up)
        {
            // Ensure vectors are normalized
            forward.Normalize();
            up.Normalize();

            // Create right vector
            Vector3 right = Vector3.Cross(up, forward);
            right.Normalize();

            // Recalculate up to ensure orthogonality
            up = Vector3.Cross(forward, right);

            // Create rotation matrix and convert to quaternion
            Matrix rotationMatrix = new Matrix(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                forward.X, forward.Y, forward.Z, 0,
                0, 0, 0, 1
            );

            return Quaternion.CreateFromRotationMatrix(rotationMatrix);
        }
    }
}