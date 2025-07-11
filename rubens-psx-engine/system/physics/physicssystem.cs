﻿using anakinsoft.system.character;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace anakinsoft.system.physics
{
    public class PhysicsSystem
    {
        public Simulation Simulation;
        public BufferPool BufferPool = new BufferPool();
        public ThreadDispatcher ThreadDispatcher;

        public PhysicsSystem(ref CharacterControllers characters)
        {
            characters = new CharacterControllers(BufferPool);

            Simulation = Simulation.Create(BufferPool,
                new DemoNarrowPhaseCallbacks(new SpringSettings(30, 3), characters),
                new DemoPoseIntegratorCallbacks(new Vector3(0, -100, 0), angularDamping: 0.2f),
                new SolveDescription(8, 1));

            ThreadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

        }

        public void Update(float dt)
        {
            Simulation.Timestep(dt, ThreadDispatcher);
        }
    }
}
