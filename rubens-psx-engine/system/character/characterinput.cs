﻿using anakinsoft.system.character;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Demos.Demos.Characters;

/// <summary>
/// Convenience structure that wraps a CharacterController reference and its associated body.
/// </summary>
/// <remarks>
/// <para>This should be treated as an example- nothing here is intended to suggest how you *must* handle characters. 
/// On the contrary, this does some fairly inefficient stuff if you're dealing with hundreds of characters in a predictable way.
/// It's just a fairly convenient interface for demos usage.</para>
/// <para>Note that all characters are dynamic and respond to constraints and forces in the simulation.</para>
/// </remarks>
public struct CharacterInput
{
    BodyHandle bodyHandle;
    CharacterControllers characters;
    float speed;
    Capsule shape;

    public BodyHandle BodyHandle { get { return bodyHandle; } }

    public BodyReference Body
    {
        get
        {
            ref var character = ref characters.GetCharacterByBodyHandle(bodyHandle);

            return new BodyReference(bodyHandle, characters.Simulation.Bodies);
        }
    }

    public CharacterInput(CharacterControllers characters, Vector3 initialPosition, Capsule shape,
        float minimumSpeculativeMargin, float mass, float maximumHorizontalForce, float maximumVerticalGlueForce,
        float jumpVelocity, float speed, float maximumSlope = MathF.PI * 0.25f)
    {
        this.characters = characters;
        var shapeIndex = characters.Simulation.Shapes.Add(shape);

        //Because characters are dynamic, they require a defined BodyInertia. For the purposes of the demos, we don't want them to rotate or fall over, so the inverse inertia tensor is left at its default value of all zeroes.
        //This is effectively equivalent to giving it an infinite inertia tensor- in other words, no torque will cause it to rotate.
        bodyHandle = characters.Simulation.Bodies.Add(
            BodyDescription.CreateDynamic(initialPosition, new BodyInertia { InverseMass = 1f / mass },
            new(shapeIndex, minimumSpeculativeMargin, float.MaxValue, ContinuousDetection.Passive), shape.Radius * 0.02f));
        ref var character = ref characters.AllocateCharacter(bodyHandle);
        character.LocalUp = new Vector3(0, 1, 0);
        character.CosMaximumSlope = MathF.Cos(maximumSlope);
        character.JumpVelocity = jumpVelocity;
        character.MaximumVerticalForce = maximumVerticalGlueForce;
        character.MaximumHorizontalForce = maximumHorizontalForce;
        character.MinimumSupportDepth = shape.Radius * -0.01f;
        character.MinimumSupportContinuationDepth = -minimumSpeculativeMargin;
        this.speed = speed;
        this.shape = shape;
    }

    static Keys MoveForward = Keys.W;
    static Keys MoveBackward = Keys.S;
    static Keys MoveRight = Keys.D;
    static Keys MoveLeft = Keys.A;
    static Keys Sprint = Keys.LeftShift;
    static Keys Jump = Keys.Space;
    static Keys JumpAlternate = Keys.Back; //I have a weird keyboard.
    bool isJumpHeld = false;

    // 
    public bool JumpWasPushed(KeyboardState input, Keys jump)
    {
        if (input.IsKeyDown(jump))
        {
            isJumpHeld = true;
        }
        if (input.IsKeyUp(jump) && isJumpHeld)
        {
            isJumpHeld = false;
            return true;
        }

        return false;
    }

    public void UpdateCharacterGoals(KeyboardState input, Camera camera, float simulationTimestepDuration)
    {
        var jumpWasPushed = JumpWasPushed(input, Jump);
        Vector2 movementDirection = default;
        var speed = 100;
        if (input.IsKeyDown(MoveForward))
        {
            movementDirection = new Vector2(0, 1) * speed;
        }
        if (input.IsKeyDown(MoveBackward))
        {
            movementDirection += new Vector2(0, -1) * speed;
        }
        if (input.IsKeyDown(MoveLeft))
        {
            movementDirection += new Vector2(-1, 0) * speed;
        }
        if (input.IsKeyDown(MoveRight))
        {
            movementDirection += new Vector2(1, 0) * speed;
        }
        var movementDirectionLengthSquared = movementDirection.LengthSquared();
        if (movementDirectionLengthSquared > 0)
        {
            movementDirection /= MathF.Sqrt(movementDirectionLengthSquared);
        }

        ref var character = ref characters.GetCharacterByBodyHandle(bodyHandle);
        character.TryJump = jumpWasPushed; //|| input.WasPushed(JumpAlternate);

        var characterBody = new BodyReference(bodyHandle, characters.Simulation.Bodies);
        var effectiveSpeed = input.IsKeyDown(Sprint) ? speed * 1.75f : speed;

        var newTargetVelocity = movementDirection * effectiveSpeed;

        var viewDirection = camera.Forward; // is this the view direction? idk lol.

        //Modifying the character's raw data does not automatically wake the character up, so we do so explicitly if necessary.
        //If you don't explicitly wake the character up, it won't respond to the changed motion goals.
        //(You can also specify a negative deactivation threshold in the BodyActivityDescription to prevent the character from sleeping at all.)
        if (!characterBody.Awake &&
            ((character.TryJump && character.Supported) ||
            newTargetVelocity != character.TargetVelocity ||
            (newTargetVelocity != Vector2.Zero && character.ViewDirection != viewDirection)))
        {
            characters.Simulation.Awakener.AwakenBody(character.BodyHandle);
        }
        character.TargetVelocity = newTargetVelocity;
        character.ViewDirection = viewDirection.ToVector3N();

        //The character's motion constraints aren't active while the character is in the air, so if we want air control, we'll need to apply it ourselves.
        //(You could also modify the constraints to do this, but the robustness of solved constraints tends to be a lot less important for air control.)
        //There isn't any one 'correct' way to implement air control- it's a nonphysical gameplay thing, and this is just one way to do it.
        //Note that this permits accelerating along a particular direction, and never attempts to slow down the character.
        //This allows some movement quirks common in some game character controllers.
        //Consider what happens if, starting from a standstill, you accelerate fully along X, then along Z- your full velocity magnitude will be sqrt(2) * maximumAirSpeed.
        //Feel free to try alternative implementations. Again, there is no one correct approach.
        if (!character.Supported && movementDirectionLengthSquared > 0)
        {
            QuaternionEx.Transform(character.LocalUp, characterBody.Pose.Orientation, out var characterUp);
            var characterRight = Vector3.Cross(character.ViewDirection, characterUp);
            var rightLengthSquared = characterRight.LengthSquared();
            if (rightLengthSquared > 1e-10f)
            {
                characterRight /= MathF.Sqrt(rightLengthSquared);
                var characterForward = Vector3.Cross(characterUp, characterRight);
                var worldMovementDirection = characterRight * movementDirection.X + characterForward * movementDirection.Y;
                var currentVelocity = Vector3.Dot(characterBody.Velocity.Linear, worldMovementDirection);
                //We'll arbitrarily set air control to be a fraction of supported movement's speed/force.
                const float airControlForceScale = .2f;
                const float airControlSpeedScale = .2f;
                var airAccelerationDt = characterBody.LocalInertia.InverseMass * character.MaximumHorizontalForce * airControlForceScale * simulationTimestepDuration;
                var maximumAirSpeed = effectiveSpeed * airControlSpeedScale;
                var targetVelocity = MathF.Min(currentVelocity + airAccelerationDt, maximumAirSpeed);
                //While we shouldn't allow the character to continue accelerating in the air indefinitely, trying to move in a given direction should never slow us down in that direction.
                var velocityChangeAlongMovementDirection = MathF.Max(0, targetVelocity - currentVelocity);
                characterBody.Velocity.Linear += worldMovementDirection * velocityChangeAlongMovementDirection;
                Debug.Assert(characterBody.Awake, "Velocity changes don't automatically update objects; the character should have already been woken up before applying air control.");
            }
        }
    }

    public void UpdateCameraPosition(Camera camera, float cameraBackwardOffsetScale = 10)
    {
        //We'll override the demo harness's camera control by attaching the camera to the character controller body.
        ref var character = ref characters.GetCharacterByBodyHandle(bodyHandle);
        var c = new BodyReference(bodyHandle, characters.Simulation.Bodies);
        //Use a simple sorta-neck model so that when the camera looks down, the center of the screen sees past the character.
        //Makes mouselocked ray picking easier.

        //camera.Position = c.Pose.Position + new Vector3(0, 0, 40) ;
        camera.Position = c.Pose.Position + new Vector3(0, shape.HalfLength, 0) +
            camera.Up * (shape.Radius * 1.2f) -
            camera.Forward * (shape.HalfLength + shape.Radius) * cameraBackwardOffsetScale;
    }

    //void RenderControl(ref Vector2 position, float textHeight, string controlName, string controlValue, TextBuilder text, TextBatcher textBatcher, Font font)
    //{
    //    text.Clear().Append(controlName).Append(": ").Append(controlValue);
    //    textBatcher.Write(text, position, textHeight, new Vector3(1), font);
    //    position.Y += textHeight * 1.1f;
    //}
    //public void RenderControls(Vector2 position, float textHeight, TextBatcher textBatcher, TextBuilder text, Font font)
    //{
    //    RenderControl(ref position, textHeight, nameof(MoveForward), ControlStrings.GetName(MoveForward), text, textBatcher, font);
    //    RenderControl(ref position, textHeight, nameof(MoveBackward), ControlStrings.GetName(MoveBackward), text, textBatcher, font);
    //    RenderControl(ref position, textHeight, nameof(MoveRight), ControlStrings.GetName(MoveRight), text, textBatcher, font);
    //    RenderControl(ref position, textHeight, nameof(MoveLeft), ControlStrings.GetName(MoveLeft), text, textBatcher, font);
    //    RenderControl(ref position, textHeight, nameof(Sprint), ControlStrings.GetName(Sprint), text, textBatcher, font);
    //    RenderControl(ref position, textHeight, nameof(Jump), ControlStrings.GetName(Jump), text, textBatcher, font);
    //}


    /// <summary>
    /// Removes the character's body from the simulation and the character from the associated characters set.
    /// </summary>
    public void Dispose()
    {
        characters.Simulation.Shapes.Remove(new BodyReference(bodyHandle, characters.Simulation.Bodies).Collidable.Shape);
        characters.Simulation.Bodies.Remove(bodyHandle);
        characters.RemoveCharacterByBodyHandle(bodyHandle);
    }
}


