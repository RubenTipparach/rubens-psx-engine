using anakinsoft.system.character;
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
    public class PhysicsSystem : IDisposable
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

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    try
                    {
                        System.Console.WriteLine($"PhysicsSystem: Starting disposal - BufferPool blocks before clear: {BufferPool?.GetHashCode()}");
                        
                        // Clear the simulation (removes all bodies, constraints, etc.)
                        Simulation?.Clear();
                        System.Console.WriteLine("PhysicsSystem: Simulation cleared");
                        
                        // Dispose the thread dispatcher
                        ThreadDispatcher?.Dispose();
                        System.Console.WriteLine("PhysicsSystem: ThreadDispatcher disposed");
                        
                        // Clear the buffer pool (this is what fixes the memory leak warning)
                        BufferPool?.Clear();
                        System.Console.WriteLine("PhysicsSystem: BufferPool cleared");
                        
                        System.Console.WriteLine("PhysicsSystem: Successfully disposed of physics resources");
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"PhysicsSystem: Error during disposal: {ex.Message}");
                    }
                }
                disposed = true;
            }
        }

        ~PhysicsSystem()
        {
            Dispose(false);
        }
    }
}
